namespace wowup.Commands.AddOns;

public sealed class RemoveAddOn(AddOnDatabase db)
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

        Print.Temp($"{addonName} uninstalling...", prefix: game.Name);

        foreach (var component in addon.Components)
        {
            var dir = Path.Combine(installDir, component.Name);
            Console.WriteLine($"  {component.Name}");
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }

        await db.UnlinkAddOn(game.Id, addon.Id);

        Print.Line($"{addonName} uninstalled", prefix: game.Name);
        if (verbose) Print.List(addon.Components.Select(x => x.Name));
    }
}