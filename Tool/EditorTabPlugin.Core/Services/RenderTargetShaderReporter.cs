using System;

namespace EditorTabPlugin_XNA.Services;

/// <summary>
/// Reports what the render-target shader resolver did to the Output tab. A compile that worked is
/// announced; a shader that stopped compiling and kept its last good version is reported through
/// <see cref="ReportError"/> and is not also announced as freshly compiled.
/// </summary>
internal sealed class RenderTargetShaderReporter
{
    private readonly Action<string> _printOutput;

    // Set by ReportError during one resolve.
    private bool _fellBackToLastGood;

    public RenderTargetShaderReporter(Action<string> printOutput)
    {
        _printOutput = printOutput;
    }

    /// <summary>Where a head's resolver reports a shader that stopped compiling.</summary>
    public void ReportError(string message)
    {
        _fellBackToLastGood = true;
        _printOutput(message);
    }

    /// <summary>
    /// Runs <paramref name="resolver"/> for <paramref name="absolutePath"/> and, when it produced an
    /// effect without falling back to a last good version, reports that it compiled. Failures keep
    /// propagating to the caller's existing error path.
    /// </summary>
    public object? ResolveAndReport(Func<string, object?>? resolver, string absolutePath)
    {
        _fellBackToLastGood = false;
        object? effect = resolver?.Invoke(absolutePath);
        if (effect != null && !_fellBackToLastGood)
        {
            _printOutput($"Compiled render-target shader for the preview:\n{absolutePath}");
        }
        return effect;
    }
}
