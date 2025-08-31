using System.Text.Json;

namespace wowup;

public class ConfigManager
{
    private static readonly string Home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    private static readonly string ConfigPath = Path.Combine(Home, ".config", "wowup");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    public AppConfig Config { get; private set; } = new();

    public void Load()
    {
        Config = LoadConfig();
    }

    public void Save()
    {
        SaveConfig(Config);
    }

    private static AppConfig LoadConfig()
    {
        if (!Directory.Exists(ConfigPath)) return new();
        var filePath = Path.Combine(ConfigPath, "config.json");
        if (!File.Exists(filePath)) return new();
        var text = File.ReadAllText(filePath);
        var config = JsonSerializer.Deserialize<AppConfig>(text, JsonOptions);
        return config ?? new();
    }

    private static void SaveConfig(AppConfig config)
    {
        if (!Directory.Exists(ConfigPath))
            Directory.CreateDirectory(ConfigPath);
        var filePath = Path.Combine(ConfigPath, "config.json");
        var text = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(path: filePath, contents: text);
    }
}
public class AppConfig
{
    public string AddOnsFolder { get; set; } = string.Empty;
    
    [Obsolete]
    public List<AddOnEntry> AddOns { get; set; } = [];
}

[Obsolete]
public class AddOnEntry
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
}
