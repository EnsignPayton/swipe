namespace wowup;

public static class Paths
{
    public static readonly string Cache = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cache", "wowup");

    private static readonly string Config = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "wowup");

    public static readonly string Db = Path.Combine(Config, "addons.db");
}