using System;
using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.Diagnostics;
using Gum.Managers;
using Gum.Services;
using Gum.ToolStates;

namespace Gum.Plugins.InternalPlugins.TreeView;

/// <summary>
/// Checks every element of the loaded project for the tree's error icons after the tree has shown,
/// a slice at a time on the UI thread, so the tree can be scrolled and clicked while the icons fill in.
/// Each check raises <see cref="IErrorChecker.ErrorsChecked"/>, which sets that element's icon.
/// </summary>
public sealed class DeferredProjectErrorCheck
{
    /// <summary>How long one posted job checks elements before it lets input and drawing run.</summary>
    public static readonly TimeSpan SliceLength = TimeSpan.FromMilliseconds(15);

    private readonly IErrorChecker _errorChecker;
    private readonly IProjectState _projectState;
    private readonly IDispatcher _dispatcher;
    private readonly TimeProvider _time;
    private int _run;

    public DeferredProjectErrorCheck(IErrorChecker errorChecker, IProjectState projectState, IDispatcher dispatcher,
        TimeProvider? time = null)
    {
        _errorChecker = errorChecker;
        _projectState = projectState;
        _dispatcher = dispatcher;
        _time = time ?? TimeProvider.System;
    }

    /// <summary>
    /// Starts checking the loaded project's elements once the UI thread is free. A later call replaces an
    /// unfinished run, and a run stops when another project is loaded.
    /// </summary>
    public void Start()
    {
        if (_projectState.GumProjectSave is not { } project)
        {
            return;
        }

        int run = ++_run;
        Queue<ElementSave> remaining = new Queue<ElementSave>(
            project.Screens.Cast<ElementSave>().Concat(project.Components).Concat(project.StandardElements));
        long started = _time.GetTimestamp();
        _dispatcher.Post(() => CheckSlice(run, project, remaining, started));
    }

    private void CheckSlice(int run, GumProjectSave project, Queue<ElementSave> remaining, long started)
    {
        if (run != _run || !ReferenceEquals(_projectState.GumProjectSave, project))
        {
            return;
        }

        long sliceStart = _time.GetTimestamp();
        // Without the cache each check rebuilds ObjectFinder's lookups (#4495); it is ref-counted.
        ObjectFinder.Self.EnableCache();
        try
        {
            while (remaining.Count > 0 && _time.GetElapsedTime(sliceStart) < SliceLength)
            {
                ElementSave element = remaining.Dequeue();
                // An element deleted after the load has no icon to set.
                if (project.AllElements.Contains(element))
                {
                    _errorChecker.GetErrorsFor(element, project);
                }
            }
        }
        finally
        {
            ObjectFinder.Self.DisableCache();
        }

        if (remaining.Count > 0)
        {
            _dispatcher.Post(() => CheckSlice(run, project, remaining, started));
        }
        else
        {
            StartupTiming.Log($"MainTreeViewPlugin error icons done, {_time.GetElapsedTime(started).TotalMilliseconds:0} ms after the load asked for them");
        }
    }
}
