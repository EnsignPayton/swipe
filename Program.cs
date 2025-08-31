using wowup;
using wowup.Commands;

if (args.Length == 0)
{
    Console.WriteLine("wowup {0}", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);
    Console.WriteLine();
    Console.WriteLine("Usage: wowup [command]");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  list     List all installed addons");
    Console.WriteLine("  update   Update a single addon or all installed addons");
    Console.WriteLine("  install  Install a new addon");
    Console.WriteLine("  remove   Remove an installed addon");
    return;
}

var configManager = new ConfigManager();
configManager.Load();
await using var db = new AddOnDatabase();
await db.InitializeAsync();

switch (args[0])
{
    case "list":
        await new ListAddOns(db, configManager.Config.AddOnsFolder).Execute();
        break;
    case "update":
        if (args.Length > 1)
        {
            await new Update(db, configManager.Config.AddOnsFolder).Execute(args[1]);
        }
        else
        {
            // TODO: Update all
            Console.WriteLine("Coming soon");
        }
        break;
    case "install":
        if (args.Length > 1)
        {
            await new Install(db, configManager.Config.AddOnsFolder).Execute(args[1]);
        }
        else
        {
            Console.WriteLine("Usage: wowup install [addon]");
        }
        break;
    case "remove":
        if (args.Length > 1)
        {
            await new Remove(db, configManager.Config.AddOnsFolder).Execute(args[1]);
        }
        else
        {
            Console.WriteLine("Usage: wowup remove [addon]");
        }
        break;
    default:
        Console.WriteLine("Unknown command: {0}", args[0]);
        break;
}
