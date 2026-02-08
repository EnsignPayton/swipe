namespace Swipe.Commands.AddOns;

public sealed class UpdateAddOn(AddOnDatabase db)
{
    public async Task Execute(Game game, string addonName, bool verbose)
    {
        var installDir = Path.Combine(game.Path, "Interface", "AddOns");
        if (!Directory.Exists(installDir))
        {
            Print.Line($"AddOns folder not found at {installDir}", prefix: game.Name);
            return;
        }

        var addon = await db.GetAddOn(game.Id, addonName);
        if (addon is null)
        {
            Print.Line($"{addonName} is not installed", prefix: game.Name);
            return;
        }

        Print.Temp($"{addonName} loading...", prefix: game.Name);
        await using var scraper = await CurseScraper.CreateAsync();
        var info = await scraper.GetLatestInfo(addonName);
        if (info is null)
        {
            Print.Line($"{addonName} not found", prefix: game.Name);
            return;
        }

        if (addon.Version == info.Version)
        {
            var zipPath = Path.Combine(Paths.Cache, addon.ZipName);
            if (File.Exists(zipPath))
            {
                var hash = await Utils.HashFile(zipPath);
                if (hash == addon.ZipHash)
                {
                    Print.Temp($"{info.Name} {info.Version} is cached, installing...", prefix: game.Name);
                    var components = await Utils.ApplyZip(zipPath, installDir);
                    Print.Line($"{info.Name} {info.Version} installed", prefix: game.Name);
                    if (verbose) Print.List(components);
                    return;
                }
            }
        }

        Print.Temp($"{info.Name} {info.Version} downloading...", prefix: game.Name);
        var zipName = await scraper.Download(info);
        var zipPath2 = Path.Combine(Paths.Cache, zipName);
        var zipHash = await Utils.HashFile(zipPath2);
        Print.Temp($"{info.Name} {info.Version} installing...", prefix: game.Name);
        var components2 = await Utils.ApplyZip(zipPath2, installDir);
        
        await db.SaveAddOn(game.Id, new AddOn
        {
            Name = addonName,
            Version = info.Version,
            ZipId = info.DownloadId,
            ZipName = zipName,
            ZipHash = zipHash,
            Components = components2.Select(x => new AddOnComponent { Name = x }).ToList(),
        });

        Print.Line($"{info.Name} {info.Version} installed", prefix: game.Name);
        if (verbose) Print.List(components2);
    }
}
