using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using Shouldly;
using ToolsUtilities;
using Xunit;

namespace MonoGameGum.Tests.Wireframe;

public class RuntimeSnapshotSerializerProjectTests : BaseTestClass
{
    [Fact]
    public void ExportSnapshot_ShouldWriteShakenLoadableProjectFromLiveRoot()
    {
        string tempDirectory = NewTempDirectory();
        try
        {
            // Exercise the public entry point: build a live tree under a GumService's Root, then export.
            GumService service = new();
            ContainerRuntime panel = new() { Name = "Panel" };
            TextRuntime label = new() { Name = "Label" };
            label.Text = "Hi"; // differs from the standard Text default -> survives the shake
            panel.AddChild(label);
            service.Root.AddChild(panel);

            string gumxPath = Path.Combine(tempDirectory, "Live." + GumProjectSave.ProjectExtension);
            service.ExportSnapshot(gumxPath);

            GumProjectSave loaded = GumProjectSave.Load(gumxPath, out GumLoadResult loadResult);

            loaded.ShouldNotBeNull();
            loadResult.ErrorMessage.ShouldBeNullOrEmpty();
            loadResult.MissingFiles.ShouldBeEmpty();

            // The screen is named after the file; the live tree is flattened into instances.
            ScreenSave loadedScreen = loaded.Screens.First(s => s.Name == "Live");
            loadedScreen.Instances.Select(i => i.Name).ShouldBe(new[] { "Panel", "Label" }, ignoreOrder: true);

            // Shaken by default: the changed value survives, a default-valued one is pruned.
            StateSave defaultState = loadedScreen.States.First(s => s.Name == "Default");
            defaultState.Variables.ShouldContain(v => v.Name == "Label.Text");
            defaultState.Variables.ShouldNotContain(v => v.Name == "Label.Visible");
        }
        finally
        {
            DeleteTempDirectory(tempDirectory);
        }
    }

    [Fact]
    public void Snapshot_Shaken_ShouldWriteProjectThatLoadsWithoutErrors()
    {
        string tempDirectory = NewTempDirectory();
        try
        {
            string gumxPath = BuildAndSaveSnapshot(tempDirectory, shake: true);

            GumProjectSave loaded = GumProjectSave.Load(gumxPath, out GumLoadResult loadResult);

            loaded.ShouldNotBeNull();
            loadResult.ErrorMessage.ShouldBeNullOrEmpty();
            loadResult.MissingFiles.ShouldBeEmpty();

            ScreenSave loadedScreen = loaded.Screens.First(s => s.Name == "Snapshot");
            loadedScreen.Instances.Select(i => i.Name).ShouldBe(new[] { "Panel", "Label" }, ignoreOrder: true);

            // Shaken: Label.Text (="Hi") differs from the default and is kept; Label.Visible matches and is pruned.
            StateSave defaultState = loadedScreen.States.First(s => s.Name == "Default");
            defaultState.Variables.ShouldContain(v => v.Name == "Label.Text");
            defaultState.Variables.ShouldNotContain(v => v.Name == "Label.Visible");
        }
        finally
        {
            DeleteTempDirectory(tempDirectory);
        }
    }

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
    public async Task Snapshot_Shaken_ShouldPassGumCliCheck()
    {
        await RunGumCliCheck(shake: true);
    }

    [Fact]
    public async Task Snapshot_ShouldPassGumCliCheck()
    {
        await RunGumCliCheck(shake: false);
    }

