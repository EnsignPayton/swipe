using System.CommandLine;
using Swipe;
using Swipe.Commands.AddOns;
using Swipe.Commands.Cache;
using Swipe.Commands.Games;

await using var db = new AddOnDatabase();
await db.InitializeAsync();

var verboseOption = new Option<bool>("--verbose", "-v") { Recursive = true };
return await new RootCommand("World of Warcraft AddOn Manager")
{
    verboseOption,
    BuildGames(),
    BuildAddOns(),
    BuildCache(),
}.Parse(args.Length > 0 ? args : ["-h"]).InvokeAsync();

Command BuildGames()
{
    return new Command("game", "Manage game installations")
    {
        BuildList(),
        BuildAdd(),
        BuildRemove(),
        BuildSet(),
        BuildScan(),
    };

    Command BuildList()
    {
        var command = new Command("list", "List registered games");
        command.SetAction(async _ => await new ListGames(db).Execute());
        return command;
    }

    Command BuildAdd()
    {
        var command = new Command("add", "Add a game");
        var nameArg = new Argument<string>("name") { Description = "Name of game" };
        var pathArg = new Argument<string>("path") { Description = "Path to game" };
        command.Arguments.Add(nameArg);
        command.Arguments.Add(pathArg);
        command.SetAction(async pr =>
        {
            var name = pr.GetRequiredValue(nameArg);
            var path = pr.GetRequiredValue(pathArg);
            await new AddGame(db).Execute(name, path);
        });
        return command;
    }

    Command BuildRemove()
    {
        var command = new Command("remove", "Remove a game");
        var nameArg = new Argument<string>("name") { Description = "Name of game" };
        command.Arguments.Add(nameArg);
        command.SetAction(async pr =>
        {
            var name = pr.GetRequiredValue(nameArg);
            await new RemoveGame(db).Execute(name);
        });
        return command;
    }

    Command BuildSet()
    {
        var command = new Command("set", "Set a game as current");
        var nameArg = new Argument<string>("name") { Description = "Name of game" };
        command.Arguments.Add(nameArg);
        command.SetAction(async pr =>
        {
            var name = pr.GetRequiredValue(nameArg);
            await new SetGame(db).Execute(name);
        });
        return command;
    }

    Command BuildScan()
    {
        var command = new Command("scan", "Scan file system for games");
        command.SetAction(async _ => await new ScanGames(db).Execute());
        return command;
    }
}

Command BuildAddOns()
{
    var gameOption = new Option<string>("--game", "-g")
    {
        Description = "Target a specific game",
        Recursive = true,
    };

    return new Command("addon", "Manage addons")
    {
        BuildList(),
        BuildInstall(),
        BuildUpdate(),
        BuildUpdateAll(),
        BuildRemove(),
    };

    Command BuildList()
    {
        var command = new Command("list", "List installed addons");
        command.SetAction(async pr =>
        {
            var verbose = pr.GetValue(verboseOption);
            var game = await ResolveGame(pr);
            if (game is null) return;
            await new ListAddOns(db).Execute(game, verbose);
        });
        return command;
    }

    Command BuildInstall()
    {
        var command = new Command("install", "Install addon");
        var nameArg = new Argument<string>("name") { Description = "Name of addon" };
        command.Arguments.Add(nameArg);
        command.SetAction(async pr =>
        {
            var verbose = pr.GetValue(verboseOption);
            var addonName = pr.GetRequiredValue(nameArg);
            var game = await ResolveGame(pr);
            if (game is null) return;
            await new InstallAddOn(db).Execute(game, addonName, verbose);
        });
        return command;
    }

    Command BuildUpdate()
    {
        var command = new Command("update", "Update addon");
        var nameArg = new Argument<string>("name") { Description = "Name of addon" };
        command.Arguments.Add(nameArg);
        command.SetAction(async pr =>
        {
            var verbose = pr.GetValue(verboseOption);
            var addonName = pr.GetRequiredValue(nameArg);
            var game = await ResolveGame(pr);
            if (game is null) return;
            await new UpdateAddOn(db).Execute(game, addonName, verbose);
        });
        return command;
    }

    Command BuildUpdateAll()
    {
        var command = new Command("update-all", "Update all addons");
        command.SetAction(async pr =>
        {
            var verbose = pr.GetValue(verboseOption);
            var game = await ResolveGame(pr);
            if (game is null) return;
            await new UpdateAllAddOns(db).Execute(game, verbose);
        });
        return command;
    }

    Command BuildRemove()
    {
        var command = new Command("remove", "Remove an addon");
        var nameArg = new Argument<string>("name") { Description = "Name of addon" };
        command.Arguments.Add(nameArg);
        command.SetAction(async pr =>
        {
            var verbose = pr.GetValue(verboseOption);
            var addonName = pr.GetRequiredValue(nameArg);
            var game = await ResolveGame(pr);
            if (game is null) return;
            await new RemoveAddOn(db).Execute(game, addonName, verbose);
        });
        return command;
    }

    async Task<Game?> ResolveGame(ParseResult pr)
    {
        var gameName = pr.GetValue(gameOption);
        if (gameName is null)
        {
            var game = await db.GetCurrentGame();
            if (game is null)
            {
                Console.WriteLine("No game set as current");
            }

            return game;
        }
        else
        {
            var game = await db.GetGame(gameName);
            if (game is null)
            {
                Console.WriteLine($"Game {gameName} not found.");
            }

            return game;
        }
    }
}

Command BuildCache()
{
    return new Command("cache", "Manage cache")
    {
        BuildClear(),
    };

    Command BuildClear()
    {
        var command = new Command("clear", "Remove all cached downloads");
        command.SetAction(_ => ClearCache.Execute());
        return command;
    }
}
