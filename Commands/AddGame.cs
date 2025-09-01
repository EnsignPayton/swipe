using System.CommandLine;

namespace wowup.Commands;

public sealed class AddGame(AddOnDatabase db)
{
    public static Command BuildCommand(AddOnDatabase db)
    {
        var nameArg = new Argument<string>("name") { Description = "Name of game" };
        var pathArg = new Argument<string>("path") { Description = "Path to game" };
        var command = new Command("add", "Add a game");
        command.Arguments.Add(nameArg);
        command.Arguments.Add(pathArg);
        command.SetAction(async pr =>
        {
            var name = pr.GetRequiredValue(nameArg);
            var path = pr.GetRequiredValue(pathArg);
            await new AddGame(db).Execute(name, path);
        });
        return command;
    }

    private async Task Execute(string name, string path)
    {
        if (!Directory.Exists(path))
        {
            Console.WriteLine("Directory {0} not found.", path);
            return;
        }

        var existing = await db.GetGame(name);
        if (existing is not null)
        {
            Console.WriteLine("Game {0} already exists at {1}", name, existing.Path);
            return;
        }

        await db.AddGame(new Game { Name = name, Path = path });
        Console.WriteLine("Game {0} added at {1}", name, path);
    }
}