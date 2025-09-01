namespace wowup.Commands.Cache;

public static class ClearCache
{
    public static void Execute()
    {
        foreach (var zip in Directory.GetFiles(Paths.Cache, "*.zip").OrderBy(x => x))
        {
            File.Delete(zip);
            Print.Line($"  {Path.GetFileName(zip)} deleted");
        }

        Print.Line("Cache cleared");
    }
}