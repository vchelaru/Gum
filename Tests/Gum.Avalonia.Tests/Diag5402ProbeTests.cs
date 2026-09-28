using System.Reflection;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Gum.Avalonia.Tests.Harness;
using Gum.Managers;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using ToolsUtilities;

namespace Gum.Avalonia.Tests;

// TEMPORARY diagnostic for #5402; removed before merge.
public class Diag5402ProbeTests
{
    [AvaloniaFact]
    public void Probe_LoadGuardLatch()
    {
        using ToolProjectFixture fixture = new ToolProjectFixture("Diag5402");
        fixture.SaveAndReload();
        ProjectManager projectManager = (ProjectManager)TestAppBuilder.Services.GetRequiredService<IProjectManager>();
        FieldInfo field = typeof(ProjectManager).GetField("_inFlightLoadProjectTask", BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(projectManager, null);

        // CPU contention, as on a loaded CI runner: busy threads preempt the UI thread.
        using CancellationTokenSource stop = new CancellationTokenSource();
        List<Thread> spinners = Enumerable.Range(0, Environment.ProcessorCount).Select(_ =>
        {
            Thread thread = new Thread(() => { while (!stop.IsCancellationRequested) { } }) { IsBackground = true };
            thread.Start();
            return thread;
        }).ToList();
        int latched = 0;
        int completedOnReturn = 0;
        const int iterations = 200;
        for (int i = 0; i < iterations; i++)
        {
            Task load = projectManager.LoadProjectAsync(new FilePath(fixture.ProjectFilePath));
            if (load.IsCompleted)
            {
                completedOnReturn++;
            }
            while (!load.IsCompleted)
            {
                Thread.Sleep(1);
                Dispatcher.UIThread.RunJobs();
            }
            Dispatcher.UIThread.RunJobs();
            if (field.GetValue(projectManager) is Task stuck)
            {
                latched++;
                field.SetValue(projectManager, null);
            }
        }

        stop.Cancel();
        $"DIAG5402 os={Environment.OSVersion} cpus={Environment.ProcessorCount} iterations={iterations} completedOnReturn={completedOnReturn} latched={latched}"
            .ShouldBe("see value");
    }
}
