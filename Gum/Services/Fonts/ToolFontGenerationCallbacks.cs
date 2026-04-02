using Gum.Commands;
using Gum.Controls;
using Gum.Logic.FileWatch;
using Gum.ProjectServices.FontGeneration;
using System;
using ToolsUtilities;

namespace Gum.Services.Fonts;

/// <summary>
/// Tool-specific implementation of <see cref="IFontGenerationCallbacks"/> that routes output
/// to <see cref="IGuiCommands"/> and file-change suppression to <see cref="IFileWatchManager"/>.
/// </summary>
public class ToolFontGenerationCallbacks : IFontGenerationCallbacks
{
    private readonly IGuiCommands _guiCommands;
    private readonly IFileWatchManager _fileWatchManager;
    private Spinner? _currentSpinner;

    public ToolFontGenerationCallbacks(IGuiCommands guiCommands, IFileWatchManager fileWatchManager)
    {
        _guiCommands = guiCommands;
        _fileWatchManager = fileWatchManager;
    }

    /// <inheritdoc/>
    public void OnOutput(string message) => _guiCommands.PrintOutput(message);

    /// <inheritdoc/>
    public IDisposable? ShowSpinner()
    {
        Spinner spinner = _guiCommands.ShowSpinner();
        _currentSpinner = spinner;
        return new SpinnerHandle(this, spinner);
    }

    /// <inheritdoc/>
    public void OnFontProgress(int completed, int total)
    {
        Spinner? spinner = _currentSpinner;
        if (spinner == null)
        {
            return;
        }

        if (completed == 0)
        {
            // SetTotal must run on the UI thread; use Invoke (synchronous) so the bar
            // is fully initialized before any IncrementProgress calls arrive.
            spinner.Dispatcher.BeginInvoke(() => spinner.SetTotal(total));
        }
        else
        {
            spinner.IncrementProgress();
        }
    }

    /// <inheritdoc/>
    public void OnIgnoreFileChange(FilePath filePath) => _fileWatchManager.IgnoreNextChangeUntil(filePath);

    private sealed class SpinnerHandle : IDisposable
    {
        private readonly ToolFontGenerationCallbacks _owner;
        private readonly Spinner _spinner;

        public SpinnerHandle(ToolFontGenerationCallbacks owner, Spinner spinner)
        {
            _owner = owner;
            _spinner = spinner;
        }

        public void Dispose()
        {
            _owner._currentSpinner = null;
            _spinner.Hide();
        }
    }
}
