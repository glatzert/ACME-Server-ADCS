using Th11s.ACMEServer.ConfigCLI;

namespace Th11s.ACMEServer.CLI;

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
        do
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

            Console.WriteLine("Options: [+]Add, [-]Remove, [Enter] finish");
            Console.Write("Select option: ");
            var readKey = Console.ReadKey();
            Console.WriteLine();
            if (readKey.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                running = false;
            }
            else if (readKey.KeyChar == '+')
            {
                var newEntry = String("Enter new value");
                if (!string.IsNullOrWhiteSpace(newEntry))
                    list.Add(newEntry);
            }
            else if (readKey.KeyChar == '-')
            {
                Console.Write("Enter index to remove: ");
                var idxStr = Console.ReadLine();
                if (int.TryParse(idxStr, out int idx) && idx >= 0 && idx < list.Count)
                    list.RemoveAt(idx);
            }
            // Unknown options are ignored silently
        } while (running);

        return list;
    }

    public static T? Select<T>(string message, IList<T> options, Func<T, string> display)
    {
        if (options == null || options.Count == 0)
            throw new ArgumentException("Options list must not be empty.", nameof(options));

        int? selectedIdx = null;
        do
        {
            Console.WriteLine($"{message}");
            for (int i = 0; i < options.Count; i++)
            {
                Console.WriteLine($"  [{i+1}] {display(options[i])}");
            }
            Console.Write("Enter index of selection (empty to cancel): ");
            var input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
            {
                return default;
            }
            if (int.TryParse(input, out int idx) && idx > 0 && idx <= options.Count)
            {
                selectedIdx = idx - 1;
            }
            else
            {
                Console.WriteLine("Invalid selection. Please enter a valid index.");
            }
        } while (!selectedIdx.HasValue);

        return options[selectedIdx.Value];
    }

    public static List<T> MultiSelect<T>(string message, IList<T> options, Func<T, string> display)
    {
        if (options == null || options.Count == 0)
            throw new ArgumentException("Options list must not be empty.", nameof(options));

        var selectedIndices = new List<int>();
        bool running = true;
         
        do
        {
            Console.WriteLine($"{message}");
            for (int i = 0; i < options.Count; i++)
            {
                var selectedMark = selectedIndices.Contains(i) ? "*" : " ";
                Console.WriteLine($"{selectedMark} [{i+1}] {display(options[i])}");
            }
            Console.Write("Enter index to select (empty to finish): ");
            var input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
            {
                running = false;
            }
            else if (int.TryParse(input, out int idx) && idx > 0 && idx <= options.Count)
            {
                int zeroIdx = idx - 1;
                if (!selectedIndices.Contains(zeroIdx))
                    selectedIndices.Add(zeroIdx);
            }
            else
            {
                Console.WriteLine("Invalid selection. Please enter a valid index.");
            }
        } while (running);

        return selectedIndices.Count == 0
            ? []
            : [..selectedIndices.Select(i => options[i])];
    }


}
