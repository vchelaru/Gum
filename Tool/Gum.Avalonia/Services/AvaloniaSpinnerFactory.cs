using Gum.Avalonia.Shell;
using Gum.Commands;
using Gum.Services;

namespace Gum.Avalonia.Services;

/// <summary>
/// Creates <see cref="ISpinner"/>s that report progress in the shell's status bar rather than in
/// a floating window, so nothing steals focus during font generation.
/// </summary>
public class AvaloniaSpinnerFactory : ISpinnerFactory
{
    private readonly ShellViewModel _shell;
    private readonly IDispatcher _dispatcher;

    /// <summary>Creates the factory over the shell it reports into.</summary>
    public AvaloniaSpinnerFactory(ShellViewModel shell, IDispatcher dispatcher)
    {
        _shell = shell;
        _dispatcher = dispatcher;
    }

    /// <inheritdoc/>
    public ISpinner Create() => new StatusBarSpinner(_shell, _dispatcher);

    private sealed class StatusBarSpinner : ISpinner
    {
        private readonly ShellViewModel _shell;
        private readonly IDispatcher _dispatcher;
        private int _total;
        private int _completed;

        public StatusBarSpinner(ShellViewModel shell, IDispatcher dispatcher)
        {
            _shell = shell;
            _dispatcher = dispatcher;
            _total = 0;
            _completed = 0;
            Report();
        }

        public void SetTotal(int total)
        {
            _total = total;
            _completed = 0;
            Report();
        }

        public void IncrementProgress()
        {
            _completed++;
            Report();
        }

        public void Hide() => _dispatcher.Post(() => _shell.ProgressText = "");

        private void Report()
        {
            string text = _total > 0 ? $"Working... {_completed}/{_total}" : "Working...";
            _dispatcher.Post(() => _shell.ProgressText = text);
        }
    }
}
