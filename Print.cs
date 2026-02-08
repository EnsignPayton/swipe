namespace Swipe;

public static class Print
{
    public static void Temp(string text, string? prefix = null)
    {
        OverwriteLine();

        if (prefix is not null)
        {
            WritePrefix(prefix);
        }

        Console.Write(text);
    }

    public static void Line(string text, string? prefix = null)
    {
        OverwriteLine();

        if (prefix is not null)
        {
            WritePrefix(prefix);
        }

        Console.WriteLine(text);
    }

    public static void List(IEnumerable<string> lines, int indent = 2)
    {
        OverwriteLine();

        foreach (var line in lines)
        {
            Console.Write(new string(' ', indent));
            Console.WriteLine(line);
        }
        
        Console.WriteLine();
    }

    private static void OverwriteLine()
    {
        var (left, top) = Console.GetCursorPosition();
        if (left != 0)
        {
            Console.SetCursorPosition(0, top);
            Console.Write(new string(' ', Console.BufferWidth));
            Console.SetCursorPosition(0, top);
        }
    }

    private static void WritePrefix(string prefix)
    {
        var fg = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write($"[{prefix}]  ");
        Console.ForegroundColor = fg;
    }
}
