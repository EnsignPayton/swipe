namespace wowup;

public class AppConfig
{
    public string AddOnsFolder { get; set; } = string.Empty;
    public List<AddOnEntry> AddOns { get; set; } = [];
}

public class AddOnEntry
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string VersionDownloadId { get; set; } = string.Empty;
    public List<string> InstalledFolders { get; set; } = [];
}
