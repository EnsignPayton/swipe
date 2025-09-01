using System.CommandLine;

namespace wowup.Commands;

public sealed class RemoveAddOn(AddOnDatabase db)
{
    public static Command BuildCommand(AddOnDatabase db)
    {
        var command = new Command("remove", "Remove an addon");
        var nameArg = new Argument<string>("name") { Description = "Name of addon" };
        command.Arguments.Add(nameArg);
        command.SetAction(async pr =>
        {
            var addonName = pr.GetRequiredValue(nameArg);
            var game = await Utils.ResolveGame(db, pr);
            if (game is null) return;
            await new RemoveAddOn(db).Execute(game, addonName);
        });
        return command;
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

        Console.WriteLine($"[{game.Name}] {addonName} uninstalling...");

        foreach (var component in addon.Components)
        {
            var dir = Path.Combine(installDir, component.Name);
            Console.WriteLine($"  {component.Name}");
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }

        Console.WriteLine($"[{game.Name}] {addonName} uninstalled.");
    }
}