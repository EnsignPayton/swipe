using System.CommandLine;

namespace wowup.Commands;

public sealed class ListGames(AddOnDatabase db)
{
    public static Command BuildCommand(AddOnDatabase db)
    {
        var command = new Command("list", "List registered games");
        command.SetAction(async _ => await new ListGames(db).Execute());
        return command;
    }

    private async Task Execute()
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
                Console.WriteLine($"  {game.Name.PadRight(maxlen)}{pad}  {game.Path}");
            }
        }
    }
}