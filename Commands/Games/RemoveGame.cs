namespace wowup.Commands.Games;

public sealed class RemoveGame(AddOnDatabase db)
{
    public async Task Execute(string name)
    {
        var existing = await db.GetGame(name);
        if (existing is null)
        {
            Console.WriteLine($"Game {name} is not registered.");
            return;
        }

        await db.DeleteGame(name);
        Console.WriteLine($"Game {name} removed");
    }
}