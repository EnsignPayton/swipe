using System.CommandLine;

namespace wowup.Commands;

public sealed class SetGame(AddOnDatabase db)
{
    public static Command BuildCommand(AddOnDatabase db)
    {
        var nameArg = new Argument<string>("name") { Description = "Name of game" };
        var command = new Command("set", "Set a game as current");
        command.Arguments.Add(nameArg);
        command.SetAction(async pr =>
        {
            var name = pr.GetRequiredValue(nameArg);
            await new SetGame(db).Execute(name);
        });
        return command;
    }

    private async Task Execute(string name)
    {
        var game = await db.GetGame(name);
        if (game is null)
        {
            Console.WriteLine($"Game {game} is not registered.");
            return;
        }

        await db.SaveCurrentGame(name);
        Console.WriteLine($"Game {name} set as current.");
    }
}