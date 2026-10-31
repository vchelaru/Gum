using Gum.Dialogs;

namespace Gum.Avalonia.Tests.Harness;

/// <summary>
/// Fails a read of New Project state that the folder checks derive (banners, the refusal, the folder notice, whether OK is on)
/// while a checks refresh has started since the last <see cref="NewProjectUiFixture.Settle"/>. The check runs on a worker
/// thread and its result lands on the UI thread later, so such a read passes on a fast machine and fails on a slow one;
/// the guard makes it fail everywhere, every time, whether or not the check has already finished.
/// </summary>
internal sealed class AsyncStateGuard
{
    private readonly NewProjectDialogViewModel _viewModel;
    private Task? _settled;
    private int _suppressed;

    public AsyncStateGuard(NewProjectDialogViewModel viewModel) => _viewModel = viewModel;

    /// <summary>Whether the checks started by the latest input change were waited for.</summary>
    public bool IsSettled => _suppressed > 0 || ReferenceEquals(_settled, _viewModel.ChecksRefresh);

    /// <summary>Records that the checks for the current inputs finished and were applied.</summary>
    public void MarkSettled() => _settled = _viewModel.ChecksRefresh;

    /// <summary>Lets reads through until the returned scope is disposed; waiting loops read state on purpose.</summary>
    public IDisposable Suppress()
    {
        _suppressed++;
        return new Scope(this);
    }

    /// <summary>Throws when <paramref name="what"/> would be read from state the checks may still change.</summary>
    public void Check(string what)
    {
        if (IsSettled)
        {
            return;
        }

        string running = _viewModel.ChecksRefresh.IsCompleted ? "finished but was not waited for" : "is still running";
        throw new InvalidOperationException(
            $"[{CallingTestLine()}] " +
            $"Read of {what} after an input change (Name \"{_viewModel.ProjectName}\", Location \"{_viewModel.Location}\") without waiting for the folder checks; " +
            $"the check {running}. Call fixture.Settle() (or WaitUntil) after the action and before this read.");
    }

    // The first frame outside the harness: the line of the test that read the state.
    private static string CallingTestLine()
    {
        foreach (System.Diagnostics.StackFrame frame in new System.Diagnostics.StackTrace(fNeedFileInfo: true).GetFrames())
        {
            string? file = frame.GetFileName();
            if (file != null && !file.Contains("Harness") && file.Contains("Tests"))
            {
                return $"{Path.GetFileName(file)}:{frame.GetFileLineNumber()}";
            }
        }
        return "unknown test line";
    }

    private sealed class Scope : IDisposable
    {
        private AsyncStateGuard? _owner;

        public Scope(AsyncStateGuard owner) => _owner = owner;

        public void Dispose()
        {
            if (_owner != null)
            {
                _owner._suppressed--;
            }
            _owner = null;
        }
    }
}
