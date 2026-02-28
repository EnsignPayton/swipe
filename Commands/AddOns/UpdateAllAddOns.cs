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
        var maxlen = addons.Max(x => x.Name.Length);
	var verlen = addons.Max(x => x.Version.Length);
        await using var scraper = await CurseScraper.CreateAsync();
        foreach (var addon in addons)
        {
		try
		{
		    await Update(game, addon, installDir, scraper, verbose, maxlen, verlen);
		}
		catch (Exception ex)
		{
			Console.WriteLine("[ERR] Update Failed for {0}: {1}", addon.Name, ex.Message);
		}
        }
    }

    private async Task Update(Game game, AddOn addon, string installDir, CurseScraper scraper, bool verbose, int maxlen, int verlen)
    {
        var info = await scraper.GetLatestInfo(addon.Name);
        if (info is null)
        {
            Print.Line($"{addon.Name.PadRight(maxlen)}  not found", prefix: game.Name);
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
                    Print.Temp($"{info.Name.PadRight(maxlen)}  {info.Version.PadRight(verlen)}  is cached, restoring...", prefix: game.Name);
                    var components = await Utils.ApplyZip(zipPath, installDir);
                    Print.Line($"{info.Name.PadRight(maxlen)}  {info.Version.PadRight(verlen)}  restored from cache", prefix: game.Name);
                    if (verbose) Print.List(components);
                    return;
                }
            }
        }

        Print.Temp($"{info.Name.PadRight(maxlen)}  {info.Version.PadRight(verlen)}  downloading...", prefix: game.Name);
        var zipName = await scraper.Download(info);
        var zipPath2 = Path.Combine(Paths.Cache, zipName);
        var zipHash = await Utils.HashFile(zipPath2);
        Print.Temp($"{info.Name.PadRight(maxlen)}  {info.Version.PadRight(verlen)}  installing...", prefix: game.Name);
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

        Print.Line($"{info.Name.PadRight(maxlen)}  {info.Version.PadRight(verlen)}  installed", prefix: game.Name);
        if (verbose) Print.List(components2);
    }
}
