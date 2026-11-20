using System;
using System.Diagnostics;
using Gum.DataTypes;
using Gum.Diagnostics;
using Gum.Services;
using Gum.ToolStates;

namespace Gum.Plugins;

/// <summary>
/// Runs a plugin's project-load work in a later UI-thread job instead of inside the load notification,
/// so the tree and canvas can draw the project first. Meant for work nothing else reads right after the
/// load. Each instance keeps only its latest request: a later <see cref="Post"/> drops work still waiting,
/// and work whose project is no longer the loaded one when it runs is dropped.
/// </summary>
public sealed class DeferredProjectLoad
{
    private readonly IDispatcher _dispatcher;
    private readonly IProjectState _projectState;
    private int _run;

    public DeferredProjectLoad(IDispatcher dispatcher, IProjectState projectState)
    {
        _dispatcher = dispatcher;
        _projectState = projectState;
    }

    /// <summary>Posts <paramref name="handler"/> for <paramref name="project"/>; <paramref name="name"/> labels its startup timing line.</summary>
    public void Post(GumProjectSave project, string name, Action<GumProjectSave> handler)
    {
        int run = ++_run;
        _dispatcher.Post(() =>
        {
            if (run != _run || !ReferenceEquals(_projectState.GumProjectSave, project))
            {
                return;
            }
            Stopwatch stopwatch = Stopwatch.StartNew();
            handler(project);
            StartupTiming.Log($"Deferred project load: {name} took {stopwatch.ElapsedMilliseconds} ms");
        });
    }
}