    private static async Task RunGumCliCheck(bool shake)
    {
        // The full integrity check uses the real CLI. It runs as a separate OS process (not an
        // in-process reference) because Gum.ProjectServices and MonoGameGum both compile the Forms
        // control types -- referencing both in one assembly is a CS0433 conflict. A separate process
        // has its own load context, so there is no collision.
        string tempDirectory = NewTempDirectory();
        try
        {
            string gumxPath = BuildAndSaveSnapshot(tempDirectory, shake);
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

    [Fact]
    public void ExportSnapshot_ShouldIncludeStandardForReferencedDeprecatedColoredRectangle()
    {
        // New (v3) projects no longer seed ColoredRectangle (it's deprecated), but a live tree may still
        // contain one. The snapshot must add the referenced standard so the instance's BaseType resolves
        // rather than dangling on a missing standard element.
        string tempDirectory = NewTempDirectory();
        try
        {
            GumService service = new();
#pragma warning disable CS0618 // ColoredRectangle is obsolete but old live trees still contain it.
            service.Root.AddChild(new ColoredRectangleRuntime { Name = "Rect" });
#pragma warning restore CS0618

            string gumxPath = Path.Combine(tempDirectory, "Live." + GumProjectSave.ProjectExtension);
            service.ExportSnapshot(gumxPath);

            GumProjectSave loaded = GumProjectSave.Load(gumxPath, out _);
            loaded.Screens.First(s => s.Name == "Live").Instances
                .First(i => i.Name == "Rect").BaseType.ShouldBe("ColoredRectangle");
            loaded.StandardElements.Any(s => s.Name == "ColoredRectangle").ShouldBeTrue();
        }
        finally
        {
            DeleteTempDirectory(tempDirectory);
        }
    }

    [Fact]
    public void ExportSnapshot_ShouldSetCanvasSizeFromRuntimeCanvas()
    {
        // The exported project's resolution should match the live canvas (the game's resolution), not
        // the 800x600 GumProjectSave default.
        float originalWidth = GraphicalUiElement.CanvasWidth;
        float originalHeight = GraphicalUiElement.CanvasHeight;
        string tempDirectory = NewTempDirectory();
        try
        {
            GraphicalUiElement.CanvasWidth = 1920;
            GraphicalUiElement.CanvasHeight = 1080;

            GumService service = new();
            service.Root.AddChild(new ContainerRuntime { Name = "Panel" });

            string gumxPath = Path.Combine(tempDirectory, "Live." + GumProjectSave.ProjectExtension);
            service.ExportSnapshot(gumxPath);

            GumProjectSave loaded = GumProjectSave.Load(gumxPath, out _);
            loaded.DefaultCanvasWidth.ShouldBe(1920);
            loaded.DefaultCanvasHeight.ShouldBe(1080);
        }
        finally
        {
            GraphicalUiElement.CanvasWidth = originalWidth;
            GraphicalUiElement.CanvasHeight = originalHeight;
            DeleteTempDirectory(tempDirectory);
        }
    }

    [Fact]
    public void ExportSnapshot_ShouldCopyReferencedFilesPreservingRelativePath()
    {
        string originalRelativeDirectory = FileManager.RelativeDirectory;
        string contentDirectory = NewTempDirectory();
        string snapshotDirectory = NewTempDirectory();
        try
        {
            // Lay a referenced texture file under a content dir, mirroring how a game ships assets.
            const string relativePath = "UI/button.png";
            string sourceFile = Path.Combine(contentDirectory, "UI", "button.png");
            Directory.CreateDirectory(Path.GetDirectoryName(sourceFile)!);
            File.WriteAllBytes(sourceFile, new byte[] { 1, 2, 3 });

            // Relative SourceFile paths resolve under here (as the content loader resolved them at load).
            FileManager.RelativeDirectory = contentDirectory;

            GumService service = new();
            SpriteRuntime sprite = new() { Name = "Sprite" };
            Microsoft.Xna.Framework.Graphics.Texture2D texture = MakeHeadlessTexture();
            texture.Name = relativePath;
            sprite.Texture = texture;
            service.Root.AddChild(sprite);

            string gumxPath = Path.Combine(snapshotDirectory, "Live." + GumProjectSave.ProjectExtension);
            service.ExportSnapshot(gumxPath);

            // The referenced file is copied next to the snapshot, preserving its relative path.
            File.Exists(Path.Combine(snapshotDirectory, "UI", "button.png")).ShouldBeTrue();

            // And the SourceFile variable points at that same relative path.
            GumProjectSave loaded = GumProjectSave.Load(gumxPath, out _);
            StateSave defaultState = loaded.Screens.First(s => s.Name == "Live").States.First(s => s.Name == "Default");
            defaultState.Variables.First(v => v.Name == "Sprite.SourceFile").Value.ShouldBe(relativePath);
        }
        finally
        {
            FileManager.RelativeDirectory = originalRelativeDirectory;
            DeleteTempDirectory(contentDirectory);
            DeleteTempDirectory(snapshotDirectory);
        }
    }

    [Fact]
    public void Snapshot_ShouldSerializeEveryStandardRuntimeType()
    {
        // Coverage guard: a snapshot containing one of every standard runtime type must serialize and
        // reload without error. Sprite and NineSlice carry an in-memory Texture2D -- the snapshot must
        // not attempt to write that non-serializable object into a VariableSave. (Previously only
        // Container and Text were ever exercised, so this whole dimension was untested.)
        ContainerRuntime root = new();
        root.AddChild(new ContainerRuntime { Name = "ContainerInstance" });
        root.AddChild(new TextRuntime { Name = "TextInstance", Text = "Hi" });
        root.AddChild(new RectangleRuntime { Name = "RectangleInstance" });
        root.AddChild(new CircleRuntime { Name = "CircleInstance" });
        root.AddChild(new PolygonRuntime { Name = "PolygonInstance" });
#pragma warning disable CS0618 // ColoredRectangle is obsolete but is still a live standard type to cover.
        root.AddChild(new ColoredRectangleRuntime { Name = "ColoredRectangleInstance" });
#pragma warning restore CS0618

        SpriteRuntime sprite = new() { Name = "SpriteInstance" };
        sprite.Texture = MakeHeadlessTexture();
        root.AddChild(sprite);

        NineSliceRuntime nineSlice = new() { Name = "NineSliceInstance" };
        nineSlice.Texture = MakeHeadlessTexture();
        root.AddChild(nineSlice);

        string tempDirectory = NewTempDirectory();
        try
        {
            string gumxPath = SaveRootAsProject(root, tempDirectory, shake: true);

            GumProjectSave loaded = GumProjectSave.Load(gumxPath, out GumLoadResult loadResult);

            loaded.ShouldNotBeNull();
            loadResult.ErrorMessage.ShouldBeNullOrEmpty();
            loadResult.MissingFiles.ShouldBeEmpty();

            loaded.Screens.First(s => s.Name == "Snapshot").Instances.Count.ShouldBe(8);
        }
        finally
        {
            DeleteTempDirectory(tempDirectory);
        }
    }

    private static string BuildAndSaveSnapshot(string tempDirectory, bool shake = false)
    {
        // Build a small live tree: a panel containing a label.
        ContainerRuntime root = new();
        ContainerRuntime panel = new() { Name = "Panel" };
        TextRuntime label = new() { Name = "Label" };
        label.Text = "Hi";
        root.AddChild(panel);
        panel.AddChild(label);

        return SaveRootAsProject(root, tempDirectory, shake);
    }

    /// <summary>
    /// Serializes an arbitrary live tree into a screen and assembles a full project (standards populated
    /// so the instances' BaseTypes resolve), saving it to <paramref name="tempDirectory"/>. Standards
    /// population is a composition-root concern, kept out of the catalog-injected serializer.
    /// </summary>
    private static string SaveRootAsProject(GraphicalUiElement root, string tempDirectory, bool shake)
    {
        StandardElementsManager.Self.Initialize();

        RuntimeSnapshotSerializer serializer = new(StandardElementsManager.Self.DefaultStates);
        ScreenSave screen = serializer.CreateScreenSave(root, "Snapshot", shake);

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

    // Fabricates a Texture2D reference without a GraphicsDevice (its ctor needs one). The snapshot path
    // only holds the reference; it is never dereferenced, so an uninitialized shell is safe here.
    private static Microsoft.Xna.Framework.Graphics.Texture2D MakeHeadlessTexture() =>
        (Microsoft.Xna.Framework.Graphics.Texture2D)System.Runtime.CompilerServices.RuntimeHelpers
            .GetUninitializedObject(typeof(Microsoft.Xna.Framework.Graphics.Texture2D));

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
