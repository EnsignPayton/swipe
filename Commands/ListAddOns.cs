namespace wowup.Commands;

public sealed class ListAddOns(AddOnDatabase db, string addOnsFolder)
{
    public async Task Execute()
    {
        var installations = await db.GetLatestInstallations();

        var anyInstalled = false;
        foreach (var installation in installations)
        {
            var allInstalled = true;
            var installed = Directory.GetDirectories(addOnsFolder);
            foreach (var content in installation.Content)
            {
                if (!installed.Any(x => x.EndsWith(content.DirName)))
                {
                    allInstalled = false;
                    break;
                }
            }

            if (allInstalled)
            {
                Console.WriteLine("{0} {1}", installation.Name, installation.Version);
                anyInstalled = true;
            }
        }

        if (!anyInstalled)
        {
            Console.WriteLine("No AddOns installed");
        }
    }
}