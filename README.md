# ScriptForge

Convert PowerShell (`.ps1`) and batch (`.bat`/`.cmd`) scripts into standalone Windows `.exe` files — CLI and GUI.

## How it works

Each build compiles a small, real C# host ("Launcher") via `dotnet publish` — it is **not** a
runtime-generated assembly (unlike classic PS2EXE), which keeps the resulting executable
clean and avoids the in-memory-compilation pattern most AV heuristics flag. The script(s) are
embedded as resources inside that host. At runtime the host extracts them to a temp folder and
runs the entry script through the system's `powershell.exe` (or `cmd.exe` for batch), so any
script — including PowerShell WPF GUI apps — behaves exactly as it would run natively.

A folder of scripts is supported: all files are embedded preserving their relative layout, so
`Import-Module`/dot-sourcing relative to `$PSScriptRoot` keeps working.

## Projects

- `src/ScriptForge.Core` — build engine (`BuildEngine.BuildAsync`)
- `src/ScriptForge.Cli` — command line front-end
- `src/ScriptForge.Gui` — WPF front-end
- `templates/Launcher` — the host template compiled on every build

## CLI

```
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
```

## Requirements

- Windows, with the .NET SDK installed on the machine building the executable (the SDK is not
  needed to *run* the produced .exe — it is self-contained).
- `powershell.exe` on the target machine to run `.ps1` scripts (present on all supported Windows
  versions).

## Notes / limitations

- Icons must be `.ico` files.
- Output executables are self-contained (`win-x64`), so they run without a separate .NET install,
  at the cost of a larger file size (~100 MB) — a deliberate trade-off to keep the executable a
  single, "normal" self-contained .NET app rather than a compressed/packed one.
