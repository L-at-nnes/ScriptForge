namespace ScriptForge.Core;

public sealed class BuildResult
{
    public required bool Success { get; init; }
    public required string OutputPath { get; init; }
    public string? Log { get; init; }
    public string? Error { get; init; }
}

public sealed class BuildException : Exception
{
    public BuildException(string message, string? log = null) : base(message) => Log = log;
    public string? Log { get; }
}
