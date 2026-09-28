using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Gum.Managers;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using ToolsUtilities;

namespace Gum.Avalonia.Tests.Harness;

public class ToolProjectFixtureTests
{
    [AvaloniaFact]
    public void Dispose_WhileAProjectLoadIsRunning_LeavesNoProjectFromThatLoadBehind()
    {
        IProjectManager projectManager = TestAppBuilder.Services.GetRequiredService<IProjectManager>();
        string otherFolder = Path.Combine(Path.GetTempPath(), "GumFixtureTests", Guid.NewGuid().ToString("N"));
        FilePath otherProject;
        try
        {
            using (ToolProjectFixture fixture = new ToolProjectFixture("GumFixtureTests"))
            {
                fixture.SaveAndReload();
                foreach (string file in Directory.GetFiles(fixture.ProjectFolder, "*.g*x", SearchOption.AllDirectories))
                {
                    string target = Path.Combine(otherFolder, Path.GetRelativePath(fixture.ProjectFolder, file));
                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    File.Copy(file, target);
                }
                otherProject = new FilePath(Path.Combine(otherFolder, Path.GetFileName(fixture.ProjectFilePath)));

                // A test that fails while waiting for a load leaves it running when its fixture is disposed.
                projectManager.LoadProjectAsync(otherProject);
            }

            // The next test pumps the UI thread, which is where the load would finish.
            for (int i = 0; i < 200; i++)
            {
                Thread.Sleep(10);
                Dispatcher.UIThread.RunJobs();
            }

            string? openProject = projectManager.GumProjectSave?.FullFileName;
            (openProject != null && new FilePath(openProject) == otherProject).ShouldBeFalse(
                $"the load the disposed fixture left running replaced the project afterwards ({openProject})");
        }
        finally
        {
            projectManager.CreateNewProject();
            try
            {
                Directory.Delete(otherFolder, recursive: true);
            }
            catch (IOException)
            {
                // A file watcher may still hold it; the temp folder is cleaned up later.
            }
        }
    }
}
