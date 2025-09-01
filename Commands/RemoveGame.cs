using System.CommandLine;

namespace wowup.Commands;

public sealed class RemoveGame(AddOnDatabase db)
{
    public static Command BuildCommand(AddOnDatabase db)
    {
        var command = new Command("remove", "Remove a game");
        var nameArg = new Argument<string>("name") { Description = "Name of game" };
        command.Arguments.Add(nameArg);
        command.SetAction(async pr =>
        {
            var name = pr.GetRequiredValue(nameArg);
            await new RemoveGame(db).Execute(name);
        });
        return command;
    }

    private async Task Execute(string name)
    {
        var existing = await db.GetGame(name);
        if (existing is null)
        {
            Console.WriteLine("Game {0} is not registered.", name);
            return;
        }

        await db.DeleteGame(name);
        Console.WriteLine("Game {0} removed", name);
    }
}