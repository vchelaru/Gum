using Gum.Managers;
using Microsoft.Extensions.DependencyInjection;

namespace Gum.Avalonia.Tests.Harness;

/// <summary>
/// Waits on the wall clock for work a gesture started asynchronously (a project load, a .gumx
/// preview), which runs partly on the thread pool and so finishes in real time, not in pumps.
/// </summary>
internal static class AsyncWorkWait
{
    /// <summary>
    /// Runs <paramref name="pump"/> until <paramref name="condition"/> holds; throws a
    /// <see cref="TimeoutException"/> naming the last project load's status after <paramref name="timeout"/>.
    /// </summary>
    public static void Until(Func<bool> condition, TimeSpan timeout, string what, Action pump, IEnumerable<string> messagesShown)
    {
        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
        while (!condition())
        {
            if (stopwatch.Elapsed > timeout)
            {
                // A load still running, or one that never started, is the first thing to rule out.
                IProjectManager projectManager = TestAppBuilder.Services.GetRequiredService<IProjectManager>();
                Task? load = (projectManager as ProjectManager)?.InFlightLoadProjectTask;
                throw new TimeoutException($"Waited {timeout.TotalSeconds:0} s for {what}. Messages shown: [{string.Join(" | ", messagesShown)}]. " +
                    $"Last project load: {load?.Status.ToString() ?? "none"}; open project: {projectManager.GumProjectSave?.FullFileName ?? "none"}.");
            }
            Thread.Sleep(10);
            pump();
        }
    }
}
