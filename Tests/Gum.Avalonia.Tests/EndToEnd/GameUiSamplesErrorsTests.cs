using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Gum.Avalonia.Shell;
using Gum.Avalonia.Tests.Harness;
using Gum.DataTypes;
using Gum.Managers;
using CommunityToolkit.Mvvm.Messaging;
using Gum.Messages;
using Gum.Plugins.Errors;
using Gum.Services;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using ToolsUtilities;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>Opening the GameUiSamples project in the tool lists no errors (#8094: four stale code files at old paths showed as GUM0005 rows, which only the tool reports).</summary>
public class GameUiSamplesErrorsTests
{
    [AvaloniaFact]
    public void GameUiSamples_ShowsNoErrors_OnceTheUiSettles()
    {
        using ToolProjectFixture fixture = new ToolProjectFixture("GumGameUiSamplesErrors");
        string source = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../Samples/GameUiSamples"));
        string target = Path.Combine(fixture.ProjectFolder, "Sample");
        CopyDirectory(source, target);
        fixture.FollowProjectFile(Path.Combine(target, "Content", "GumProject", "GameUiSamplesGumProject.gumx"));
        IProjectManager projectManager = TestAppBuilder.Services.GetRequiredService<IProjectManager>();
        IMessenger messenger = TestAppBuilder.Services.GetRequiredService<IMessenger>();
        object recipient = new object();
        bool orphanScanApplied = false;
        // The orphan code file scan runs off the UI thread and posts its result back, and it always
        // asks for an error refresh when it does, even with nothing to report.
        messenger.Register<RequestErrorRefreshMessage>(recipient, (_, message) =>
        {
            orphanScanApplied |= message.RequestingPlugin?.FriendlyName == "Orphan Code File Plugin";
        });
        try
        {
            Task load = projectManager.LoadProjectAsync(new FilePath(fixture.ProjectFilePath));
            System.Diagnostics.Stopwatch waited = System.Diagnostics.Stopwatch.StartNew();
            while (!(load.IsCompleted && orphanScanApplied))
            {
                waited.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(60), "the project load and orphan scan did not finish");
                Thread.Sleep(5);
                Dispatcher.UIThread.RunJobs();
            }
            load.GetAwaiter().GetResult();
            Dispatcher.UIThread.RunJobs();
        }
        finally
        {
            messenger.UnregisterAll(recipient);
        }

        AvaloniaPluginTab tab = TestAppBuilder.Services.GetRequiredService<AvaloniaTabManager>().AllTabs.Single(item => item.Title == "Errors");
        AllErrorsViewModel errors = tab.Content as AllErrorsViewModel ?? (AllErrorsViewModel)((Control)tab.Content).DataContext!;
        string[] messages = errors.Errors.Select(error => $"[{error.Code}] {error.ElementName}: {error.Message}").ToArray();
        messages.ShouldBeEmpty(string.Join("\n", messages));
    }

    private static void CopyDirectory(string source, string target)
    {
        Directory.CreateDirectory(target);
        foreach (string file in Directory.GetFiles(source))
        {
            File.Copy(file, Path.Combine(target, Path.GetFileName(file)), true);
        }
        foreach (string directory in Directory.GetDirectories(source))
        {
            if (Path.GetFileName(directory) is "bin" or "obj")
            {
                continue;
            }
            CopyDirectory(directory, Path.Combine(target, Path.GetFileName(directory)));
        }
    }
}
