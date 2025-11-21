namespace Th11s.ACMEServer.ConfigCLI;

internal class CLIPrompt
{
    public static bool Bool(string message)
    {
        bool? result = null;
        while (!result.HasValue)
        {
            Console.Write($"{message} ([y]es/[n]o): ");
            var readKey = Console.ReadKey();
            Console.WriteLine();
            if (readKey.Key == ConsoleKey.Y)
            {
                result = true;
            }
            else if (readKey.Key == ConsoleKey.N || readKey.Key == ConsoleKey.Escape)
            {
                result = false;
            }
        }


        return result.Value;
    }

    public static string String(string message)
    {
        Console.Write($"{message}: ");
        var result = Console.ReadLine() ?? string.Empty;

        Console.WriteLine();
        return result;
    }

    public static List<string> StringList(string message, List<string> initialList)
    {
        var list = new List<string>(initialList);
        bool running = true;
        while (running)
        {
            Console.WriteLine($"{message}");
            if (list.Count == 0)
            {
                Console.WriteLine("<empty>");
            }
            else
            {
                for (int i = 0; i < list.Count; i++)
                {
                    Console.WriteLine($"  [{i}] {list[i]}");
                }
            }

            Console.WriteLine("Options: [A]dd, [R]emove, [Enter] finish");
            Console.Write("Select option: ");
            var key = Console.ReadKey();
            Console.WriteLine();
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                running = false;
                continue;
            }
            else if (key.Key == ConsoleKey.A)
            {
                var newEntry = String("Enter new value");
                if (!string.IsNullOrWhiteSpace(newEntry))
                    list.Add(newEntry);
            }
            else if (key.Key == ConsoleKey.R)
            {
                Console.Write("Enter index to remove: ");
                var idxStr = Console.ReadLine();
                if (int.TryParse(idxStr, out int idx) && idx >= 0 && idx < list.Count)
                    list.RemoveAt(idx);
            }
            // Unknown options are ignored silently
        }
        return list;
    }
}
