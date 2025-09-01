using System.CommandLine;
using wowup;
using wowup.Commands;

await using var db = new AddOnDatabase();
await db.InitializeAsync();

return await new RootCommand
{
    Subcommands =
    {
        new Command("game", "Manage game installations")
        {
            Subcommands =
            {
                ListGames.BuildCommand(db),
                AddGame.BuildCommand(db),
                RemoveGame.BuildCommand(db),
                SetGame.BuildCommand(db),
                ScanGames.BuildCommand(db),
            }
        },
        new Command("addon", "Manage addons")
        {
            Options = 
            {
                new Option<string>("--game", "-g")
                {
                    Description = "Target a specific game",
                    Recursive = true,
                }
            },
            Subcommands =
            {
                ListAddOns.BuildCommand(db),
                InstallAddOn.BuildCommand(db),
                RemoveAddOn.BuildCommand(db),
                UpdateAddOn.BuildCommand(db),
            }
        },
    }
}.Parse(args).InvokeAsync();
