namespace wowup.Commands.Games;

public sealed class SetGame(AddOnDatabase db)
{
    public async Task Execute(string name)
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