using System.Collections.Generic;
using System.Diagnostics;
using Gum.Plugins.InternalPlugins.EditorTab.Services;

namespace Gum.Presentation.Tests;

/// <summary>A stand-in Preview process: records each start and counts closes, spawns nothing.</summary>
internal sealed class RecordingPreviewStarter : IPreviewProcessStarter, IPreviewProcess
{
    private readonly string _executablePath;

    public RecordingPreviewStarter(string executablePath)
    {
        _executablePath = executablePath;
    }

    public List<ProcessStartInfo> Started { get; } = new List<ProcessStartInfo>();

    public bool HasExited { get; set; }

    public int CloseCount { get; private set; }

    public int ActivateCount { get; private set; }

    public void Activate() => ActivateCount++;

    public void Close()
    {
        CloseCount++;
        HasExited = true;
    }

    public ResolvedPreviewExecutable? Resolve() => new ResolvedPreviewExecutable(_executablePath, IsNativeAot: false);

    public IPreviewProcess? Start(ProcessStartInfo startInfo)
    {
        Started.Add(startInfo);
        return this;
    }
}
