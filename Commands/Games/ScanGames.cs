using System.Diagnostics;

namespace wowup.Commands.Games;

public sealed class ScanGames(AddOnDatabase db)
{
    public async Task Execute()
    {
        Console.WriteLine("Scanning...");
        var gamePaths = SearchFileSystem();
        if (gamePaths.Count == 0)
        {
            Console.WriteLine("No games found.");
            return;
        }

        var games = await db.GetAllGames();

        var registered = games.Where(x => gamePaths.Contains(x.Path)).ToList();
        if (registered.Count != 0)
        {
            Console.WriteLine("Registered Games:");
            var maxlen = registered.Max(x => x.Name.Length);
            foreach (var game in games.Where(x => gamePaths.Contains(x.Path)))
            {
                Console.WriteLine($"  {game.Name.PadRight(maxlen)}  {game.Path}");
            }

            Console.WriteLine();
        }

        var unregistered = gamePaths.Where(p => games.All(g => g.Path != p)).ToList();
        if (unregistered.Count != 0)
        {
            Console.WriteLine("Unregistered Games:");
            foreach (var path in unregistered)
            {
                Console.WriteLine($"  {path}");
            }
        }
    }

    private static List<string> SearchFileSystem()
    {
        var roots = new List<string>();
        if (OperatingSystem.IsWindows())
        {
            roots.Add(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
            roots.Add(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86));
        }
        else
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var wineDrives = FindDirectories(home, "drive_c");
            foreach (var wineDrive in wineDrives)
            {
                roots.Add(Path.Combine(wineDrive, "Program Files"));
                roots.Add(Path.Combine(wineDrive, "Program Files (x86)"));
            }
        }

        var result = new List<string>();

        foreach (var root in roots)
        {
            var wow = Path.Combine(root, "World of Warcraft");
            if (!Directory.Exists(wow)) continue;

            if (Directory.Exists(Path.Combine(wow, "Interface", "AddOns")))
                result.Add(wow);

            result.AddRange(Directory.EnumerateDirectories(wow)
                .Where(subWow => Directory.Exists(Path.Combine(subWow, "Interface", "AddOns"))));
        }

        return result;
    }

    private static List<string> FindDirectories(string root, string targetName)
    {
        var result = new List<string>();

        var startInfo = new ProcessStartInfo
        {
            FileName = "find",
            ArgumentList = { root, "-type", "d", "-name", targetName },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(startInfo);
        if (process is null) return result;

        process.WaitForExit();

        while (true)
        {
            var line = process.StandardOutput.ReadLine();
            if (line is null) break;
            result.Add(line.Trim());
        }

        return result;
    }
}