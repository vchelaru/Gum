#if MONOGAME || KNI || FNA
#define XNALIKE
#endif
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

#if XNALIKE
using MonoGameGum.GueDeriving;
using MonoGameGum.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
namespace MonoGameGum;
#elif RAYLIB
using Gum.GueDeriving;
using RaylibGum.Input;
using GameTime = double;
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

    public GameTime GameTime { get; private set; }

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

#if XNALIKE
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

        Root.Children.CollectionChanged += (o, e) => Gum.Forms.FormsUtilities.HandleRootCollectionChanged(Root, e);

        DeferredQueue = new DeferredActionQueue();
    }


#if XNALIKE
    /// <summary>
    /// Initializes Gum, optionally loading a Gum project.
    /// </summary>
    /// <param name="game">The game instance.</param>
    /// <param name="gumProjectFile">An optional project to load. If not specified, no project is loaded and Gum can be used "code only".</param>
    /// <returns>The loaded project, or null if no project is loaded</returns>
    public GumProjectSave? Initialize(Game game, string? gumProjectFile = null)
    {
        if (game.GraphicsDevice == null)
        {
            throw new InvalidOperationException(
                "game.GraphicsDevice cannot be null. " +
                "Be sure to call Initialize in the Game's Initialize method or later " +
                "so that the Game has a valid GrahicsDevice");
        }
        return InitializeInternal(
            game, game.GraphicsDevice,
            gumProjectFile,
            defaultVisualsVersion: Gum.Forms.DefaultVisualsVersion.Newest);
    }
#else
    /// <summary>
    /// Initializes Gum, optionally loading a Gum project.
    /// </summary>
    /// <param name="gumProjectFile">An optional project to load. If not specified, no project is loaded and Gum can be used "code only".</param>
    /// <returns>The loaded project, or null if no project is loaded</returns>
    public GumProjectSave Initialize(string gumProjectFile)
    {
        return InitializeInternal(
            gumProjectFile,
            defaultVisualsVersion: DefaultVisualsVersion.Newest)!;
    }
#endif

#if XNALIKE
    public void Initialize(Game game, Gum.Forms.DefaultVisualsVersion defaultVisualsVersion)
    {
        if (game.GraphicsDevice == null)
        {
            throw new InvalidOperationException(
                "game.GraphicsDevice cannot be null. " +
                "Be sure to call Initialize in the Game's Initialize method or later " +
                "so that the Game has a valid GrahicsDevice");
        }

        InitializeInternal(game, game.GraphicsDevice, defaultVisualsVersion: defaultVisualsVersion);
    }
    public void Initialize(Game game, SystemManagers systemManagers)
    {
        InitializeInternal(game, game.GraphicsDevice, systemManagers: systemManagers);
    }
#else
    public void Initialize(DefaultVisualsVersion defaultVisualsVersion = DefaultVisualsVersion.V2)
    {
        InitializeInternal(
            gumProjectFile: null,
            systemManagers: SystemManagers.Default,
            defaultVisualsVersion: defaultVisualsVersion);
    }
