using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Wireframe;

public class RuntimeSnapshotSerializerProjectTests : BaseTestClass
{
    [Fact]
    public void Snapshot_ShouldWriteProjectThatLoadsWithoutErrors()
    {
        string tempDirectory = NewTempDirectory();
        try
        {
            string gumxPath = BuildAndSaveSnapshot(tempDirectory);

            // Reload via the same deserialization core (catches malformed XML + missing files).
            GumProjectSave loaded = GumProjectSave.Load(gumxPath, out GumLoadResult loadResult);

            loaded.ShouldNotBeNull();
            loadResult.ErrorMessage.ShouldBeNullOrEmpty();
            loadResult.MissingFiles.ShouldBeEmpty();

            ScreenSave loadedScreen = loaded.Screens.First(s => s.Name == "Snapshot");
            loadedScreen.Instances.Select(i => i.Name).ShouldBe(new[] { "Panel", "Label" }, ignoreOrder: true);
        }
        finally
        {
            DeleteTempDirectory(tempDirectory);
        }
    }

    [Fact]
    public async Task Snapshot_ShouldPassGumCliCheck()
    {
        // The full integrity check uses the real CLI. It runs as a separate OS process (not an
        // in-process reference) because Gum.ProjectServices and MonoGameGum both compile the Forms
        // control types -- referencing both in one assembly is a CS0433 conflict. A separate process
        // has its own load context, so there is no collision.
        string tempDirectory = NewTempDirectory();
        try
        {
            string gumxPath = BuildAndSaveSnapshot(tempDirectory);
            string cliProject = LocateGumCliCsproj();
            string configuration = DetectBuildConfiguration();

            ProcessStartInfo startInfo = new()
            {
                FileName = "dotnet",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            startInfo.ArgumentList.Add("run");
            startInfo.ArgumentList.Add("--project");
            startInfo.ArgumentList.Add(cliProject);
            // The build-only ProjectReference already built gumcli in this configuration; reuse it
            // rather than rebuilding (which made this test take ~37s).
            startInfo.ArgumentList.Add("--configuration");
            startInfo.ArgumentList.Add(configuration);
            startInfo.ArgumentList.Add("--no-build");
            startInfo.ArgumentList.Add("--");
            startInfo.ArgumentList.Add("check");
            startInfo.ArgumentList.Add(gumxPath);

            using CancellationTokenSource cts = new(TimeSpan.FromMinutes(3));
            using Process process = Process.Start(startInfo)!;
            string stdout = await process.StandardOutput.ReadToEndAsync(cts.Token);
            string stderr = await process.StandardError.ReadToEndAsync(cts.Token);
            await process.WaitForExitAsync(cts.Token);

            process.ExitCode.ShouldBe(0,
                $"gumcli check reported errors (exit {process.ExitCode}).\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}");
        }
        finally
        {
            DeleteTempDirectory(tempDirectory);
        }
    }

    private static string BuildAndSaveSnapshot(string tempDirectory)
    {
        StandardElementsManager.Self.Initialize();

        // Build a small live tree: a panel containing a label.
        ContainerRuntime root = new();
        ContainerRuntime panel = new() { Name = "Panel" };
        TextRuntime label = new() { Name = "Label" };
        label.Text = "Hi";
        root.AddChild(panel);
        panel.AddChild(label);

        // Serialize the tree into a screen, then assemble a full project (standards populated so the
        // instances' BaseTypes resolve). Standards population is a composition-root concern, kept out
        // of the catalog-injected serializer.
        RuntimeSnapshotSerializer serializer = new(StandardElementsManager.Self.DefaultStates);
        ScreenSave screen = serializer.CreateScreenSave(root, "Snapshot");

        GumProjectSave project = new();
        StandardElementsManager.Self.PopulateProjectWithDefaultStandards(project);
        project.Screens.Add(screen);
        project.ScreenReferences.Add(new ElementReference { Name = "Snapshot", ElementType = ElementType.Screen });

        Directory.CreateDirectory(Path.Combine(tempDirectory, ElementReference.ScreenSubfolder));
        Directory.CreateDirectory(Path.Combine(tempDirectory, ElementReference.StandardSubfolder));

        string gumxPath = Path.Combine(tempDirectory, "Snapshot." + GumProjectSave.ProjectExtension);
        project.Save(gumxPath, saveElements: true);
        return gumxPath;
    }

    private static string NewTempDirectory()
    {
        return Path.Combine(Path.GetTempPath(), "GumSnapshotTest_" + Guid.NewGuid().ToString("N"));
    }

    private static void DeleteTempDirectory(string tempDirectory)
    {
        if (Directory.Exists(tempDirectory))
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static string LocateGumCliCsproj()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory != null)
        {
            string candidate = Path.Combine(directory.FullName, "Tools", "Gum.Cli", "Gum.Cli.csproj");
            if (File.Exists(candidate))
            {
                return candidate;
            }
            directory = directory.Parent;
        }
        throw new FileNotFoundException(
            $"Could not locate Tools/Gum.Cli/Gum.Cli.csproj by walking up from {AppContext.BaseDirectory}.");
    }

    private static string DetectBuildConfiguration()
    {
        // The test runs from .../bin/<Config>/net8.0/; the build-only gumcli reference is built in the
        // same configuration, so run that one with --no-build.
        DirectoryInfo tfmDirectory = new(AppContext.BaseDirectory);
        if (tfmDirectory.Parent?.Parent?.Name == "bin")
        {
            return tfmDirectory.Parent.Name;
        }
        return "Debug";
    }
}
