namespace wowup.Commands;

public sealed class Remove(AddOnDatabase db, string addOnsFolder)
{
    public async Task Execute(string text)
    {
        var installation = await db.GetLatestInstallation(text);
        if (installation is null)
        {
            Console.WriteLine("{0} is not installed", text);
            return;
        }

        bool anyRemoved = false;
        foreach (var item in installation.Content)
        {
            var dir = Path.Combine(addOnsFolder, item.DirName);
            if (Directory.Exists(dir))
            {
                Console.WriteLine("Removing {0}...", item.DirName);
                Directory.Delete(dir, recursive: true);
                anyRemoved = true;
            }
        }

        if (anyRemoved)
        {
            Console.WriteLine("{0} uninstalled", text);
        }
        else
        {
            Console.WriteLine("{0} is not installed", text);
        }
    }
}