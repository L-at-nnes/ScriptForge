namespace ScriptForge.Core;

/// <summary>Mirrors ScriptForge.Launcher's PayloadManifest — kept in sync manually, no shared assembly.</summary>
internal sealed class PayloadManifest
{
    public string Entry { get; set; } = "";
    public string Kind { get; set; } = "ps1";
    public bool Console { get; set; } = true;
    public bool PauseOnExit { get; set; }
    public List<PayloadFileEntry> Files { get; set; } = new();
}

internal sealed class PayloadFileEntry
{
    public string Resource { get; set; } = "";
    public string RelativePath { get; set; } = "";
}
