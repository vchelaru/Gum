using System.Text;
using Gum.Avalonia.Diagnostics;
using Gum.Diagnostics;
using Gum.Managers;
using Gum.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// Records every way an exception can surface in the running tool without failing the gesture that
/// caused it: an error line in the Output tab, an exception the head's UI-thread hook would write
/// to the crash log, and a plugin the plugin manager disabled after it threw. A scenario starts the
/// watch before its first gesture and calls <see cref="AssertClean"/> at its end.
/// </summary>
internal sealed class ToolExceptionWatch : IDisposable
{
    private readonly MainOutputViewModel _output;
    private readonly PluginManager _pluginManager;
    private readonly StringBuilder _outputWritten;
    private readonly List<string> _outputErrors;
    private string _outputText;
    private string _lastOutputEntry;
    private readonly RecordingCrashReporter _crashReporter;
    private readonly IDisposable _dispatcherHook;
    private readonly HashSet<PluginContainer> _enabledAtStart;

    public ToolExceptionWatch()
    {
        IServiceProvider services = TestAppBuilder.Services;
        _output = services.GetRequiredService<MainOutputViewModel>();
        _pluginManager = services.GetRequiredService<PluginManager>();
        _outputText = _output.OutputText;
        _outputWritten = new StringBuilder();
        _lastOutputEntry = string.Empty;
        _outputErrors = new List<string>();
        _output.PropertyChanged += HandleOutputChanged;
        _output.ErrorAdded += HandleErrorAdded;
        _crashReporter = new RecordingCrashReporter();
        // The head installs this hook at startup: an exception in a posted job (a menu action runs
        // as one) is logged and swallowed rather than ending the process.
        _dispatcherHook = UnhandledExceptionHooks.InstallDispatcherHook(_crashReporter);
        _enabledAtStart = _pluginManager.PluginContainers.Values.Where(container => container.IsEnabled).ToHashSet();
    }

    /// <summary>
    /// Throws at once when a gesture crashed (an exception the head would log, or a disabled
    /// plugin), so the failure names the gesture rather than a later assertion it broke.
    /// </summary>
    public void ThrowIfCrashed()
    {
        if (_crashReporter.Reports.Count > 0 || _enabledAtStart.Any(container => !container.IsEnabled))
        {
            AssertClean();
        }
    }

    /// <summary>Throws when anything since the watch started logged an error or an exception.</summary>
    public void AssertClean()
    {
        StringBuilder problems = new StringBuilder();
        foreach (string error in _outputErrors)
        {
            problems.AppendLine($"  Output error: {error}");
        }
        foreach (string line in _outputWritten.ToString().Split('\n').Where(line => line.Contains("Exception", StringComparison.Ordinal)))
        {
            problems.AppendLine($"  Output line naming an exception: {line.Trim()}");
        }
        foreach ((Exception exception, string source) in _crashReporter.Reports)
        {
            problems.AppendLine($"  Crash log ({source}): {exception}");
        }
        foreach (PluginContainer container in _enabledAtStart.Where(container => !container.IsEnabled))
        {
            problems.AppendLine($"  Plugin disabled: {container.Name}: {container.FailureDetails}");
        }
        if (problems.Length > 0)
        {
            throw new Shouldly.ShouldAssertException($"The tool reported problems:{Environment.NewLine}{problems}");
        }
    }

    public void Dispose()
    {
        _output.PropertyChanged -= HandleOutputChanged;
        _output.ErrorAdded -= HandleErrorAdded;
        _dispatcherHook.Dispose();
    }

    // The Output text is shared by the whole run and drops its older half once it grows long, so
    // what this watch saw written is collected entry by entry rather than diffed at the end.
    private void HandleOutputChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MainOutputViewModel.OutputText))
        {
            return;
        }
        string text = _output.OutputText;
        if (text.StartsWith(_outputText, StringComparison.Ordinal))
        {
            _lastOutputEntry = text.Substring(_outputText.Length);
        }
        else
        {
            // Trimmed or cleared: the newest entry starts at the last timestamp.
            int lastEntry = text.LastIndexOf("\n[", StringComparison.Ordinal);
            _lastOutputEntry = lastEntry >= 0 ? text.Substring(lastEntry) : text;
        }
        _outputWritten.Append(_lastOutputEntry);
        _outputText = text;
    }

    // ErrorAdded carries no text; the error is the entry just written.
    private void HandleErrorAdded() => _outputErrors.Add(_lastOutputEntry.Trim());

    private sealed class RecordingCrashReporter : ICrashReporter
    {
        public RecordingCrashReporter()
        {
            Reports = new List<(Exception, string)>();
        }

        public List<(Exception Exception, string Source)> Reports { get; }

        public string DirectoryPath => Path.GetTempPath();

        public string? ReportRecoverable(Exception exception, string source)
        {
            Reports.Add((exception, source));
            return null;
        }

        public string? ReportFatal(Exception exception, string source)
        {
            Reports.Add((exception, source));
            return null;
        }

        public void PromptForPreviousCrash()
        {
        }
    }
}
