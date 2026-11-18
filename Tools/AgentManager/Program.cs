using UnderGrind.AgentManager;

if (args.Length == 0)
{
    PrintUsage();
    return 2;
}

var settings = AgentManagerSettings.Load(args.Length > 1 && args[0] == "--settings" ? args[1] : null);
var store = new SqliteAgentTaskStore(settings.DatabasePath);
store.Initialize();

var commandOffset = args[0] == "--settings" ? 2 : 0;
if (commandOffset >= args.Length)
{
    PrintUsage();
    return 2;
}

switch (args[commandOffset].ToLowerInvariant())
{
    case "init":
        Console.WriteLine($"Database initialized: {settings.DatabasePath}");
        return 0;

    case "enqueue":
        if (args.Length < commandOffset + 3)
        {
            Console.Error.WriteLine("Usage: enqueue <title> <description> [priority] [provider]");
            return 2;
        }
        var priority = args.Length > commandOffset + 3 &&
                       int.TryParse(args[commandOffset + 3], out var parsedPriority)
            ? parsedPriority
            : 0;
        var provider = args.Length > commandOffset + 4 ? args[commandOffset + 4] : "manual";
        var taskId = store.Enqueue(args[commandOffset + 1], args[commandOffset + 2], priority, provider);
        Console.WriteLine(taskId);
        return 0;

    case "list":
        foreach (var task in store.List())
            Console.WriteLine($"{task.Id}\t{task.State}\tP{task.Priority}\t{task.Provider}\t{task.Title}");
        return 0;

    case "lease":
        if (args.Length <= commandOffset + 1)
        {
            Console.Error.WriteLine("Usage: lease <worker-id>");
            return 2;
        }
        var leased = store.TryLeaseNext(args[commandOffset + 1], settings.LeaseDuration);
        if (leased is null)
        {
            Console.WriteLine("No ready task.");
            return 0;
        }
        Console.WriteLine($"{leased.Id}\t{leased.Provider}\t{leased.Title}\n{leased.Description}");
        return 0;

    case "complete":
        if (args.Length < commandOffset + 3)
        {
            Console.Error.WriteLine("Usage: complete <task-id> <worker-id> [result]");
            return 2;
        }
        store.Complete(args[commandOffset + 1], args[commandOffset + 2],
            args.Length > commandOffset + 3 ? args[commandOffset + 3] : null);
        return 0;

    case "fail":
        if (args.Length < commandOffset + 4)
        {
            Console.Error.WriteLine("Usage: fail <task-id> <worker-id> <error>");
            return 2;
        }
        store.Fail(args[commandOffset + 1], args[commandOffset + 2], args[commandOffset + 3]);
        return 0;

    case "recover":
        Console.WriteLine($"Expired leases recovered: {store.RecoverExpiredLeases()}");
        return 0;

    default:
        PrintUsage();
        return 2;
}

static void PrintUsage()
{
    Console.WriteLine("""
UnderGrind Agent Manager — persistent task queue

Commands:
  init
  enqueue <title> <description> [priority] [provider]
  list
  lease <worker-id>
  complete <task-id> <worker-id> [result]
  fail <task-id> <worker-id> <error>
  recover

Optional prefix:
  --settings <path>
""");
}
