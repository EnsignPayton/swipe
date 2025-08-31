namespace wowup.Commands;

public sealed class Install(AddOnDatabase db, string addOnsFolder)
{
    private static readonly string Home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    private static readonly string CachePath = Path.Combine(Home, ".cache", "wowup");

    public async Task Execute(string text)
    {
        var installation = await db.GetLatestInstallation(text);
        if (installation is not null)
        {
            bool allInstalled = true;
            var installed = Directory.GetDirectories(addOnsFolder);
            foreach (var content in installation.Content)
            {
                if (!installed.Any(x => x.EndsWith(content.DirName)))
                {
                    allInstalled = false;
                    break;
                }
            }

            if (allInstalled)
            {
                Console.WriteLine("{0} {1} is already installed", text, installation.Version);
                return;
            }

            var zipPath = Path.Combine(CachePath, installation.ZipName);
            if (File.Exists(zipPath))
            {
                var hash = await Utils.HashFile(zipPath);
                if (hash == installation.ZipHash)
                {
                    Console.WriteLine("{0} {1} is cached, installing...", text, installation.Version);
                    await Utils.ApplyZip(zipPath, addOnsFolder);
                    Console.WriteLine("{0} {1} installed", text, installation.Version);
                    return;
                }
            }
        }

        Console.WriteLine("{0} querying...", text);
        await using var scraper = await CurseScraper.CreateAsync();
        var info = await scraper.GetLatestInfo(text);
        if (info is null)
        {
            Console.WriteLine("{0} not found", text);
        }
        else
        {
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
}