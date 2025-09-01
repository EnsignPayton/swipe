using System.CommandLine;

namespace wowup.Commands;

public sealed class UpdateAddOn(AddOnDatabase db)
{
    private static readonly string CachePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cache", "wowup");

    public static Command BuildCommand(AddOnDatabase db)
    {
        var command = new Command("update", "Update addon");
        var nameArg = new Argument<string>("name") { Description = "Name of addon" };
        command.Arguments.Add(nameArg);
        command.SetAction(async pr =>
        {
            var gameName = pr.GetValue<string>("--game");
            var addonName = pr.GetRequiredValue(nameArg);
            var target = new UpdateAddOn(db);
            if (gameName is null)
                await target.Execute(addonName);
            else
                await target.Execute(gameName, addonName);
        });
        return command;
    }

    private async Task Execute(string addonName)
    {
        var game = await db.GetCurrentGame();
        if (game is null)
        {
            Console.WriteLine("No game set as current.");
            return;
        }

        await Execute(game, addonName);
    }

    private async Task Execute(string gameName, string addonName)
    {
        var game = await db.GetGame(gameName);
        if (game is null)
        {
            Console.WriteLine("No game set as current.");
            return;
        }

        await Execute(game, addonName);
    }

    private async Task Execute(Game game, string addonName)
    {
        var installDir = Path.Combine(game.Path, "Interface", "AddOns");
        if (!Directory.Exists(installDir))
        {
            Console.WriteLine($"[{game.Name}] AddOns folder not found at {installDir}");
            return;
        }

        var addon = await db.GetAddOn(game.Id, addonName);
        if (addon is null)
        {
            Console.WriteLine($"[{game.Name}] {addonName} is not installed.");
            return;
        }

        Console.WriteLine($"[{game.Name}] {addonName} loading...");
        await using var scraper = await CurseScraper.CreateAsync();
        var info = await scraper.GetLatestInfo(addonName);
        if (info is null)
        {
            Console.WriteLine($"[{game.Name}] {addonName} not found.");
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

        Console.WriteLine($"[{game.Name}] {addonName} {info.Version} downloading...");
        var zipName = await scraper.Download(info, CachePath);
        var zipPath2 = Path.Combine(CachePath, zipName);
        var zipHash = await Utils.HashFile(zipPath2);
        Console.WriteLine($"[{game.Name}] {addonName} {info.Version} installing...");
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

        Console.WriteLine($"[{game.Name}] {addonName} {info.Version} installed.");
        foreach (var component in components2)
        {
            Console.WriteLine($"  {component}");
        }
    }
}