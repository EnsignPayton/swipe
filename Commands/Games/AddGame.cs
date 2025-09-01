namespace wowup.Commands.Games;

public sealed class AddGame(AddOnDatabase db)
{
    public async Task Execute(string name, string path)
    {
        if (!Directory.Exists(path))
        {
            Console.WriteLine($"Directory {path} not found.");
            return;
        }

        var existing = await db.GetGame(name);
        if (existing is not null)
        {
            Console.WriteLine($"Game {name} already exists at {existing.Path}");
            return;
        }

        await db.AddGame(new wowup.Game { Name = name, Path = path });
        Console.WriteLine($"Game {name} added at {path}");
    }
}