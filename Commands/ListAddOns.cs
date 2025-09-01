using System.CommandLine;

namespace wowup.Commands;

public sealed class ListAddOns(AddOnDatabase db)
{
    public static Command BuildCommand(AddOnDatabase db)
    {
        var command = new Command("list", "List installed addons");
        command.SetAction(async pr =>
        {
            var game = await Utils.ResolveGame(db, pr);
            if (game is null) return;
            await new ListAddOns(db).Execute(game);
        });
        return command;
    }

    private async Task Execute(Game game)
    {
        var installDir = Path.Combine(game.Path, "Interface", "AddOns");
        if (!Directory.Exists(installDir))
        {
            Console.WriteLine($"[{game.Name}] AddOns folder not found at {installDir}");
            return;
        }

        var installedFolders = Directory.GetDirectories(installDir)
            .Select(Path.GetFileName)
            .ToList();

        var addons = await db.GetAddOns(game.Id);

        var installedAddons = addons
            .Where(x => x.Components.All(c => installedFolders.Contains(c.Name)))
            .ToList();

        if (installedAddons.Count == 0)
        {
            Console.WriteLine($"[{game.Name}] No installed addons.");
        }
        else
        {
            Console.WriteLine($"[{game.Name}] Installed AddOns:");
            foreach (var addon in installedAddons)
            {
                Console.WriteLine($"  {addon.Name} {addon.Version}");
                foreach (var component in addon.Components)
                {
                    Console.WriteLine($"    {component.Name}");
                }
            }
        }
    }
}
