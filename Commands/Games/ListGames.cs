namespace Swipe.Commands.Games;

public sealed class ListGames(AddOnDatabase db)
{
    public async Task Execute()
    {
        var games = await db.GetAllGames();
        if (games.Count == 0)
        {
            Console.WriteLine("No registered games.");
        }
        else
        {
            Console.WriteLine("Registered Games:");
            var current = await db.GetCurrentGame();
            var maxlen = games.Max(x => x.Name.Length);
            foreach (var game in games)
            {
                var pad = game.Id == current?.Id ? "*" : " ";
                Console.WriteLine($"{pad} {game.Name.PadRight(maxlen)}  {game.Path}");
            }
        }
    }
}
