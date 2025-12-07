using Gum.DataTypes;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RaylibGum.Input;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using ToolsUtilities;

#if MONOGAME || KNI || FNA
namespace MonoGameGum;
#elif RAYLIB
namespace RaylibGum;
#endif

public class GumService
{
    #region Default
    static GumService _default;
    public static GumService Default
    {
        get
        {
            if (_default == null)
            {
                _default = new GumService();
            }
            return _default;
        }
    }

    #endregion

#if MONOGAME || KNI || FNA
    public GameTime GameTime { get; private set; }
#endif

    public Cursor Cursor => FormsUtilities.Cursor;

    public Keyboard Keyboard => FormsUtilities.Keyboard;

    public GamePad[] Gamepads => Gum.Forms.FormsUtilities.Gamepads;

    public Renderer Renderer => this.SystemManagers.Renderer;

    public SystemManagers SystemManagers { get; private set; }

    public float CanvasWidth
    {
        get => GraphicalUiElement.CanvasWidth;
        set => GraphicalUiElement.CanvasWidth = value;
    }

    public float CanvasHeight
    {
        get => GraphicalUiElement.CanvasHeight;
        set => GraphicalUiElement.CanvasHeight = value;
    }

    public InteractiveGue Root { get; private set; } = new ContainerRuntime();

    public GumService()
    {
        Root.Width = 0;
        Root.WidthUnits = DimensionUnitType.RelativeToParent;
        Root.Height = 0;
        Root.HeightUnits = DimensionUnitType.RelativeToParent;
        Root.Name = "Main Root";
        Root.HasEvents = false;

        Root.Children.CollectionChanged += (o, e) => Gum.Forms.FormsUtilities.HandleRootCollectionChanged(Root, e);

        //DeferredQueue = new DeferredActionQueue();
    }

    public void Initialize(DefaultVisualsVersion defaultVisualsVersion = DefaultVisualsVersion.V2)
    {
        InitializeInternal(
            gumProjectFile: null,
            systemManagers: SystemManagers.Default,
            defaultVisualsVersion: defaultVisualsVersion);
    }

    public GumProjectSave Initialize(string gumprojectFile)
    {
        return InitializeInternal(
            gumProjectFile: gumprojectFile,
            defaultVisualsVersion: DefaultVisualsVersion.V2)!;
    }

    public bool IsInitialized { get; private set; }

    GumProjectSave? InitializeInternal(string? gumProjectFile = null,
        SystemManagers? systemManagers = null,
        DefaultVisualsVersion defaultVisualsVersion = DefaultVisualsVersion.V2)
    {
        if (IsInitialized)
        {
            throw new InvalidOperationException("Initialize has already been called once. It cannot be called again");
        }
        IsInitialized = true;

        //_game = game;
        // RegisterRuntimeTypesThroughReflection();

        this.SystemManagers = systemManagers ?? new SystemManagers();

        if (systemManagers == null)
        {
            SystemManagers.Default = this.SystemManagers;
#if NET6_0_OR_GREATER
            ISystemManagers.Default = this.SystemManagers;
#endif
        }


#if MONOGAME || FNA || KNI
        this.SystemManagers.Initialize(graphicsDevice, fullInstantiation: true);
#elif RAYLIB
        this.SystemManagers.Initialize();
#endif

        FormsUtilities.InitializeDefaults(defaultVisualsVersion: defaultVisualsVersion);

        Root.AddToManagers(SystemManagers);
        Root.UpdateLayout();

        var mainLayer = SystemManagers.Renderer.MainLayer;
        mainLayer.Remove(Root.RenderableComponent as IRenderableIpso);
        mainLayer.Insert(0, Root.RenderableComponent as IRenderableIpso);

        GumProjectSave? gumProject = null;

        if (!string.IsNullOrEmpty(gumProjectFile))
        {

            gumProject = GumProjectSave.Load(gumProjectFile);
            ObjectFinder.Self.GumProjectSave = gumProject;
            gumProject.Initialize();
            Gum.Forms.FormsUtilities.RegisterFromFileFormRuntimeDefaults();

            var absoluteFile = gumProjectFile;
            if (FileManager.IsRelative(absoluteFile))
            {
                absoluteFile = FileManager.MakeAbsolute(gumProjectFile);
            }

            var gumDirectory = FileManager.GetDirectory(absoluteFile);

            FileManager.RelativeDirectory = gumDirectory;

            ApplyStandardElementDefaults(gumProject);
        }

        return gumProject;
    }

    private void ApplyStandardElementDefaults(GumProjectSave gumProject)
    {
        var current = gumProject.StandardElements.Find(item => item.Name == "ColoredRectangle");
        ColoredRectangleRuntime.DefaultWidth = GetFloat("Width");
        ColoredRectangleRuntime.DefaultHeight = GetFloat("Height");

        current = gumProject.StandardElements.Find(item => item.Name == "NineSlice");

        // October 18, 2025 - this isn't functional in MonoGame either
        //NineSliceRuntime.DefaultSourceFile = GetString("SourceFile");

        float GetFloat(string variableName) => current.DefaultState.GetValueOrDefault<float>(variableName);
        string GetString(string varialbeName) => current.DefaultState.GetValueOrDefault<string>(varialbeName);
    }

    List<GraphicalUiElement> roots = new List<GraphicalUiElement>();
    public void Update(float seconds)
    {
        roots.Clear();
        roots.Add(Root);
        FormsUtilities.Update(seconds, roots);
    }

    public void Draw()
    {
        SystemManagers.Default.Draw();
    }

}

public static class GraphicalUiElementExtensionMethods
{
    public static void AddToRoot(this GraphicalUiElement element)
    {
        GumService.Default.Root.Children.Add(element);
    }

    public static void RemoveFromRoot(this GraphicalUiElement element)
    {
        element.Parent = null;
    }

    public static void AddToRoot(this FrameworkElement element)
    {
        GumService.Default.Root.Children.Add(element.Visual);
    }
}

public static class ElementSaveExtensionMethods
{
    public static GraphicalUiElement ToGraphicalUiElement(this ElementSave elementSave, SystemManagers? systemManagers = null)
    {
        return elementSave.ToGraphicalUiElement(systemManagers ?? SystemManagers.Default, addToManagers: false);
    }

}