#endif

    [Obsolete("Experimental - this API may change in future versions")]
    public void LoadAnimations()
    {
        var project = ObjectFinder.Self.GumProjectSave;

        foreach (var element in project.AllElements)
        {
            var animation = TryLoadAnimation(element);

            if (animation != null)
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

        if (FileManager.FileExists(fileName))
        {
            var animation = FileManager.XmlDeserialize<ElementAnimationsSave>(fileName);
            animation.ElementName = element.Name;
            return animation;
        }
        return null;
    }

#if XNALIKE
    [Obsolete("Initialize passing Game as the first parameter rather than GraphicsDevice. Using this method does not support non-(EN-US) keyboard layouts, and " +
        "does not support ALT+numeric key codes for accents in TextBoxes. This method will be removed in June 2026")]
    public GumProjectSave? Initialize(GraphicsDevice graphicsDevice, string? gumProjectFile = null)
    {
        return InitializeInternal(null, graphicsDevice, gumProjectFile);
    }
#endif

    public bool IsInitialized { get; private set; }

    GumProjectSave? InitializeInternal(
#if XNALIKE
        Game game, GraphicsDevice graphicsDevice,
#endif
        string? gumProjectFile = null,
        SystemManagers? systemManagers = null,
        DefaultVisualsVersion defaultVisualsVersion = DefaultVisualsVersion.Newest)
    {
        if (IsInitialized)
        {
            throw new InvalidOperationException("Initialize has already been called once. It cannot be called again");
        }
        IsInitialized = true;

#if XNALIKE
        Game = game;
        RegisterRuntimeTypesThroughReflection();
#endif

        this.SystemManagers = systemManagers ?? new SystemManagers();
        if (systemManagers == null)
        {
            SystemManagers.Default = this.SystemManagers;
            ISystemManagers.Default = this.SystemManagers;
        }

#if XNALIKE
        this.SystemManagers.Initialize(graphicsDevice, fullInstantiation: true);
#elif RAYLIB
        this.SystemManagers.Initialize();
#endif

        FormsUtilities.InitializeDefaults(systemManagers: this.SystemManagers,
            defaultVisualsVersion: defaultVisualsVersion);

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

        float GetFloat(string variableName) => current.DefaultState.GetValueOrDefault<float>(variableName);
        string GetString(string varialbeName) => current.DefaultState.GetValueOrDefault<string>(varialbeName);
    }

#if XNALIKE
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

        if (types != null)
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
#endif
    #endregion


    #region Update

#if XNALIKE
    [Obsolete("Use the version that does not take a Game")]
    public void Update(Game game, GameTime gameTime, FrameworkElement root) => Update(gameTime, root.Visual);
    [Obsolete("Use the version that does not take a Game")]
    public void Update(Game game, GameTime gameTime) => Update(gameTime);
    [Obsolete("Use the version which does not take a Game")]
    public void Update(Game game, GameTime gameTime, GraphicalUiElement root) => Update(gameTime, root);
    [Obsolete("Use the version of this method which does not take a Game")]
    public void Update(Game game, GameTime gameTime, IEnumerable<GraphicalUiElement> roots) => Update(gameTime, roots);
#endif


#if XNALIKE
    /// <summary>
    /// Performs every-frame updates including updating root sizes to fill the entire screen, 
    /// cursor update, keyboard update, gamepad updates, and raising events on all controls.
    /// </summary>
    /// <param name="gameTime">The GameTime obtained from the Game class in the Update call.</param>
#else
    /// <summary>
    /// Performs every-frame updates including updating root sizes to fill the entire screen, 
    /// cursor update, keyboard update, gamepad updates, and raising events on all controls.
    /// </summary>
    /// <param name="gameTime">The total number of seconds passed since the game has started.</param>
#endif
    public void Update(GameTime gameTime)
    {
        Gum.Forms.FormsUtilities.SetDimensionsToCanvas(this.Root);

        Update(gameTime, this.Root);
    }
    List<GraphicalUiElement> roots = new List<GraphicalUiElement>();
    public void Update(GameTime totalGameTime, GraphicalUiElement root)
    {
        roots.Clear();
        roots.Add(root);

        Update(totalGameTime, roots);
    }


    public void Update(GameTime gameTime, IEnumerable<GraphicalUiElement> roots)
    { 
#if XNALIKE
        var difference = gameTime.ElapsedGameTime.TotalSeconds;
#else
        var difference = GameTime - gameTime;
#endif

        DeferredQueue.ProcessPending();
        GameTime = gameTime;
#if XNALIKE
        FormsUtilities.Update(this.Game, gameTime, roots);
#else
        FormsUtilities.Update(gameTime, roots);
#endif
        // SystemManagers.Activity (as of Sept 13, 2025) only 
        // performs Sprite animation internally. This is not a 
        // critical system, but unit tests cannot initialize a SystemManagers
        // because these require a graphics device. Therefore, we can tolerate
        // a null SystemManagers to simplify unit tests.
#if XNALIKE
        this.SystemManagers?.Activity(gameTime.TotalGameTime.TotalSeconds);
#endif
        foreach (var item in roots)
        {
            item.AnimateSelf(difference);
        }
    }

    #endregion

    public void Draw()
    {
        SystemManagers.Default.Draw();
    }
}

#region GraphicalUiElementExtensionMethods Class
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

#if !RAYLIB
    public static void AddChild(this GraphicalUiElement element, Gum.Forms.Controls.FrameworkElement child)
    {
        element.Children.Add(child.Visual);
    }
#endif

    public static void AddToRoot(this FrameworkElement element)
    {
        GumService.Default.Root.Children.Add(element.Visual);
    }
}

#endregion

public static class ElementSaveExtensionMethods
{
    public static GraphicalUiElement ToGraphicalUiElement(this ElementSave elementSave, SystemManagers? systemManagers = null)
    {
        return elementSave.ToGraphicalUiElement(systemManagers ?? SystemManagers.Default, addToManagers: false);
    }
}