using ScriptForge.Core;

namespace ScriptForge.Cli;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            PrintUsage();
            return args.Length == 0 ? 1 : 0;
        }

        if (args[0] != "build")
        {
            Console.Error.WriteLine($"Unknown command '{args[0]}'.");
            PrintUsage();
            return 1;
        }

        string? input = null, output = null, entry = null, icon = null;
        string version = "1.0.0.0", product = "", company = "", copyright = "", description = "";
        bool admin = false, gui = false, pause = false;

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--in": if (!TryNext(args, ref i, out input)) return 1; break;
                case "--out": if (!TryNext(args, ref i, out output)) return 1; break;
                case "--entry": if (!TryNext(args, ref i, out entry)) return 1; break;
                case "--icon": if (!TryNext(args, ref i, out icon)) return 1; break;
                case "--version": if (!TryNext(args, ref i, out version)) return 1; break;
                case "--product": if (!TryNext(args, ref i, out product)) return 1; break;
                case "--company": if (!TryNext(args, ref i, out company)) return 1; break;
                case "--copyright": if (!TryNext(args, ref i, out copyright)) return 1; break;
                case "--description": if (!TryNext(args, ref i, out description)) return 1; break;
                case "--admin": admin = true; break;
                case "--gui": gui = true; break;
                case "--pause": pause = true; break;
                default:
                    Console.Error.WriteLine($"Unknown option '{args[i]}'.");
                    return 1;
            }
        }

        if (input is null || output is null)
        {
            Console.Error.WriteLine("--in and --out are required.");
            PrintUsage();
            return 1;
        }

        var options = new BuildOptions
        {
            InputPath = input,
            OutputPath = output,
            EntryRelativePath = entry,
            IconPath = icon,
            Version = version,
            Product = product,
            Company = company,
            Copyright = copyright,
            Description = description,
            RequireAdmin = admin,
            ShowConsole = !gui,
            PauseOnExit = pause,
            Progress = new ConsoleProgress(),
        };

        var result = await BuildEngine.BuildAsync(options);
        if (!result.Success)
        {
            Console.Error.WriteLine($"Build failed: {result.Error}");
            if (result.Log is { Length: > 0 })
            {
                Console.Error.WriteLine(result.Log);
            }
            return 1;
        }

        Console.WriteLine($"Built '{result.OutputPath}'.");
        return 0;
    }

    private static bool TryNext(string[] args, ref int i, out string value)
    {
        if (i + 1 >= args.Length)
        {
            Console.Error.WriteLine($"Option '{args[i]}' requires a value.");
            value = "";
            return false;
        }
        value = args[++i];
        return true;
    }

    /// <summary>Writes synchronously, unlike Progress&lt;T&gt; which posts to the thread pool and can drop lines at exit.</summary>
    private sealed class ConsoleProgress : IProgress<string>
    {
        public void Report(string value) => Console.WriteLine(value);
    }

    private static void PrintUsage()
    {
        Console.WriteLine("""
        ScriptForge - convert PowerShell/batch scripts into standalone .exe files

        Usage:
          scriptforge build --in <file|folder> --out <file.exe> [options]

        Required:
          --in <path>             .ps1/.bat/.cmd file, or a folder of scripts
          --out <path>            output .exe path

        Options:
          --entry <relative>      entry script relative to --in, when --in is a folder with several scripts
          --icon <file.ico>       application icon
          --version <x.y.z.w>     file/product version (default 1.0.0.0)
          --product <text>        product name
          --company <text>        company/author name
          --copyright <text>      copyright string
          --description <text>    file description
          --admin                 require administrator privileges (UAC)
          --gui                   build a windowed app with no console window
          --pause                 wait for a key press before the console window closes
        """);
    }
}
