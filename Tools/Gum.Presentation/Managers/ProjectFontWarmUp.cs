using System;
using System.Collections.Generic;
using System.Linq;
using Gum.Bundle;
using Gum.DataTypes;
using Gum.Diagnostics;
using Gum.ProjectServices;
using Gum.Services;

namespace Gum.Managers;

/// <summary>
/// Collects a just-loaded project's fonts into the shared <see cref="FontReferenceCache"/> a slice at a
/// time on the UI thread, and holds back the project-wide passes that need them (the Errors tab's project
/// check, the file watch scan, the font cache check) until it is done. Each pass then finds every font
/// collected instead of collecting them in one long block of its own.
/// </summary>
public interface IProjectFontWarmUp
{
    /// <summary>
    /// Starts collecting <paramref name="project"/>'s fonts once the UI thread is free. Call it before
    /// the load notification, so the passes plugins queue during that notification wait for it.
    /// </summary>
    void Start(GumProjectSave project);

    /// <summary>Posts <paramref name="action"/> to the UI thread, after the running collection if one is.</summary>
    void PostAfterWarmUp(Action action);
}

/// <inheritdoc cref="IProjectFontWarmUp"/>
public sealed class ProjectFontWarmUp : IProjectFontWarmUp
{
    /// <summary>How long one posted job collects before it lets input and drawing run.</summary>
    public static readonly TimeSpan SliceLength = TimeSpan.FromMilliseconds(15);

    private readonly IFontReferenceCacheSource _fontReferences;
    private readonly IDispatcher _dispatcher;
    private readonly TimeProvider _time;
    private readonly List<Action> _waiting = new List<Action>();
    private int _run;

    public ProjectFontWarmUp(IFontReferenceCacheSource fontReferences, IDispatcher dispatcher, TimeProvider? time = null)
    {
        _fontReferences = fontReferences;
        _dispatcher = dispatcher;
        _time = time ?? TimeProvider.System;
    }

    /// <summary>True while a collection runs and passes wait for it.</summary>
    public bool IsRunning { get; private set; }

    /// <summary>How many collections reached every element; a test reads it to see the load started one.</summary>
    public int CompletedCount { get; private set; }

    /// <inheritdoc/>
    public void Start(GumProjectSave project)
    {
        int run = ++_run;
        if (!project.AllElements.Any())
        {
            Finish(completed: true);
            return;
        }
        IsRunning = true;
        Queue<ElementSave> remaining = new Queue<ElementSave>(project.AllElements);
        long started = _time.GetTimestamp();
        _dispatcher.Post(() => CollectSlice(run, project, remaining, cache: null, started));
    }

    /// <inheritdoc/>
    public void PostAfterWarmUp(Action action)
    {
        if (IsRunning)
        {
            _waiting.Add(action);
        }
        else
        {
            _dispatcher.Post(action);
        }
    }

    private void CollectSlice(int run, GumProjectSave project, Queue<ElementSave> remaining, FontReferenceCache? cache, long started)
    {
        if (run != _run)
        {
            return;
        }

        // Another project replaced this one (a load, a new project): its passes are for the new one.
        if (!ReferenceEquals(ObjectFinder.Self.GumProjectSave, project))
        {
            Finish(completed: false);
            return;
        }

        try
        {
            CollectSliceOf(run, project, remaining, cache, started);
        }
        catch
        {
            // A failed collection must not hold the passes back for good; they collect for themselves.
            Finish(completed: false);
            throw;
        }
    }

    private void CollectSliceOf(int run, GumProjectSave project, Queue<ElementSave> remaining, FontReferenceCache? cache, long started)
    {
        FontReferenceCache current = _fontReferences.For(project);
        if (cache != null && !ReferenceEquals(cache, current))
        {
            // The project changed since the last slice, so what was collected is gone; the passes collect
            // what they need themselves rather than waiting for a collection that may never settle.
            Finish(completed: false);
            return;
        }

        FontReferenceCollector collector = new FontReferenceCollector(instance => ObjectFinder.Self.GetElementSave(instance));
        long sliceStart = _time.GetTimestamp();
        // Without the cache each instance's base type is a linear search; it is ref-counted.
        ObjectFinder.Self.EnableCache();
        try
        {
            // At least one element per slice, so a slow clock still moves the collection forward.
            do
            {
                ElementSave element = remaining.Dequeue();
                // Collected the way GumProjectDependencyWalker and font generation do for an unlocalized
                // walk, so they find it in the cache.
                current.Collect(new[] { element }, initializesProject: false,
                    single => collector.Collect(project, new[] { single }));
            }
            while (remaining.Count > 0 && _time.GetElapsedTime(sliceStart) < SliceLength);
        }
        finally
        {
            ObjectFinder.Self.DisableCache();
        }

        if (remaining.Count > 0)
        {
            _dispatcher.Post(() => CollectSlice(run, project, remaining, current, started));
        }
        else
        {
            StartupTiming.Log($"ProjectFontWarmUp collected {project.AllElements.Count()} elements in {_time.GetElapsedTime(started).TotalMilliseconds:0} ms");
            Finish(completed: true);
        }
    }

    // Each waiting pass is its own UI-thread job, so input runs between them.
    private void Finish(bool completed)
    {
        IsRunning = false;
        if (completed)
        {
            CompletedCount++;
        }
        List<Action> waiting = _waiting.ToList();
        _waiting.Clear();
        foreach (Action action in waiting)
        {
            _dispatcher.Post(action);
        }
    }
}
