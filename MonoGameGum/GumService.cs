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
            if(_default == null)
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
    public Game Game { get; private set; }
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

        Root.Children.CollectionChanged += (o,e) => Gum.Forms.FormsUtilities.HandleRootCollectionChanged(Root,e);

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
    public GumProjectSave? Initialize(Game game, string? gumProjectFile = null)
    {
#if MONOGAME || KNI || FNA
        if (game.GraphicsDevice == null)
        {
            throw new InvalidOperationException(
                "game.GraphicsDevice cannot be null. " +
                "Be sure to call Initialize in the Game's Initialize method or later " +
                "so that the Game has a valid GrahicsDevice");
        }
#endif

        return InitializeInternal(game, game.GraphicsDevice, gumProjectFile, defaultVisualsVersion:
            Gum.Forms.DefaultVisualsVersion.Newest);
    }


    public void Initialize(Game game, Gum.Forms.DefaultVisualsVersion defaultVisualsVersion)
    {
        if (game.GraphicsDevice == null)
        {
            throw new InvalidOperationException(
                "game.GraphicsDevice cannot be null. " +
                "Be sure to call Initialize in the Game's Initialize method or later " +
                "so that the Game has a valid GrahicsDevice");
        }

        InitializeInternal(game, game.GraphicsDevice, defaultVisualsVersion:defaultVisualsVersion);
    }

    public void Initialize(Game game, SystemManagers systemManagers)
    {
        InitializeInternal(game, game.GraphicsDevice, systemManagers: systemManagers);
    }

    [Obsolete("Experimental - this API may change in future versions")]
    public void LoadAnimations()
    {
        var project = ObjectFinder.Self.GumProjectSave;

        foreach (var element in project.AllElements)
        {
            var animation = TryLoadAnimation(element);

            if(animation != null)
            {
                project.ElementAnimations.Add(animation);
            }
        }
    }

    private ElementAnimationsSave? TryLoadAnimation(ElementSave element)
    {
        string prefix = element is ScreenSave ? "Screens/" :
            element is ComponentSave ? "Components/" :
            element is StandardElementSave ? "StandardElements/" : string.Empty;

        var fileName = prefix + element.Name + "Animations.ganx";

        if(FileManager.FileExists(fileName))
        {
            var animation = FileManager.XmlDeserialize<ElementAnimationsSave>(fileName);
            animation.ElementName = element.Name;
            return animation;
        }
        return null;
    }

    [Obsolete("Initialize passing Game as the first parameter rather than GraphicsDevice. Using this method does not support non-(EN-US) keyboard layouts, and " +
        "does not support ALT+numeric key codes for accents in TextBoxes. This method will be removed in future versions of Gum")]
    public GumProjectSave? Initialize(GraphicsDevice graphicsDevice, string? gumProjectFile = null)
    {
        return InitializeInternal(null, graphicsDevice, gumProjectFile);
    }

    public bool IsInitialized { get; private set; }
    GumProjectSave? InitializeInternal(Game game, GraphicsDevice graphicsDevice, 
        string? gumProjectFile = null, 
        SystemManagers? systemManagers = null, 
        Gum.Forms.DefaultVisualsVersion defaultVisualsVersion = Gum.Forms.DefaultVisualsVersion.V1)
    {
        if(IsInitialized)
        {
            throw new InvalidOperationException("Initialize has already been called once. It cannot be called again");
        }
        IsInitialized = true;

        Game = game;
        RegisterRuntimeTypesThroughReflection();

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
#elif raylib

#endif

        FormsUtilities.InitializeDefaults(systemManagers: this.SystemManagers, defaultVisualsVersion: defaultVisualsVersion);


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
            if(FileManager.IsRelative(absoluteFile))
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
        NineSliceRuntime.DefaultSourceFile = GetString("SourceFile");

        float GetFloat (string variableName) => current.DefaultState.GetValueOrDefault<float>(variableName);
        string GetString(string varialbeName) => current.DefaultState.GetValueOrDefault<string>(varialbeName);
    }

    // In December 31, 2024 we moved to using ModuleInitializer 
    // More info: https://github.com/vchelaru/Gum/issues/275
    // Therefore, this is no longer needed. However, old projects
    // may still use this. Not sure when we can remove this, sometime
    // in the future....
    private void RegisterRuntimeTypesThroughReflection()
    {
        // Get the currently executing assembly
        Assembly executingAssembly = Assembly.GetEntryAssembly();

        // Get all types in the assembly
        var types = executingAssembly?.GetTypes();

        if(types != null)
        {
            foreach (Type type in types)
            {
                var method = type.GetMethod("RegisterRuntimeType", BindingFlags.Static | BindingFlags.Public);

                if (method != null)
                {
                    method.Invoke(null, null);
                }
            }
        }
    }

#endregion

    #region Update

    public void Update(GameTime gameTime)
    {
        Update(Game, gameTime);
    }

    public void Update(Game game, GameTime gameTime)
    {
        Gum.Forms.FormsUtilities.SetDimensionsToCanvas(this.Root);
        Update(game, gameTime, this.Root);

    }

    public void Update(Game game, GameTime gameTime, FrameworkElement root) =>
        Update(game, gameTime, root.Visual);

    public void Update(Game game, GameTime gameTime, GraphicalUiElement root)
    {
        DeferredQueue.ProcessPending();
        GameTime = gameTime;
        Gum.Forms.FormsUtilities.Update(game, gameTime, root);
        // SystemManagers.Activity (as of Sept 13, 2025) only 
        // performs Sprite animation internally. This is not a 
        // critical system, but unit tests cannot initialize a SystemManagers
        // because these require a graphics device. Therefore, we can tolerate
        // a null SystemManagers to simplify unit tests.
        this.SystemManagers?.Activity(gameTime.TotalGameTime.TotalSeconds);
        root.AnimateSelf(gameTime.ElapsedGameTime.TotalSeconds);
    }

    public void Update(Game game, GameTime gameTime, IEnumerable<GraphicalUiElement> roots)
    {
        DeferredQueue.ProcessPending();
        GameTime = gameTime;
        Gum.Forms.FormsUtilities.Update(game, gameTime, roots);
        this.SystemManagers.Activity(gameTime.TotalGameTime.TotalSeconds);
        foreach(var item in roots)
        {
            item.AnimateSelf(gameTime.ElapsedGameTime.TotalSeconds);
        }
    }

    #endregion

    #region Draw

    public void Draw()
    {
        SystemManagers.Default.Draw();
    }

    #endregion
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


    public static void AddChild(this GraphicalUiElement element, Gum.Forms.Controls.FrameworkElement child)
    {
        element.Children.Add(child.Visual);
    }

    public static void AddToRoot(this Gum.Forms.Controls.FrameworkElement element)
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

public static class FrameworkElementExtensionMethods
{

}