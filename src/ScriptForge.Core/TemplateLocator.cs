namespace ScriptForge.Core;

internal static class TemplateLocator
{
    /// <summary>
    /// Finds the "Launcher" template directory. Looks next to the running
    /// executable first (packaged layout), then walks up from the base
    /// directory looking for a repo-style "templates/Launcher" (dev layout).
    /// </summary>
    public static string LocateLauncherTemplate()
    {
        var baseDir = AppContext.BaseDirectory;

        var packaged = Path.Combine(baseDir, "templates", "Launcher");
        if (Directory.Exists(packaged))
        {
            return packaged;
        }

        var dir = new DirectoryInfo(baseDir);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "templates", "Launcher");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate the 'templates/Launcher' directory (looked next to the executable and in parent directories).");
    }
}
