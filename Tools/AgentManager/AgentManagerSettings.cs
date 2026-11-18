using System.Text.Json;

namespace UnderGrind.AgentManager;

public sealed class AgentManagerSettings
{
    public string DatabasePath { get; init; } = "Data/undergrind-agent-manager.db";
    public int LeaseDurationSeconds { get; init; } = 900;

    public TimeSpan LeaseDuration => TimeSpan.FromSeconds(Math.Clamp(LeaseDurationSeconds, 30, 86400));

    public static AgentManagerSettings Load(string? path)
    {
        var settingsPath = path ?? Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(settingsPath))
            return new AgentManagerSettings();

        var json = File.ReadAllText(settingsPath);
        var settings = JsonSerializer.Deserialize<AgentManagerSettings>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (settings is null || string.IsNullOrWhiteSpace(settings.DatabasePath))
            throw new InvalidOperationException("Agent Manager settings are invalid.");

        return settings;
    }
}
