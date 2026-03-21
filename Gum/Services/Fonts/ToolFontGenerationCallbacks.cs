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
        return new SpinnerHandle(spinner);
    }

    /// <inheritdoc/>
    public void OnIgnoreFileChange(FilePath filePath) => _fileWatchManager.IgnoreNextChangeUntil(filePath);

    private sealed class SpinnerHandle : IDisposable
    {
        private readonly Spinner _spinner;

        public SpinnerHandle(Spinner spinner)
        {
            _spinner = spinner;
        }

        public void Dispose() => _spinner.Hide();
    }
}
