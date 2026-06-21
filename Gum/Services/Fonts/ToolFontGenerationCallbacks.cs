using Gum.Commands;
using Gum.Logic.FileWatch;
using Gum.ProjectServices.FontGeneration;
using System;
using ToolsUtilities;

namespace Gum.Services.Fonts;

/// <summary>
/// Tool-specific implementation of <see cref="IFontGenerationCallbacks"/> that routes output
/// to <see cref="IGuiCommands"/> and file-change suppression to <see cref="IFileWatchIgnoreList"/>.
/// </summary>
public class ToolFontGenerationCallbacks : IFontGenerationCallbacks
{
    private readonly IGuiCommands _guiCommands;
    private readonly IFileWatchIgnoreList _fileWatchIgnoreList;
    private ISpinner? _currentSpinner;

    public ToolFontGenerationCallbacks(IGuiCommands guiCommands, IFileWatchIgnoreList fileWatchIgnoreList)
    {
        _guiCommands = guiCommands;
        _fileWatchIgnoreList = fileWatchIgnoreList;
    }

    /// <inheritdoc/>
    public void OnOutput(string message) => _guiCommands.PrintOutput(message);

    /// <inheritdoc/>
    public IDisposable? ShowSpinner()
    {
        ISpinner spinner = _guiCommands.ShowSpinner();
        _currentSpinner = spinner;
        return new SpinnerHandle(this, spinner);
    }

    /// <inheritdoc/>
    public void OnFontProgress(int completed, int total)
    {
        ISpinner? spinner = _currentSpinner;
        if (spinner == null)
        {
            return;
        }

        if (completed == 0)
        {
            spinner.SetTotal(total);
        }
        else
        {
            spinner.IncrementProgress();
        }
    }

    /// <inheritdoc/>
    public void OnIgnoreFileChange(FilePath filePath) => _fileWatchIgnoreList.IgnoreNextChangeUntil(filePath);

    private sealed class SpinnerHandle : IDisposable
    {
        private readonly ToolFontGenerationCallbacks _owner;
        private readonly ISpinner _spinner;

        public SpinnerHandle(ToolFontGenerationCallbacks owner, ISpinner spinner)
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
