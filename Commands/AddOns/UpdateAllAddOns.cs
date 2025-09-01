namespace wowup.Commands.AddOns;

public sealed class UpdateAllAddOns(AddOnDatabase db)
{
    private static readonly string CachePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cache", "wowup");

    public async Task Execute(Game game)
    {
        var installDir = Path.Combine(game.Path, "Interface", "AddOns");
        if (!Directory.Exists(installDir))
        {
            Console.WriteLine($"[{game.Name}] AddOns folder not found at {installDir}");
            return;
        }

        var addons = await db.GetAddOns(game.Id);
        if (addons.Count == 0)
        {
            Console.WriteLine($"[{game.Name}] No installed addons.");
            return;
        }

        Console.WriteLine($"[{game.Name}] loading addons...");
        await using var scraper = await CurseScraper.CreateAsync();
        foreach (var addon in addons)
        {
            await Update(game, addon, installDir, scraper);
        }
    }

    private async Task Update(Game game, AddOn addon, string installDir, CurseScraper scraper)
    {
        var info = await scraper.GetLatestInfo(addon.Name);
        if (info is null)
        {
            Console.WriteLine($"[{game.Name}] {addon.Name} not found.");
            return;
        }

        if (addon.Version == info.Version)
        {
            var zipPath = Path.Combine(CachePath, addon.ZipName);
            if (File.Exists(zipPath))
            {
                var hash = await Utils.HashFile(zipPath);
                if (hash == addon.ZipHash)
                {
                    Console.WriteLine($"[{game.Name}] {addon.Name} {addon.Version} is cached, installing...");
                    var components = await Utils.ApplyZip(zipPath, installDir);
                    Console.WriteLine($"[{game.Name}] {addon.Name} {addon.Version} installed");
                    foreach (var component in components)
                    {
                        Console.WriteLine($"  {component}");
                    }

                    return;
                }
            }
        }

        Console.WriteLine($"[{game.Name}] {addon.Name} {info.Version} downloading...");
        var zipName = await scraper.Download(info, CachePath);
        var zipPath2 = Path.Combine(CachePath, zipName);
        var zipHash = await Utils.HashFile(zipPath2);
        Console.WriteLine($"[{game.Name}] {addon.Name} {info.Version} installing...");
        var components2 = await Utils.ApplyZip(zipPath2, installDir);

        await db.SaveAddOn(game.Id, new AddOn
        {
            Name = addon.Name,
            Version = info.Version,
            ZipId = info.DownloadId,
            ZipName = zipName,
            ZipHash = zipHash,
            Components = components2.Select(x => new AddOnComponent { Name = x }).ToList(),
        });

        Console.WriteLine($"[{game.Name}] {addon.Name} {info.Version} installed.");
        foreach (var component in components2)
        {
            Console.WriteLine($"  {component}");
        }
    }
}