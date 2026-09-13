namespace ScriptForge.Core;

/// <summary>
/// Everything needed to turn a .ps1/.bat script (or a folder of them) into a
/// standalone Windows executable.
/// </summary>
public sealed class BuildOptions
{
    /// <summary>Path to a single script file, or a folder containing one or more scripts.</summary>
    public required string InputPath { get; init; }

    /// <summary>Path to the .exe to produce.</summary>
    public required string OutputPath { get; init; }

    /// <summary>
    /// When <see cref="InputPath"/> is a folder, the path of the entry script
    /// relative to that folder. Required if the folder contains more than one
    /// top-level .ps1/.bat file.
    /// </summary>
    public string? EntryRelativePath { get; init; }

    /// <summary>Path to a .ico file used as the executable's icon. Optional.</summary>
    public string? IconPath { get; init; }

    /// <summary>File/product version, e.g. "1.2.3.0". Defaults to 1.0.0.0.</summary>
    public string Version { get; init; } = "1.0.0.0";

    public string? Product { get; init; }
    public string? Company { get; init; }
    public string? Copyright { get; init; }
    public string? Description { get; init; }

    /// <summary>If true, requests elevation (UAC) via requireAdministrator.</summary>
    public bool RequireAdmin { get; init; }

    /// <summary>If true, the exe keeps a console window and prints script output/errors to it.
    /// If false, it runs windowed with no console (GUI mode); failures are shown in a message box.</summary>
    public bool ShowConsole { get; init; } = true;

    /// <summary>When <see cref="ShowConsole"/> is true, wait for a key press before the console
    /// window closes, so double-clicking the exe doesn't just flash a window and vanish.</summary>
    public bool PauseOnExit { get; init; }

    /// <summary>Optional progress/log sink.</summary>
    public IProgress<string>? Progress { get; init; }
}
