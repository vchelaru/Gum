using System.Diagnostics;
using Avalonia.Threading;
using Gum.Avalonia.Dialogs;
using Gum.Services.Dialogs;
using Microsoft.Extensions.DependencyInjection;

namespace Gum.Avalonia.Tests.Harness;

/// <summary>
/// Routes the tool's dialogs to the head's own <see cref="AvaloniaDialogService"/> while it lives, so
/// dialogs open in real, owned <see cref="DialogWindow"/>s with nested modal loops, as in the tool.
/// Each queued step waits for the dialog at its nesting depth (1 is the first dialog, 2 a prompt it
/// raises) and drives it. A step that never finds its dialog, or throws, closes every open dialog so
/// the nested loops unwind, and <see cref="AssertFinished"/> reports it.
/// </summary>
internal sealed class HeadDialogScript : IDisposable
{
    private static readonly TimeSpan StepTimeout = TimeSpan.FromSeconds(30);

    private readonly Queue<(int Depth, Action<DialogWindowDriver> Interact)> _steps;
    private readonly List<string> _failures;
    private readonly IDisposable _routing;
    private readonly Stopwatch _sinceLastStep;
    private bool _isPolling;

    public HeadDialogScript()
    {
        Service = TestAppBuilder.Services.GetRequiredService<AvaloniaDialogService>();
        _steps = new Queue<(int, Action<DialogWindowDriver>)>();
        _failures = new List<string>();
        _sinceLastStep = new Stopwatch();
        _routing = ((SwitchableDialogService)TestAppBuilder.Services.GetRequiredService<IDialogService>()).UseHeadDialogs();
    }

    public AvaloniaDialogService Service { get; }

    /// <summary>Queues <paramref name="interact"/> for the dialog open at <paramref name="depth"/>.</summary>
    public void When(int depth, Action<DialogWindowDriver> interact)
    {
        _steps.Enqueue((depth, interact));
        if (!_isPolling)
        {
            _isPolling = true;
            _sinceLastStep.Restart();
            SchedulePoll();
        }
    }

    /// <summary>Waits for every step to run and every dialog to close, then fails on anything that went wrong.</summary>
    public void AssertFinished()
    {
        Stopwatch waited = Stopwatch.StartNew();
        while ((_steps.Count > 0 || Service.OpenDialogs.Count > 0) && waited.Elapsed < StepTimeout)
        {
            Thread.Sleep(10);
            Dispatcher.UIThread.RunJobs();
        }
        if (_steps.Count > 0 || Service.OpenDialogs.Count > 0)
        {
            _failures.Add($"{_steps.Count} step(s) never ran and {Service.OpenDialogs.Count} dialog(s) stayed open.");
            CloseAll();
        }
        if (_failures.Count > 0)
        {
            throw new InvalidOperationException(string.Join("\n", _failures));
        }
    }

    public void Dispose()
    {
        _steps.Clear();
        CloseAll();
        _routing.Dispose();
    }

    private void Poll()
    {
        if (_steps.Count == 0)
        {
            _isPolling = false;
            return;
        }

        (int depth, Action<DialogWindowDriver> interact) = _steps.Peek();
        IReadOnlyList<DialogWindow> open = Service.OpenDialogs;
        if (open.Count == depth && open[depth - 1].IsVisible)
        {
            _steps.Dequeue();
            _sinceLastStep.Restart();
            // Post the next poll first: interact can block in the nested loop of a dialog it opens.
            SchedulePoll();
            try
            {
                interact(new DialogWindowDriver(open[depth - 1]));
            }
            catch (Exception exception)
            {
                Fail($"The step for the dialog at depth {depth} threw: {exception}");
            }
            return;
        }

        if (_sinceLastStep.Elapsed > StepTimeout)
        {
            Fail($"No dialog opened at depth {depth}; {open.Count} open.");
            return;
        }
        SchedulePoll();
    }

    // A timer, not a re-posted job: the drivers' RunJobs would never drain a job that re-posts itself.
    private void SchedulePoll() => DispatcherTimer.RunOnce(Poll, TimeSpan.FromMilliseconds(10));

    private void Fail(string message)
    {
        _failures.Add(message);
        _steps.Clear();
        CloseAll();
    }

    private void CloseAll()
    {
        foreach (DialogWindow window in Service.OpenDialogs.Reverse().ToList())
        {
            window.Close();
        }
    }
}
