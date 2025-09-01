namespace wowup.Commands.AddOns;

public sealed class ListAddOns(AddOnDatabase db)
{
    public async Task Execute(Game game, bool verbose)
    {
        var installDir = Path.Combine(game.Path, "Interface", "AddOns");
        if (!Directory.Exists(installDir))
        {
            Print.Line($"AddOns folder not found at {installDir}", prefix: game.Name);
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
            Print.Line("No installed addons.", prefix: game.Name);
        }
        else
        {
            Print.Line("Installed addons:", prefix: game.Name);
            var maxlen = installedAddons.Max(x => x.Name.Length);
            foreach (var addon in installedAddons)
            {
                Print.Line($"  {addon.Name.PadRight(maxlen)}  {addon.Version}");
                if (verbose) Print.List(addon.Components.Select(x => x.Name), indent: 4);
            }
        }
    }
}
