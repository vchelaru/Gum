using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Wireframe;
using RenderingLibrary;
using Shouldly;
using SkiaSharp;
using ToolsUtilities;

namespace Gum.NetStandard.Tests;

/// <summary>
/// A netstandard2.1 host that can't read files directly (Unity on Android or WebGL) serves the project
/// through <see cref="FileManager.CustomGetStreamFromFile"/>. The netstandard2.1 build has no
/// OperatingSystem checks, so the hook alone must route every project and element read through it.
/// </summary>
public class CustomStreamProjectLoadTests : IDisposable
{
    private readonly Func<string, Stream>? _previousHook = FileManager.CustomGetStreamFromFile;
    private readonly TestGumService _service = new TestGumService();

    public void Dispose()
    {
        FileManager.CustomGetStreamFromFile = _previousHook;
        _service.Uninitialize();
        ObjectFinder.Self.GumProjectSave = null;
    }

    [Fact]
    public void Initialize_ProjectOnlyReachableThroughHook_BuildsScreen()
    {
        GumProjectSave project = new GumProjectSave();
        AddStandard(project, "Container");
        AddStandard(project, "Rectangle");

        ScreenSave screen = new ScreenSave { Name = "MainScreen" };
        StateSave screenState = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(screenState);
        screen.Instances.Add(new InstanceSave { Name = "Box", BaseType = "Rectangle", ParentContainer = screen });
        screenState.Variables.Add(new VariableSave { Name = "Box.X", Type = "float", Value = 12f, SetsValue = true });
        project.Screens.Add(screen);
        project.ScreenReferences.Add(new ElementReference { Name = "MainScreen", ElementType = ElementType.Screen });

        string directory = Path.Combine(Path.GetTempPath(), "NetStandardHookLoad_" + Path.GetRandomFileName());
        Directory.CreateDirectory(directory);
        string gumxPath = Path.Combine(directory, "Proj.gumx");
        Dictionary<string, byte[]> files;
        try
        {
            project.Save(gumxPath, saveElements: true);
            files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
                .ToDictionary(Normalize, File.ReadAllBytes);
        }
        finally
        {
            // Nothing is left on disk, so a direct File read would fail the load.
            Directory.Delete(directory, recursive: true);
        }

        List<string> requested = new List<string>();
        FileManager.CustomGetStreamFromFile = fileName =>
        {
            requested.Add(fileName);
            return files.TryGetValue(Normalize(fileName), out byte[]? bytes)
                ? new MemoryStream(bytes)
                : throw new FileNotFoundException(fileName);
        };

        using SKSurface surface = SKSurface.Create(new SKImageInfo(200, 100));
        _service.Initialize(surface.Canvas, 200, 100, gumxPath);

        _service.LastLoadResult!.ErrorMessage.ShouldBeNullOrEmpty();
        requested.ShouldContain(name => name.EndsWith("MainScreen.gusx", StringComparison.OrdinalIgnoreCase));

        ScreenSave loadedScreen = ObjectFinder.Self.GetScreen("MainScreen")!;
        GraphicalUiElement visual = GumRuntime.ElementSaveExtensions.ToGraphicalUiElement(
            loadedScreen, SystemManagers.Default, addToManagers: false);
        visual.GetGraphicalUiElementByName("Box")!.X.ShouldBe(12f);
    }

    private static void AddStandard(GumProjectSave project, string name)
    {
        StandardElementSave standard = new StandardElementSave { Name = name };
        standard.States.Add(new StateSave { Name = "Default", ParentContainer = standard });
        project.StandardElements.Add(standard);
        project.StandardElementReferences.Add(new ElementReference { Name = name, ElementType = ElementType.Standard });
    }

    private static string Normalize(string path) =>
        Path.GetFullPath(path).Replace('\\', '/').ToLowerInvariant();
}
