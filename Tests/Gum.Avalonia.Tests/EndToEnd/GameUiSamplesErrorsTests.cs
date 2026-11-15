using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Gum.Avalonia.Shell;
using Gum.Avalonia.Tests.Harness;
using Gum.DataTypes;
using Gum.Managers;
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
        Task load = projectManager.LoadProjectAsync(new FilePath(fixture.ProjectFilePath));
        System.Diagnostics.Stopwatch waited = System.Diagnostics.Stopwatch.StartNew();
        while (waited.Elapsed < TimeSpan.FromSeconds(8))
        {
            Thread.Sleep(20);
            Dispatcher.UIThread.RunJobs();
        }
        load.IsCompleted.ShouldBeTrue();

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
