using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Gum.DataTypes;
using Gum.Diagnostics;
using Gum.Managers;
using Gum.ProjectServices;
using Gum.Services;

namespace Gum.Plugins.Errors;

/// <summary>
/// Runs the Errors tab's project check without a long block on the UI thread: it reads the project a
/// slice at a time there, checks the disk on a worker thread using only strings, and applies the rows
/// back on the UI thread only when the project has not changed since it was read. A change in between
/// discards the rows and starts over, so what is applied is what the synchronous check would report now.
/// </summary>
public sealed class BackgroundProjectErrorCheck
{
    /// <summary>How long one posted job reads the project before it lets input and drawing run.</summary>
    public static readonly TimeSpan SliceLength = TimeSpan.FromMilliseconds(15);

    private readonly IErrorChecker _errorChecker;
    private readonly IProjectChangeVersion _changeVersion;
    private readonly IDispatcher _dispatcher;
    private readonly Func<GumProjectSave?> _currentProject;
    private readonly Func<Func<IReadOnlyList<ErrorResult>>, Task<IReadOnlyList<ErrorResult>>> _runOffThread;
    private readonly TimeProvider _time;
    private int _run;

    /// <param name="currentProject">The loaded project when a run starts or restarts.</param>
    /// <param name="runOffThread">Runs the disk half; a test runs it where it can see it.</param>
    public BackgroundProjectErrorCheck(IErrorChecker errorChecker, IProjectChangeVersion changeVersion, IDispatcher dispatcher,
        Func<GumProjectSave?> currentProject,
        Func<Func<IReadOnlyList<ErrorResult>>, Task<IReadOnlyList<ErrorResult>>>? runOffThread = null,
        TimeProvider? time = null)
    {
        _errorChecker = errorChecker;
        _changeVersion = changeVersion;
        _dispatcher = dispatcher;
        _currentProject = currentProject;
        _runOffThread = runOffThread ?? (work => Task.Run(work));
        _time = time ?? TimeProvider.System;
    }

    /// <summary>How many runs were thrown away because the project changed before their rows were applied.</summary>
    public int DiscardedCount { get; private set; }

    /// <summary>How many runs failed on the worker thread; the failure goes to the tool log.</summary>
    public int FailedCount { get; private set; }

    /// <summary>
    /// Checks the loaded project and passes its rows to <paramref name="apply"/> on the UI thread. A later
    /// call replaces a run that has not applied yet. With no project loaded, applies no rows.
    /// </summary>
    public void Start(Action<ErrorViewModel[]> apply)
    {
        int run = ++_run;
        if (_currentProject() is not { } project)
        {
            apply(Array.Empty<ErrorViewModel>());
            return;
        }

        long version = _changeVersion.Version;
        ProjectErrorCheck check = _errorChecker.BeginProjectErrors(project);
        long started = _time.GetTimestamp();
        _dispatcher.Post(() => PrepareSlice(run, version, check, apply, started));
    }

    private bool IsCurrent(int run, long version, ProjectErrorCheck check) =>
        run == _run && version == _changeVersion.Version && ReferenceEquals(_currentProject(), check.Project);

    private void PrepareSlice(int run, long version, ProjectErrorCheck check, Action<ErrorViewModel[]> apply, long started)
    {
        if (run != _run)
        {
            return;
        }
        if (!IsCurrent(run, version, check))
        {
            Restart(apply);
            return;
        }

        long sliceStart = _time.GetTimestamp();
        // At least one step per slice, so a slow clock still moves the check forward.
        do
        {
            check.PrepareNext();
        }
        while (!check.IsPrepared && _time.GetElapsedTime(sliceStart) < SliceLength);

        if (!check.IsPrepared)
        {
            _dispatcher.Post(() => PrepareSlice(run, version, check, apply, started));
            return;
        }

        _runOffThread(check.Check).ContinueWith(
            finished => _dispatcher.Post(() => Apply(run, version, check, apply, finished, started)),
            CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    private void Apply(int run, long version, ProjectErrorCheck check, Action<ErrorViewModel[]> apply,
        Task<IReadOnlyList<ErrorResult>> finished, long started)
    {
        if (run != _run)
        {
            return;
        }
        if (!IsCurrent(run, version, check))
        {
            Restart(apply);
            return;
        }

        if (finished.IsFaulted || finished.IsCanceled)
        {
            // A disk error (a folder the case check may not list, say) keeps the rows shown now; the next
            // save, file change or load checks again.
            Exception error = finished.Exception?.GetBaseException() ?? new TaskCanceledException();
            ToolLog.WriteException("Errors tab project check", error);
            FailedCount++;
            return;
        }

        IReadOnlyList<ErrorResult> rows = finished.Result;
        StartupTiming.Log($"BackgroundProjectErrorCheck applied {rows.Count} rows {_time.GetElapsedTime(started).TotalMilliseconds:0} ms after it started");
        apply(_errorChecker.ToViewModels(rows));
    }

    private void Restart(Action<ErrorViewModel[]> apply)
    {
        DiscardedCount++;
        Start(apply);
    }
}
