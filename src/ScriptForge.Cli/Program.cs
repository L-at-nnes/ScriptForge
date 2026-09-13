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
        bool admin = false, gui = false;

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--in": input = args[++i]; break;
                case "--out": output = args[++i]; break;
                case "--entry": entry = args[++i]; break;
                case "--icon": icon = args[++i]; break;
                case "--version": version = args[++i]; break;
                case "--product": product = args[++i]; break;
                case "--company": company = args[++i]; break;
                case "--copyright": copyright = args[++i]; break;
                case "--description": description = args[++i]; break;
                case "--admin": admin = true; break;
                case "--gui": gui = true; break;
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
            Progress = new Progress<string>(line => Console.WriteLine(line)),
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
        """);
    }
}
