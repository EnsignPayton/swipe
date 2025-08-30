using wowup;

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

switch (args[0])
{
    case "list":
        AddOnManager.List();
        break;
    case "update":
        if (args.Length > 1)
        {
            AddOnManager.Update(args[1]);
        }
        else
        {
            AddOnManager.UpdateAll();
        }
        break;
    case "install":
        if (args.Length > 1)
        {
            await AddOnManager.Install(args[1]);
        }
        else
        {
            Console.WriteLine("Usage: wowup install [addon]");
        }
        break;
    case "remove":
        if (args.Length > 1)
        {
            AddOnManager.Remove(args[1]);
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
