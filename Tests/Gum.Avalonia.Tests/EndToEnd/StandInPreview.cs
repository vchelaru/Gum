using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Gum.Plugins.InternalPlugins.EditorTab.Services;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>Stands in for GumPreview: records each launch and reports itself running until told otherwise.</summary>
internal sealed class StandInPreview : IPreviewProcessStarter, IPreviewProcess
{
    public List<ProcessStartInfo> Started { get; } = new List<ProcessStartInfo>();

    public bool HasExited { get; set; }

    public int CloseCount { get; private set; }

    public void Close()
    {
        CloseCount++;
        HasExited = true;
    }

    public ResolvedPreviewExecutable? Resolve() => new ResolvedPreviewExecutable("/Preview/GumPreview", IsNativeAot: false);

    public IPreviewProcess? Start(ProcessStartInfo startInfo)
    {
        Started.Add(startInfo);
        HasExited = false;
        return this;
    }

    public string Argument(int launch, string name)
    {
        List<string> arguments = Started[launch].ArgumentList.ToList();
        return arguments[arguments.IndexOf(name) + 1];
    }

    public PreviewSelectionMessage Selection(int launch) =>
        PreviewSelectionMessage.TryParse(File.ReadAllLines(Argument(launch, "--selection-file")))
        ?? throw new InvalidOperationException("The selection file holds no selection.");

    /// <summary>The selection most recently written for the latest launch.</summary>
    public PreviewSelectionMessage LastPushed() => Selection(Started.Count - 1);

    public void DeleteSelectionFile()
    {
        if (Started.Count > 0)
        {
            File.Delete(Argument(Started.Count - 1, "--selection-file"));
        }
    }

    public void DeleteSelectionFiles()
    {
        for (int launch = 0; launch < Started.Count; launch++)
        {
            File.Delete(Argument(launch, "--selection-file"));
        }
    }
}
