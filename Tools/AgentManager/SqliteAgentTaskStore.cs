using Microsoft.Data.Sqlite;

namespace UnderGrind.AgentManager;

public sealed record AgentTask(
    string Id,
    string Title,
    string Description,
    string State,
    int Priority,
    string Provider,
    DateTime CreatedUtc,
    string? LeaseOwner);

public sealed class SqliteAgentTaskStore
{
    private readonly string _connectionString;

    public SqliteAgentTaskStore(string databasePath)
    {
        var fullPath = Path.GetFullPath(databasePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = fullPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
            ForeignKeys = true
        }.ToString();
    }

    public void Initialize()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            PRAGMA journal_mode = WAL;
            PRAGMA foreign_keys = ON;
            CREATE TABLE IF NOT EXISTS Tasks (
                Id TEXT PRIMARY KEY,
                Title TEXT NOT NULL,
                Description TEXT NOT NULL,
                State TEXT NOT NULL CHECK (State IN ('Queued','Running','WaitingForApproval','Succeeded','Failed','Cancelled','Blocked')),
                Priority INTEGER NOT NULL DEFAULT 0,
                Provider TEXT NOT NULL,
                CreatedUtc TEXT NOT NULL,
                UpdatedUtc TEXT NOT NULL,
                LeaseOwner TEXT NULL,
                LeaseUntilUtc TEXT NULL,
                Result TEXT NULL,
                Error TEXT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_Tasks_Ready ON Tasks(State, Priority DESC, CreatedUtc);
            CREATE TABLE IF NOT EXISTS TaskDependencies (
                TaskId TEXT NOT NULL REFERENCES Tasks(Id) ON DELETE CASCADE,
                PrerequisiteTaskId TEXT NOT NULL REFERENCES Tasks(Id) ON DELETE RESTRICT,
                PRIMARY KEY (TaskId, PrerequisiteTaskId),
                CHECK (TaskId <> PrerequisiteTaskId)
            );
            CREATE TABLE IF NOT EXISTS AuditLog (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                TaskId TEXT NULL,
                Event TEXT NOT NULL,
                Details TEXT NULL,
                CreatedUtc TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_AuditLog_Task ON AuditLog(TaskId, Id);
            """;
        command.ExecuteNonQuery();
    }

    public string Enqueue(string title, string description, int priority = 0, string provider = "manual",
        IReadOnlyCollection<string>? prerequisiteIds = null)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Task title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(provider)) throw new ArgumentException("Provider is required.", nameof(provider));

        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        var id = Guid.NewGuid().ToString("N");
        var now = DateTime.UtcNow.ToString("O");

        using (var insert = connection.CreateCommand())
        {
            insert.Transaction = transaction;
            insert.CommandText = """
                INSERT INTO Tasks (Id, Title, Description, State, Priority, Provider, CreatedUtc, UpdatedUtc)
                VALUES ($id, $title, $description, 'Queued', $priority, $provider, $now, $now);
                """;
            insert.Parameters.AddWithValue("$id", id);
            insert.Parameters.AddWithValue("$title", title.Trim());
            insert.Parameters.AddWithValue("$description", description ?? string.Empty);
            insert.Parameters.AddWithValue("$priority", priority);
            insert.Parameters.AddWithValue("$provider", provider.Trim());
            insert.Parameters.AddWithValue("$now", now);
            insert.ExecuteNonQuery();
        }

        foreach (var prerequisiteId in prerequisiteIds ?? Array.Empty<string>())
        {
            using var dependency = connection.CreateCommand();
            dependency.Transaction = transaction;
            dependency.CommandText = "INSERT INTO TaskDependencies (TaskId, PrerequisiteTaskId) VALUES ($task, $prerequisite);";
            dependency.Parameters.AddWithValue("$task", id);
            dependency.Parameters.AddWithValue("$prerequisite", prerequisiteId);
            dependency.ExecuteNonQuery();
        }

        AppendAudit(connection, transaction, id, "TaskQueued", $"provider={provider}; priority={priority}");
        transaction.Commit();
        return id;
    }

    public IReadOnlyList<AgentTask> List()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Title, Description, State, Priority, Provider, CreatedUtc, LeaseOwner
            FROM Tasks ORDER BY CreatedUtc DESC;
            """;
        using var reader = command.ExecuteReader();
        var tasks = new List<AgentTask>();
        while (reader.Read())
            tasks.Add(ReadTask(reader));
        return tasks;
    }

    public AgentTask? TryLeaseNext(string workerId, TimeSpan leaseDuration)
    {
        if (string.IsNullOrWhiteSpace(workerId)) throw new ArgumentException("Worker ID is required.", nameof(workerId));

        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        var now = DateTime.UtcNow;
        using var select = connection.CreateCommand();
        select.Transaction = transaction;
        select.CommandText = """
            SELECT t.Id, t.Title, t.Description, t.State, t.Priority, t.Provider, t.CreatedUtc, t.LeaseOwner
            FROM Tasks t
            WHERE t.State = 'Queued'
              AND NOT EXISTS (
                  SELECT 1 FROM TaskDependencies d
                  JOIN Tasks p ON p.Id = d.PrerequisiteTaskId
                  WHERE d.TaskId = t.Id AND p.State <> 'Succeeded'
              )
            ORDER BY t.Priority DESC, t.CreatedUtc
            LIMIT 1;
            """;

        AgentTask? task = null;
        using (var reader = select.ExecuteReader())
            if (reader.Read()) task = ReadTask(reader);

        if (task is null)
        {
            transaction.Commit();
            return null;
        }

        using var update = connection.CreateCommand();
        update.Transaction = transaction;
        update.CommandText = """
            UPDATE Tasks
            SET State = 'Running', LeaseOwner = $worker, LeaseUntilUtc = $until, UpdatedUtc = $now
            WHERE Id = $id AND State = 'Queued';
            """;
        update.Parameters.AddWithValue("$worker", workerId);
        update.Parameters.AddWithValue("$until", now.Add(leaseDuration).ToString("O"));
        update.Parameters.AddWithValue("$now", now.ToString("O"));
        update.Parameters.AddWithValue("$id", task.Id);
        if (update.ExecuteNonQuery() != 1)
        {
            transaction.Rollback();
            return null;
        }

        AppendAudit(connection, transaction, task.Id, "TaskLeased", $"worker={workerId}");
        transaction.Commit();
        return task with { State = "Running", LeaseOwner = workerId };
    }

    public void Complete(string taskId, string workerId, string? result)
        => Finish(taskId, workerId, "Succeeded", result, null);

    public void Fail(string taskId, string workerId, string error)
        => Finish(taskId, workerId, "Failed", null, error);

    public int RecoverExpiredLeases()
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        var now = DateTime.UtcNow.ToString("O");
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            UPDATE Tasks
            SET State = 'Queued', LeaseOwner = NULL, LeaseUntilUtc = NULL, UpdatedUtc = $now
            WHERE State = 'Running' AND LeaseUntilUtc < $now;
            """;
        command.Parameters.AddWithValue("$now", now);
        var recovered = command.ExecuteNonQuery();
        AppendAudit(connection, transaction, null, "ExpiredLeasesRecovered", $"count={recovered}");
        transaction.Commit();
        return recovered;
    }

    private void Finish(string taskId, string workerId, string state, string? result, string? error)
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            UPDATE Tasks
            SET State = $state, Result = $result, Error = $error,
                LeaseOwner = NULL, LeaseUntilUtc = NULL, UpdatedUtc = $now
            WHERE Id = $id AND State = 'Running' AND LeaseOwner = $worker;
            """;
        command.Parameters.AddWithValue("$state", state);
        command.Parameters.AddWithValue("$result", (object?)result ?? DBNull.Value);
        command.Parameters.AddWithValue("$error", (object?)error ?? DBNull.Value);
        command.Parameters.AddWithValue("$now", DateTime.UtcNow.ToString("O"));
        command.Parameters.AddWithValue("$id", taskId);
        command.Parameters.AddWithValue("$worker", workerId);

        if (command.ExecuteNonQuery() != 1)
            throw new InvalidOperationException("Task not found, not leased, or leased by a different worker.");

        AppendAudit(connection, transaction, taskId, "Task" + state, error ?? result);
        transaction.Commit();
    }

    private static AgentTask ReadTask(SqliteDataReader reader) => new(
        reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
        reader.GetInt32(4), reader.GetString(5), DateTime.Parse(reader.GetString(6)).ToUniversalTime(),
        reader.IsDBNull(7) ? null : reader.GetString(7));

    private static void AppendAudit(SqliteConnection connection, SqliteTransaction transaction,
        string? taskId, string eventName, string? details)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO AuditLog (TaskId, Event, Details, CreatedUtc)
            VALUES ($task, $event, $details, $now);
            """;
        command.Parameters.AddWithValue("$task", (object?)taskId ?? DBNull.Value);
        command.Parameters.AddWithValue("$event", eventName);
        command.Parameters.AddWithValue("$details", (object?)details ?? DBNull.Value);
        command.Parameters.AddWithValue("$now", DateTime.UtcNow.ToString("O"));
        command.ExecuteNonQuery();
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }
}
