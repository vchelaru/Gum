using Gum.DataTypes;
using Gum.Managers;
using Gum.StateAnimation.SaveClasses;
using Gum.Wireframe;
using GumRuntime;
using Gum.Forms.Controls;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Reflection;
using ToolsUtilities;
using Gum.Forms;
using Gum.Threading;

#if MONOGAME || KNI || FNA
using MonoGameGum.GueDeriving;
using MonoGameGum.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
namespace MonoGameGum;
#elif RAYLIB
using Gum.GueDeriving;
using RaylibGum.Input;
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

    public DeferredActionQueue DeferredQueue { get; private set; }

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

    public ContentLoader? ContentLoader => LoaderManager.Self.ContentLoader as ContentLoader;

    public InteractiveGue Root { get; private set; } = new ContainerRuntime();
    /// <inheritdoc/>
    public InteractiveGue PopupRoot => FrameworkElement.PopupRoot;
    /// <inheritdoc/>
    public InteractiveGue ModalRoot => FrameworkElement.ModalRoot;

    public void UseKeyboardDefaults()
    {
        Gum.Forms.Controls.FrameworkElement.KeyboardsForUiControl.Add(GumService.Default.Keyboard);
    }

    public void UseGamepadDefaults()
    {
        Gum.Forms.Controls.FrameworkElement.GamePadsForUiControl.AddRange(GumService.Default.Gamepads);
    }

#if MONOGAME || KNI || FNA
    Game _game;
#endif

    #region Initialize

    public GumService()
    {
        Root.Width = 0;
        Root.WidthUnits = DimensionUnitType.RelativeToParent;
        Root.Height = 0;
        Root.HeightUnits = DimensionUnitType.RelativeToParent;
        Root.Name = "Main Root";
        Root.HasEvents = false;

        Root.Children.CollectionChanged += (o, e) => Gum.Forms.FormsUtilities.HandleRootCollectionChanged(Root, e);

        DeferredQueue = new DeferredActionQueue();
    }

    /// <summary>
    /// Initializes Gum, optionally loading a Gum project.
    /// </summary>
#if MONOGAME || KNI || FNA
    /// <param name="game">The game instance.</param>
#endif
    /// <param name="gumProjectFile">An optional project to load. If not specified, no project is loaded and Gum can be used "code only".</param>
    /// <returns>The loaded project, or null if no project is loaded</returns>
    public GumProjectSave Initialize(string gumProjectFile)
    {
        return InitializeInternal(
            gumProjectFile: gumProjectFile,
            defaultVisualsVersion: DefaultVisualsVersion.Newest)!;
    }

    public void Initialize(DefaultVisualsVersion defaultVisualsVersion = DefaultVisualsVersion.V2)
    {
        InitializeInternal(
            gumProjectFile: null,
            systemManagers: SystemManagers.Default,
            defaultVisualsVersion: defaultVisualsVersion);
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

#endregion

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
        DeferredQueue.ProcessPending();
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
