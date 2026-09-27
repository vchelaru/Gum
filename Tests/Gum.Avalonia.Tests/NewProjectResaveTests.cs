using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Gum.Avalonia.Tests.Harness;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Managers;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using ToolsUtilities;

namespace Gum.Avalonia.Tests;

/// <summary>
/// A new project re-saves unchanged after it is reopened (#5194).
/// </summary>
/// <remarks>
/// The same check is <c>ProjectOracleTests.NewProject_ReloadResavesUnchanged</c> on the #5141
/// end-to-end branch; once that lands, keep one of the two.
/// </remarks>
public class NewProjectResaveTests
{
    private static IServiceProvider Services => TestAppBuilder.Services;

    [AvaloniaFact]
    public void NewProject_ReloadResavesUnchanged()
    {
        using ToolProjectFixture fixture = new ToolProjectFixture("GumNewProjectResave");
        // The editor tab plugin, which sits out of headless runs, fills the canvas sizes on load.
        fixture.Project.CustomCanvasSizes ??= new List<CustomCanvasSize>();
        IFileCommands fileCommands = Services.GetRequiredService<IFileCommands>();
        fileCommands.ForceSaveProject(forceSaveContainedElements: true);
        Dictionary<string, string> created = ReadProjectFiles(fixture.ProjectFolder);

        Task load = Services.GetRequiredService<IProjectManager>().LoadProjectAsync(new FilePath(fixture.Project.FullFileName!));
        while (!load.IsCompleted)
        {
            Thread.Sleep(10);
            Dispatcher.UIThread.RunJobs();
        }
        load.GetAwaiter().GetResult();
        Dispatcher.UIThread.RunJobs();
        fileCommands.ForceSaveProject(forceSaveContainedElements: true);

        Dictionary<string, string> resaved = ReadProjectFiles(fixture.ProjectFolder);
        resaved.Keys.ShouldBe(created.Keys, ignoreOrder: true);
        foreach (string path in created.Keys)
        {
            resaved[path].ShouldBe(created[path], $"{path} changed after the new project was reopened and saved");
        }
    }

    private static Dictionary<string, string> ReadProjectFiles(string projectFolder) =>
        Directory.EnumerateFiles(projectFolder, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(projectFolder, path).Replace('\\', '/'))
            .Where(path => !path.StartsWith("UserData/", StringComparison.Ordinal) && !path.StartsWith("FontCache/", StringComparison.Ordinal))
            .ToDictionary(path => path, path => File.ReadAllText(Path.Combine(projectFolder, path)));
}
