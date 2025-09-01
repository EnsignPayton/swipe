namespace wowup.Commands.AddOns;

public sealed class RemoveAddOn(AddOnDatabase db)
{
    public async Task Execute(Game game, string addonName)
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