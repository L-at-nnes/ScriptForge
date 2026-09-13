using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace ScriptForge.Core;

public static class BuildEngine
{
    private static readonly string[] Ps1Extensions = { ".ps1" };
    private static readonly string[] BatExtensions = { ".bat", ".cmd" };
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    public static async Task<BuildResult> BuildAsync(BuildOptions options, CancellationToken ct = default)
    {
        var progress = options.Progress;
        string buildDir = CreateTempDir();

        try
        {
            progress?.Report("Resolving input...");
            var input = ResolveInput(options);

            progress?.Report("Preparing launcher project...");
            var templateDir = TemplateLocator.LocateLauncherTemplate();
            CopyDirectory(templateDir, buildDir);

            var payloadDir = Path.Combine(buildDir, "Payload");
            Directory.CreateDirectory(payloadDir);

            progress?.Report("Embedding script payload...");
            var manifest = BuildPayload(input, payloadDir);
            manifest.Console = options.ShowConsole;
            File.WriteAllText(
                Path.Combine(payloadDir, "manifest.json"),
                JsonSerializer.Serialize(manifest, JsonOptions));

            var manifestXmlPath = Path.Combine(
                buildDir,
                options.RequireAdmin ? "app.admin.manifest" : "app.asinvoker.manifest");

            var assemblyName = Path.GetFileNameWithoutExtension(options.OutputPath);
            var version = NormalizeVersion(options.Version);
            var publishDir = Path.Combine(buildDir, "publish");
            var csprojPath = Path.Combine(buildDir, "Launcher.csproj");

            progress?.Report("Compiling executable (dotnet publish)...");
            var log = await RunDotnetPublish(
                csprojPath, publishDir, assemblyName, version, manifestXmlPath, options, ct);

            var producedExe = Path.Combine(publishDir, assemblyName + ".exe");
            if (!File.Exists(producedExe))
            {
                throw new BuildException(
                    $"Build finished but expected output '{producedExe}' was not found.", log);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(options.OutputPath))!);
            File.Copy(producedExe, options.OutputPath, overwrite: true);

            progress?.Report("Done.");
            return new BuildResult { Success = true, OutputPath = options.OutputPath, Log = log };
        }
        catch (BuildException ex)
        {
            return new BuildResult
            {
                Success = false,
                OutputPath = options.OutputPath,
                Error = ex.Message,
                Log = ex.Log,
            };
        }
        catch (Exception ex)
        {
            return new BuildResult { Success = false, OutputPath = options.OutputPath, Error = ex.Message };
        }
        finally
        {
            TryDeleteDirectory(buildDir);
        }
    }

    private readonly record struct ResolvedInput(string Root, string EntryRelativePath, string Kind, IReadOnlyList<string> AllFiles);

    private static ResolvedInput ResolveInput(BuildOptions options)
    {
        var input = options.InputPath;

        if (File.Exists(input))
        {
            var kind = DetectKind(input);
            var name = Path.GetFileName(input);
            return new ResolvedInput(Path.GetDirectoryName(Path.GetFullPath(input))!, name, kind, new[] { name });
        }

        if (!Directory.Exists(input))
        {
            throw new BuildException($"Input path '{input}' does not exist.");
        }

        var root = Path.GetFullPath(input);
        var allFiles = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Where(f => !IsInBinOrObj(root, f))
            .Select(f => Path.GetRelativePath(root, f))
            .ToList();

        if (allFiles.Count == 0)
        {
            throw new BuildException($"Folder '{input}' does not contain any files.");
        }

        string entry;
        if (!string.IsNullOrWhiteSpace(options.EntryRelativePath))
        {
            entry = options.EntryRelativePath.Replace('\\', '/');
            if (!allFiles.Any(f => f.Replace('\\', '/') == entry))
            {
                throw new BuildException($"Entry script '{entry}' was not found under '{input}'.");
            }
        }
        else
        {
            var candidates = allFiles
                .Where(f => IsScript(f) && !f.Contains(Path.DirectorySeparatorChar) && !f.Contains('/'))
                .ToList();

            if (candidates.Count == 0)
            {
                throw new BuildException(
                    $"No .ps1/.bat/.cmd file found at the top level of '{input}'. Specify --entry.");
            }
            if (candidates.Count > 1)
            {
                throw new BuildException(
                    $"Multiple candidate entry scripts found ({string.Join(", ", candidates)}). Specify --entry.");
            }
            entry = candidates[0];
        }

        var entryKind = DetectKind(entry);
        return new ResolvedInput(root, entry, entryKind, allFiles);
    }

    private static bool IsInBinOrObj(string root, string fullPath)
    {
        var rel = Path.GetRelativePath(root, fullPath).Replace('\\', '/');
        return rel.StartsWith("bin/", StringComparison.OrdinalIgnoreCase)
            || rel.StartsWith("obj/", StringComparison.OrdinalIgnoreCase)
            || rel.StartsWith(".git/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsScript(string path) =>
        Ps1Extensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase)
        || BatExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

    private static string DetectKind(string path)
    {
        var ext = Path.GetExtension(path);
        if (Ps1Extensions.Contains(ext, StringComparer.OrdinalIgnoreCase)) return "ps1";
        if (BatExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase)) return "bat";
        throw new BuildException($"Unsupported script extension '{ext}'. Expected .ps1, .bat or .cmd.");
    }

    private static PayloadManifest BuildPayload(ResolvedInput input, string payloadDir)
    {
        var manifest = new PayloadManifest
        {
            Entry = input.EntryRelativePath.Replace('\\', '/'),
            Kind = input.Kind,
        };

        int index = 0;
        foreach (var relativePath in input.AllFiles)
        {
            var sourcePath = Path.Combine(input.Root, relativePath);
            var sanitizedName = $"{index}__{SanitizeFileName(Path.GetFileName(relativePath))}";
            File.Copy(sourcePath, Path.Combine(payloadDir, sanitizedName), overwrite: true);

            manifest.Files.Add(new PayloadFileEntry
            {
                Resource = sanitizedName,
                RelativePath = relativePath.Replace('\\', '/'),
            });
            index++;
        }

        return manifest;
    }

    private static string SanitizeFileName(string name)
    {
        var sb = new StringBuilder(name.Length);
        foreach (var c in name)
        {
            sb.Append(char.IsLetterOrDigit(c) || c is '.' or '-' or '_' ? c : '_');
        }
        return sb.ToString();
    }

    private static string NormalizeVersion(string version)
    {
        var parts = version.Split('.');
        var numeric = parts.Select(p => int.TryParse(p, out var n) ? n : 0).ToList();
        while (numeric.Count < 4) numeric.Add(0);
        return string.Join('.', numeric.Take(4));
    }

    private static async Task<string> RunDotnetPublish(
        string csprojPath,
        string publishDir,
        string assemblyName,
        string version,
        string manifestXmlPath,
        BuildOptions options,
        CancellationToken ct)
    {
        var args = new List<string>
        {
            "publish", $"\"{csprojPath}\"",
            "-c", "Release",
            "-r", "win-x64",
            "--self-contained", "true",
            "-o", $"\"{publishDir}\"",
            "--nologo",
            "-v", "minimal",
            $"-p:AssemblyName={assemblyName}",
            $"-p:Version={version}",
            $"-p:FileVersion={version}",
            $"-p:AssemblyVersion={version}",
            $"-p:OutputType={(options.ShowConsole ? "Exe" : "WinExe")}",
            $"-p:ApplicationManifest=\"{manifestXmlPath}\"",
        };

        if (!string.IsNullOrWhiteSpace(options.Product)) args.Add($"-p:Product=\"{Escape(options.Product)}\"");
        if (!string.IsNullOrWhiteSpace(options.Company)) args.Add($"-p:Company=\"{Escape(options.Company)}\"");
        if (!string.IsNullOrWhiteSpace(options.Copyright)) args.Add($"-p:Copyright=\"{Escape(options.Copyright)}\"");
        if (!string.IsNullOrWhiteSpace(options.Description))
        {
            // Win32 "File description" comes from AssemblyTitle, not the NuGet-style "Description" property.
            args.Add($"-p:AssemblyTitle=\"{Escape(options.Description)}\"");
            args.Add($"-p:Description=\"{Escape(options.Description)}\"");
        }

        if (!string.IsNullOrWhiteSpace(options.IconPath))
        {
            if (!Path.GetExtension(options.IconPath).Equals(".ico", StringComparison.OrdinalIgnoreCase))
            {
                throw new BuildException("Icon must be a .ico file.");
            }
            if (!File.Exists(options.IconPath))
            {
                throw new BuildException($"Icon file '{options.IconPath}' does not exist.");
            }
            args.Add($"-p:ApplicationIcon=\"{Path.GetFullPath(options.IconPath)}\"");
        }

        var psi = new ProcessStartInfo("dotnet")
        {
            Arguments = string.Join(' ', args),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var proc = Process.Start(psi)!;
        var stdout = await proc.StandardOutput.ReadToEndAsync(ct);
        var stderr = await proc.StandardError.ReadToEndAsync(ct);
        await proc.WaitForExitAsync(ct);

        var log = stdout + Environment.NewLine + stderr;
        options.Progress?.Report(log);

        if (proc.ExitCode != 0)
        {
            throw new BuildException("dotnet publish failed.", log);
        }

        return log;
    }

    private static string Escape(string value) => value.Replace("\"", "\\\"");

    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "ScriptForge-build", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static void CopyDirectory(string sourceDir, string destDir)
    {
        foreach (var dir in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
        {
            var name = Path.GetRelativePath(sourceDir, dir);
            if (name.StartsWith("bin", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("obj", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            Directory.CreateDirectory(Path.Combine(destDir, name));
        }

        foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(sourceDir, file);
            if (rel.StartsWith("bin", StringComparison.OrdinalIgnoreCase) ||
                rel.StartsWith("obj", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            var destPath = Path.Combine(destDir, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
            File.Copy(file, destPath, overwrite: true);
        }
    }

    private static void TryDeleteDirectory(string dir)
    {
        try { Directory.Delete(dir, recursive: true); }
        catch { /* best effort */ }
    }
}
