namespace Swipe.Commands.AddOns;

public sealed class UpdateAllAddOns(AddOnDatabase db)
{
    public async Task Execute(Game game, bool verbose)
    {
        var installDir = Path.Combine(game.Path, "Interface", "AddOns");
        if (!Directory.Exists(installDir))
        {
            Print.Line($"AddOns folder not found at {installDir}", prefix: game.Name);
            return;
        }

        var addons = await db.GetAddOns(game.Id, includeComponents: false);
        if (addons.Count == 0)
        {
            Print.Line("No installed addons", prefix: game.Name);
            return;
        }

        Print.Line("Loading addons...", prefix: game.Name);
        await using var scraper = await CurseScraper.CreateAsync();
        foreach (var addon in addons)
        {
            await Update(game, addon, installDir, scraper, verbose);
        }
    }

    private async Task Update(Game game, AddOn addon, string installDir, CurseScraper scraper, bool verbose)
    {
        var info = await scraper.GetLatestInfo(addon.Name);
        if (info is null)
        {
            Print.Line($"{addon.Name} not found", prefix: game.Name);
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
            Name = addon.Name,
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
