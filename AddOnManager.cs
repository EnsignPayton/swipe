using System.IO.Compression;
using System.Text.Json;

namespace wowup;

public static class AddOnManager
{
    private static readonly string Home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    private static readonly string ConfigPath = Path.Combine(Home, ".config", "wowup");
    private static readonly string CachePath = Path.Combine(Home, ".cache", "wowup");
    
    public static void List()
    {
        var config = LoadConfig();
        if (config.AddOns.Count == 0)
        {
            Console.WriteLine("No AddOns installed");
        }

        foreach (var entry in config.AddOns)
        {
            Console.WriteLine(entry.Name);
            Console.WriteLine("  version: {0}", entry.Version);
        }
    }

    public static void Update(string text)
    {
        var config = LoadConfig();
        
        var entry = config.AddOns.FirstOrDefault(x => x.Name == text);
        if (entry is null)
        {
            Console.WriteLine("{0} is not installed", text);
        }
        else
        {
            Update(entry);
            SaveConfig(config);
        }
    }

    public static void UpdateAll()
    {
        var config = LoadConfig();

        foreach (var entry in config.AddOns)
        {
            Update(entry);
        }
        
        SaveConfig(config);
    }

    private static void Update(AddOnEntry entry)
    {
        var oldVersion = entry.Version;
        entry.Version = oldVersion == "4.20.69" ? "6.9.420" : "4.20.69";
        Console.WriteLine("{0} version {1} --> {2}", entry.Name, oldVersion, entry.Version);
    }

    public static async Task Install(string text)
    {
        var config = LoadConfig();

        var existing = config.AddOns.FirstOrDefault(x => x.Name == text);
        if (existing is not null)
        {
            Console.WriteLine("{0} version {1} is already installed", existing.Name, existing.Version);
            return;
        }

        await using var scraper = await CurseScraper.CreateAsync();
        var entry = await scraper.GetLatestInfo(text);
        if (entry is null)
        {
            Console.WriteLine("{0} not found", text);
            return;
        }

        var fileName = await scraper.Download(entry, CachePath);
        await using (var fs = File.OpenRead(Path.Combine(CachePath, fileName)))
        using (var zipArchive = new ZipArchive(fs, ZipArchiveMode.Read))
        {
            foreach (var zipEntry in zipArchive.Entries)
            {
                if (zipEntry.Name.Length == 0 && zipEntry.FullName.IndexOf('/') == zipEntry.FullName.Length - 1)
                {
                    entry.InstalledFolders.Add(zipEntry.FullName.TrimEnd('/'));
                }
            }

            zipArchive.ExtractToDirectory(config.AddOnsFolder, overwriteFiles: true);
        }

        config.AddOns.Add(entry);
        SaveConfig(config);
        Console.WriteLine("{0} version {1} has been installed", entry.Name, entry.Version);
    }

    public static void Remove(string text)
    {
        var config = LoadConfig();

        var entry = config.AddOns.FirstOrDefault(x => x.Name == text);
        if (entry is null)
        {
            Console.WriteLine("{0} is not installed", text);
            return;
        }

        foreach (var folder in entry.InstalledFolders)
        {
            var path = Path.Combine(config.AddOnsFolder, folder);
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }

        config.AddOns.Remove(entry);
        SaveConfig(config);
        Console.WriteLine("{0} has been removed", text);
    }

    private static AppConfig LoadConfig()
    {
        if (!Directory.Exists(ConfigPath)) return new();
        var filePath = Path.Combine(ConfigPath, "config.json");
        if (!File.Exists(filePath)) return new();
        var text = File.ReadAllText(filePath);
        var config = JsonSerializer.Deserialize<AppConfig>(text);
        return config ?? new();
    }

    private static void SaveConfig(AppConfig config)
    {
        if (!Directory.Exists(ConfigPath))
            Directory.CreateDirectory(ConfigPath);
        var filePath = Path.Combine(ConfigPath, "config.json");
        var text = JsonSerializer.Serialize(config);
        File.WriteAllText(path: filePath, contents: text);
    }
}
