namespace wowup.Commands;

public sealed class Update(AddOnDatabase db, string addOnsFolder)
{
    private static readonly string Home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    private static readonly string CachePath = Path.Combine(Home, ".cache", "wowup");

    public async Task Execute(string text)
    {
        var installation = await db.GetLatestInstallation(text);
        if (installation is null)
        {
            Console.WriteLine("{0} is not installed", text);
            return;
        }
        
        Console.WriteLine("{0} querying...", text);
        await using var scraper = await CurseScraper.CreateAsync();
        var info = await scraper.GetLatestInfo(text);
        if (info is null)
        {
            Console.WriteLine("{0} not found", text);
            return;
        }

        if (installation.Version == info.Version)
        {
            Console.WriteLine("{0} {1} is up to date", text, info.Version);
            return;
        }

        foreach (var item in installation.Content)
        {
            var dir = Path.Combine(addOnsFolder, item.DirName);
            if (Directory.Exists(dir))
            {
                Console.WriteLine("Removing {0}...", item.DirName);
                Directory.Delete(dir, recursive: true);
            }
        }

        Console.WriteLine("{0} {1} downloading...", info.Name, info.Version);
        var zipName = await scraper.Download(info, CachePath);
        Console.WriteLine("{0} {1} downloaded, installing...", info.Name, info.Version);
        var zipPath = Path.Combine(CachePath, zipName);
        var zipHash = await Utils.HashFile(zipPath);
        var content = await Utils.ApplyZip(zipPath, addOnsFolder);

        await db.SaveInstallation(new Installation
        {
            Name = info.Name,
            Version = info.Version,
            ZipId = info.DownloadId,
            ZipName = zipName,
            ZipHash = zipHash,
            Content = content.Select(x => new InstallationContent { DirName = x }).ToList(),
        });

        Console.WriteLine("{0} {1} installed", info.Name, info.Version);
    }
}