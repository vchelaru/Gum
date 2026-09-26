using Gum.Services.Dialogs;

namespace Gum.Avalonia.Tests.Harness;

/// <summary>
/// The test container's <see cref="IDialogService"/>: forwards to the head's real service until a
/// harness installs a scripted one with <see cref="Use"/>, so services built once for the whole run
/// (the grid manager, set-variable logic, delete service) open scripted dialogs for that harness.
/// </summary>
internal sealed class SwitchableDialogService : IDialogService
{
    private readonly IDialogService _fallback;
    private IDialogService? _override;

    public SwitchableDialogService(IDialogService fallback)
    {
        _fallback = fallback;
    }

    private IDialogService Current => _override ?? _fallback;

    /// <summary>Routes every dialog to <paramref name="dialogs"/> until the returned handle is disposed.</summary>
    public IDisposable Use(IDialogService dialogs)
    {
        if (_override != null)
        {
            throw new InvalidOperationException("Another harness already scripts the tool's dialogs; dispose it first.");
        }
        _override = dialogs;
        return new Restore(this);
    }

    /// <inheritdoc/>
    public MessageDialogResult ShowMessage(string message, string? title = null, MessageDialogStyle? style = null) =>
        Current.ShowMessage(message, title, style);

    /// <inheritdoc/>
    public bool Show<T>(T dialogViewModel) where T : DialogViewModel => Current.Show(dialogViewModel);

    /// <inheritdoc/>
    public bool Show<T>(Action<T>? initializer, out T viewModel) where T : DialogViewModel =>
        Current.Show(initializer, out viewModel);

    /// <inheritdoc/>
    public string? GetUserString(string message, string? title = null, GetUserStringOptions? options = null) =>
        Current.GetUserString(message, title, options);

    /// <inheritdoc/>
    public List<string>? OpenFile(OpenFileDialogOptions? options = null) => Current.OpenFile(options);

    /// <inheritdoc/>
    public string? SaveFile(SaveFileDialogOptions? options = null) => Current.SaveFile(options);

    /// <inheritdoc/>
    public string? OpenFolder(OpenFolderDialogOptions? options = null) => Current.OpenFolder(options);

    private sealed class Restore : IDisposable
    {
        private SwitchableDialogService? _owner;

        public Restore(SwitchableDialogService owner)
        {
            _owner = owner;
        }

        public void Dispose()
        {
            if (_owner != null)
            {
                _owner._override = null;
                _owner = null;
            }
        }
    }
}
