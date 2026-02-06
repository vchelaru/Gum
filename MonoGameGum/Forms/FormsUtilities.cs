#if MONOGAME || KNI || FNA
#define XNALIKE
#endif
using Gum.Forms.Controls;
using Gum.Forms.DefaultFromFileVisuals;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

#if RAYLIB
using Gum.GueDeriving;
using Raylib_cs;
using RaylibGum.Input;
#else
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum.Forms.DefaultVisuals;
using MonoGameGum.GueDeriving;
using MonoGameGum.Input;
#endif

#if FRB
namespace MonoGameGum.Forms;
#else
namespace Gum.Forms;
#endif

/// <summary>
/// The version to use for default visuals in a code-only project.
/// </summary>
public enum DefaultVisualsVersion
{
    /// <summary>
    /// The first version introduced with the first version of Gum Forms.
    /// Most controls use solid colors and ColoredRectangles for their backgrounds.
    /// </summary>
    V1,
    /// <summary>
    /// The second version introduced mid 2025. This version uses NineSlices for backgrounds,
    /// and respects a centralized styling.
    /// </summary>
    V2,
    /// <summary>
    /// The third version introduced end of 2025. This version makes styling with colors easier.
    /// </summary>
    V3,

    /// <summary>
    /// Specifies that the newest version is used.
    /// </summary>
    /// <remarks>This value is an alias for the latest supported version. Use this option to ensure the most
    /// recent features and updates are applied.</remarks>
    Newest = V3,
}

public class FormsUtilities
{
    static ICursor cursor;

    public static Cursor? Cursor => cursor as Cursor;

    public static void SetCursor(ICursor cursor)
    {
        FormsUtilities.cursor = cursor;
        FrameworkElement.MainCursor = cursor;
    }

    static Keyboard keyboard;

    public static Keyboard Keyboard => keyboard;

    public static GamePad[] Gamepads { get; private set; } = new GamePad[4];


#if XNALIKE
    /// <summary>
    /// Initializes defaults to enable FlatRedBall Forms. This method should be called before using Forms.
    /// </summary>
    /// <remarks>
    /// Projects can make further customization to Forms such as by modifying the FrameworkElement.Root or the DefaultFormsComponents.
    /// </remarks>
    /// <param name="game">The Game instance, used for creating and updating input such as the Keyboard and Mouse</param>
    /// <param name="systemManagers">The optional system managers. If not specified, the default system managers are used. Games with a single SystemsManager
    /// do not need to provide one.</param>
    /// <param name="defaultVisualsVersion">The version of visuals. Changing between visuals can change the apperance, as well as the structure of the Visual objects.</param>
    public static void InitializeDefaults(Game? game = null, SystemManagers? systemManagers = null, DefaultVisualsVersion defaultVisualsVersion = DefaultVisualsVersion.V1)
#else
    /// <summary>
    /// Initializes defaults to enable FlatRedBall Forms. This method should be called before using Forms.
    /// </summary>
    /// <remarks>
    /// Projects can make further customization to Forms such as by modifying the FrameworkElement.Root or the DefaultFormsComponents.
    /// </remarks>
    /// <param name="systemManagers">The optional system managers. If not specified, the default system managers are used. Games with a single SystemsManager
    /// do not need to provide one.</param>
    /// <param name="defaultVisualsVersion">The version of visuals. Changing between visuals can change the apperance, as well as the structure of the Visual objects.</param>
    public static void InitializeDefaults(SystemManagers? systemManagers = null, DefaultVisualsVersion defaultVisualsVersion = DefaultVisualsVersion.V2)
#endif
    {
        systemManagers = systemManagers ?? SystemManagers.Default;

        if (systemManagers == null)
        {
            throw new InvalidOperationException("" +
                "You must call this method after initializing SystemManagers.Default, or you must explicitly specify a SystemsManager instance");
        }

#if RAYLIB
        Texture2D uiSpriteSheet = systemManagers.LoadEmbeddedTexture2d("UISpriteSheet.png").Value;
#else
        Texture2D uiSpriteSheet = systemManagers.LoadEmbeddedTexture2d("UISpriteSheet.png")!;
#endif

        switch (defaultVisualsVersion)
        {
#if XNALIKE
            case DefaultVisualsVersion.V1:
                TryAdd(typeof(Button), typeof(DefaultButtonRuntime));
                TryAdd(typeof(CheckBox), typeof(DefaultCheckboxRuntime));
                TryAdd(typeof(ComboBox), typeof(DefaultComboBoxRuntime));
                TryAdd(typeof(Label), typeof(DefaultLabelRuntime));
                TryAdd(typeof(ListBox), typeof(DefaultListBoxRuntime));
                TryAdd(typeof(ListBoxItem), typeof(DefaultListBoxItemRuntime));
                TryAdd(typeof(Menu), typeof(DefaultMenuRuntime));
                TryAdd(typeof(MenuItem), typeof(DefaultMenuItemRuntime));
                TryAdd(typeof(PasswordBox), typeof(DefaultPasswordBoxRuntime));
                TryAdd(typeof(RadioButton), typeof(DefaultRadioButtonRuntime));
                TryAdd(typeof(ScrollBar), typeof(DefaultScrollBarRuntime));
                TryAdd(typeof(ScrollViewer), typeof(DefaultScrollViewerRuntime));
                TryAdd(typeof(TextBox), typeof(DefaultTextBoxRuntime));
                TryAdd(typeof(Slider), typeof(DefaultSliderRuntime));
                TryAdd(typeof(Splitter), typeof(DefaultSplitterRuntime));
                TryAdd(typeof(Window), typeof(DefaultWindowRuntime));
                Gum.Forms.DefaultVisuals.Styling.ActiveStyle = new(uiSpriteSheet);
                break;
#endif
            case DefaultVisualsVersion.V2:
                TryAdd(typeof(Button), typeof(DefaultVisuals.ButtonVisual));
                TryAdd(typeof(CheckBox), typeof(DefaultVisuals.CheckBoxVisual));
                TryAdd(typeof(ComboBox), typeof(DefaultVisuals.ComboBoxVisual));
#if !RAYLIB
                TryAdd(typeof(ItemsControl), typeof(DefaultVisuals.ItemsControlVisual));
#endif
                TryAdd(typeof(Label), typeof(DefaultVisuals.LabelVisual));
                TryAdd(typeof(ListBox), typeof(DefaultVisuals.ListBoxVisual));
                TryAdd(typeof(ListBoxItem), typeof(DefaultVisuals.ListBoxItemVisual));
#if !RAYLIB
                TryAdd(typeof(Menu), typeof(DefaultVisuals.MenuVisual));
                TryAdd(typeof(MenuItem), typeof(DefaultVisuals.MenuItemVisual));
                TryAdd(typeof(PasswordBox), typeof(DefaultVisuals.PasswordBoxVisual));
#endif
                TryAdd(typeof(RadioButton), typeof(DefaultVisuals.RadioButtonVisual));
                TryAdd(typeof(ScrollBar), typeof(DefaultVisuals.ScrollBarVisual));
                TryAdd(typeof(ScrollViewer), typeof(DefaultVisuals.ScrollViewerVisual));
                TryAdd(typeof(Slider), typeof(DefaultVisuals.SliderVisual));
                TryAdd(typeof(Splitter), typeof(DefaultVisuals.SplitterVisual));
#if !RAYLIB
                TryAdd(typeof(TextBox), typeof(DefaultVisuals.TextBoxVisual));
#endif
                TryAdd(typeof(Window), typeof(DefaultVisuals.WindowVisual));
                Gum.Forms.DefaultVisuals.Styling.ActiveStyle = new(uiSpriteSheet);

                break;

            case DefaultVisualsVersion.V3:
                TryAdd(typeof(Button), typeof(DefaultVisuals.V3.ButtonVisual));
                TryAdd(typeof(CheckBox), typeof(DefaultVisuals.V3.CheckBoxVisual));
                TryAdd(typeof(ComboBox), typeof(DefaultVisuals.V3.ComboBoxVisual));
                TryAdd(typeof(ItemsControl), typeof(DefaultVisuals.V3.ItemsControlVisual));
                TryAdd(typeof(Label), typeof(DefaultVisuals.V3.LabelVisual));
                TryAdd(typeof(ListBox), typeof(DefaultVisuals.V3.ListBoxVisual));
                TryAdd(typeof(ListBoxItem), typeof(DefaultVisuals.V3.ListBoxItemVisual));
#if !RAYLIB
                TryAdd(typeof(Menu), typeof(DefaultVisuals.V3.MenuVisual));
                TryAdd(typeof(MenuItem), typeof(DefaultVisuals.V3.MenuItemVisual));
                TryAdd(typeof(PasswordBox), typeof(DefaultVisuals.V3.PasswordBoxVisual));
#endif
                TryAdd(typeof(RadioButton), typeof(DefaultVisuals.V3.RadioButtonVisual));
                TryAdd(typeof(ScrollBar), typeof(DefaultVisuals.V3.ScrollBarVisual));
                TryAdd(typeof(ScrollViewer), typeof(DefaultVisuals.V3.ScrollViewerVisual));
                TryAdd(typeof(TextBox), typeof(DefaultVisuals.V3.TextBoxVisual));
                TryAdd(typeof(Slider), typeof(DefaultVisuals.V3.SliderVisual));
                TryAdd(typeof(Splitter), typeof(DefaultVisuals.V3.SplitterVisual));
                TryAdd(typeof(Window), typeof(DefaultVisuals.V3.WindowVisual));
                Gum.Forms.DefaultVisuals.V3.Styling.ActiveStyle = new(uiSpriteSheet);

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(defaultVisualsVersion), defaultVisualsVersion, null);
        }

        void TryAdd(Type formsType, Type runtimeType)
        {
            if (!FrameworkElement.DefaultFormsTemplates.ContainsKey(formsType))
            {
                FrameworkElement.DefaultFormsTemplates[formsType] = new VisualTemplate(runtimeType);
            }
#if XNALIKE
            // This is needed until MonoGameGum.Forms goes away completely. It's now marked as obsolete with error as of November 2025
            if (formsType.FullName.StartsWith("MonoGameGum.Forms."))
            {
                var baseType = formsType.BaseType;

                if (baseType?.FullName.StartsWith("Gum.Forms.") == true && !FrameworkElement.DefaultFormsTemplates.ContainsKey(baseType))
                {
                    FrameworkElement.DefaultFormsTemplates[baseType] = new VisualTemplate(runtimeType);
                }
            }
#endif
        }

        cursor = new Cursor();

#if !FRB
        // This was added to MonoGame/raylib on 1/22/2026 to support
        // simplified behavior.
        ICursor.VisualOverBehavior = VisualOverBehavior.IfHasEventsIsTrue;
#endif

#if XNALIKE
        keyboard = new Keyboard(game);
#else
        keyboard = new Keyboard();
#endif

        for (int i = 0; i < Gamepads.Length; i++)
        {
            Gamepads[i] = new GamePad();
        }

        // Do an initial update to update connectivity
        UpdateGamepads(0);

        FrameworkElement.MainCursor = cursor;
        FrameworkElement.MainKeyboard = keyboard;


        FrameworkElement.PopupRoot = CreateFullscreenContainer(nameof(FrameworkElement.PopupRoot), systemManagers);
        FrameworkElement.ModalRoot = CreateFullscreenContainer(nameof(FrameworkElement.ModalRoot), systemManagers);
    }

    static ContainerRuntime CreateFullscreenContainer(string name, SystemManagers systemManagers)
    {
        var container = new ContainerRuntime();

        container.Children.CollectionChanged += (o, e) => HandleRootCollectionChanged(container, e);
        container.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        container.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        container.Width = GraphicalUiElement.CanvasWidth;
        container.Height = GraphicalUiElement.CanvasHeight;
        container.Name = name;

        container.AddToManagers(systemManagers);

        return container;
    }

    internal static void HandleRootCollectionChanged(InteractiveGue container, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Remove ||
            e.Action == NotifyCollectionChangedAction.Replace ||
            e.Action == NotifyCollectionChangedAction.Reset)
        {
            if (InteractiveGue.CurrentInputReceiver != null)
            {
                var recieverVisual = InteractiveGue.CurrentInputReceiver as InteractiveGue;
                if (InteractiveGue.CurrentInputReceiver is Gum.Forms.Controls.FrameworkElement frameworkElement)
                {
                    recieverVisual = frameworkElement.Visual;
                }

                if (recieverVisual != null)
                {
                    var removedElements = e.OldItems;

                    var topParent = GetRootElement(recieverVisual);

                    if (e.Action == NotifyCollectionChangedAction.Reset && topParent == container)
                    {
                        InteractiveGue.CurrentInputReceiver = null;
                    }
                    else if (removedElements?.Contains(topParent) == true)
                    {
                        InteractiveGue.CurrentInputReceiver = null;
                    }
                }
            }
        }
    }

    static GraphicalUiElement GetRootElement(GraphicalUiElement item)
    {
        if (item.Parent is GraphicalUiElement parentGue)
        {
            return GetRootElement(parentGue);
        }
        else
        {
            return item;
        }
    }

    static List<GraphicalUiElement> innerList = new List<GraphicalUiElement>();
    static List<GraphicalUiElement> innerRootList = new List<GraphicalUiElement>();

#if XNALIKE
    [Obsolete("Use the overload which takes a Game as the first argument, and pass the game instance.")]
    public static void Update(GameTime gameTime, GraphicalUiElement rootGue)
    {
        Update(null, gameTime, rootGue);
    }
#endif

#if XNALIKE
    public static void Update(Game game, GameTime gameTime, GraphicalUiElement rootGue)
#else
    public static void Update(double gameTime, GraphicalUiElement rootGue)
#endif
    {
        innerRootList.Clear();
        if (rootGue != null)
        {
            innerRootList.Add(rootGue);
        }
#if XNALIKE
        Update(game, gameTime, innerRootList);
#else
        Update(gameTime, innerRootList);
#endif
    }

#if XNALIKE
    public static void Update(Game game, GameTime gameTime, IEnumerable<GraphicalUiElement> roots)
#else
    public static void Update(double gameTime, IEnumerable<GraphicalUiElement> roots)
#endif
    {
#if XNALIKE
        // tolerate null games for now...
        var shouldProcess = game == null || game.IsActive;
#else
        var shouldProcess = true;
#endif

        if (!shouldProcess)
        {
            return;
        }

        var frameworkElementOverBefore =
            cursor.WindowPushed?.FormsControlAsObject as FrameworkElement ??
            cursor.VisualOver?.FormsControlAsObject as FrameworkElement;

#if XNALIKE
        cursor.Activity(gameTime.TotalGameTime.TotalSeconds);
        keyboard.Activity(gameTime.TotalGameTime.TotalSeconds, game);
        UpdateGamepads(gameTime.TotalGameTime.TotalSeconds);
#else
        cursor.Activity(gameTime);
        keyboard.Activity(gameTime);
        UpdateGamepads(gameTime);
#endif
        innerList.Clear();

        var didModalsProcessInput = false;
        if (FrameworkElement.ModalRoot.Children.Count > 0)
        {
#if FULL_DIAGNOSTICS
            if (FrameworkElement.ModalRoot.Managers == null)
            {
                throw new InvalidOperationException("The ModalRoot has a Managers property of null. Did you accidentally call RemoveFromManagers?");
            }
#endif
            SetDimensionsToCanvas(FrameworkElement.ModalRoot);

            // make sure this is the last:
            foreach (var layer in SystemManagers.Default.Renderer.Layers)
            {
                if (layer.Renderables.Contains(FrameworkElement.ModalRoot.RenderableComponent) && layer.Renderables.Last() != FrameworkElement.ModalRoot.RenderableComponent)
                {
                    layer.Remove(FrameworkElement.ModalRoot.RenderableComponent as IRenderableIpso);
                    layer.Add(FrameworkElement.ModalRoot.RenderableComponent as IRenderableIpso);
                }
            }

            for (int i = FrameworkElement.ModalRoot.Children.Count - 1; i > -1; i--)
            {
                var item = FrameworkElement.ModalRoot.Children[i];
                if (item.Visible)
                {
                    didModalsProcessInput = true;
                    innerList.Add(item);
                    // only the top-most element receives input
                    break;
                }
            }
        }

        if (!didModalsProcessInput)
        {
            if (roots != null)
            {
                innerList.AddRange(roots);
            }

            var isRootInRoots = roots?.Contains(FrameworkElement.PopupRoot) == true;

            if (!isRootInRoots && FrameworkElement.PopupRoot.Children.Count > 0)
            {
#if FULL_DIAGNOSTICS
                if (FrameworkElement.PopupRoot.Managers == null)
                {
                    throw new InvalidOperationException("The PopupRoot has a Managers property of null. Did you accidentally call RemoveFromManagers?");
                }
#endif

                SetDimensionsToCanvas(FrameworkElement.PopupRoot);
                // make sure this is the last:
                foreach (var layer in SystemManagers.Default.Renderer.Layers)
                {
                    if (layer.Renderables.Contains(FrameworkElement.PopupRoot.RenderableComponent) &&
                        layer.Renderables.Last() != FrameworkElement.PopupRoot.RenderableComponent)
                    {
                        layer.Remove(FrameworkElement.PopupRoot.RenderableComponent as IRenderableIpso);
                        layer.Add(FrameworkElement.PopupRoot.RenderableComponent as IRenderableIpso);
                    }
                }

                foreach (var item in FrameworkElement.PopupRoot.Children)
                {
                    innerList.Add(item);
                }
            }
        }

#if XNALIKE
        GueInteractiveExtensionMethods.DoUiActivityRecursively(
            innerList,
            cursor,
            keyboard,
            gameTime.TotalGameTime.TotalSeconds);
#else
        GueInteractiveExtensionMethods.DoUiActivityRecursively(
            innerList,
            cursor,
            keyboard,
            gameTime);
#endif

        var frameworkElementOver =
            cursor.WindowPushed?.FormsControlAsObject as FrameworkElement ??
            cursor.VisualOver?.FormsControlAsObject as FrameworkElement;

        // It's possible that a cursor pushes on a control, which would set its state to Pushed. After the cursor releases,
        // the control is no longer pushed, so it should update its state to reflect that it is no longer pushed, such as
        // by showing hover or focused state. This need was uncovered by the MonoGame iOS sample which focused the slider when
        // hovering, but the state never got updated on release.
        if (cursor.PrimaryClick)
        {
            if (InteractiveGue.CurrentInputReceiver is FrameworkElement frameworkElementInputReceiver)
            {
                frameworkElementInputReceiver.UpdateState();
            }
            if (frameworkElementOver != null && frameworkElementOver != InteractiveGue.CurrentInputReceiver)
            {
                frameworkElementOver.UpdateState();
            }
        }

        var didChangeFrameworkElement = frameworkElementOver != frameworkElementOverBefore;

        if (frameworkElementOver?.IsEnabled == true && frameworkElementOver.CustomCursor != null)
        {
            cursor.CustomCursor = frameworkElementOver?.CustomCursor;
        }
        else if (didChangeFrameworkElement)
        {
            cursor.CustomCursor = Cursors.Arrow;
        }
    }

    private static void UpdateGamepads(double time)
    {
#if XNALIKE
        for (int i = 0; i < Gamepads.Length; i++)
        {
#if FNA
            var gamepadState = Microsoft.Xna.Framework.Input.GamePad.GetState((PlayerIndex)i);
#else
            var gamepadState = Microsoft.Xna.Framework.Input.GamePad.GetState((int)i);
#endif
            Gamepads[i].Activity(gamepadState, time);
        }
#endif

#if FULL_DIAGNOSTICS
        var hashSet = FrameworkElement.GamePadsForUiControl.ToHashSet();

        if (hashSet.Count != FrameworkElement.GamePadsForUiControl.Count)
        {
            throw new InvalidOperationException("The same gamepad has been added to FrameworkElement.GamePadsForUiControl multiple times. " +
                "This should not be done");
        }
#endif
    }

    internal static void SetDimensionsToCanvas(InteractiveGue container)
    {
        // Just to be safe, we'll set X and Y:
        container.X = 0;
        container.Y = 0;
        container.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        container.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        container.Width = GraphicalUiElement.CanvasWidth;
        container.Height = GraphicalUiElement.CanvasHeight;
    }

    public static void RegisterFromFileFormRuntimeDefaults()
    {
#if FULL_DIAGNOSTICS
        if (ObjectFinder.Self.GumProjectSave == null)
        {
            throw new InvalidOperationException("A Gum project (gumx) must be loaded and assigned to" +
                "ObjectFinder.Self.GumProjectSave before making this call");
        }
#endif
        // Some thoughts about this method:
        // 1. We can probably be more efficient 
        //    here by doing a single loop for categories
        //    and behaviors rather than calling Any multiple
        //    times.
        // 2. I believe Gum Forms was written before behaviors
        //    were used. Therefore a lot here use categories instead
        //    of behaviors. New items (like Menu) are using behaviors
        //    and old controls should proably be migrated over if we have
        //    any conflicts.
        foreach (var component in ObjectFinder.Self.GumProjectSave.Components)
        {
            var categoryNames = component.Categories.Select(item => item.Name).ToList();
            var behaviorNames = component.Behaviors.Select(item => item.BehaviorName).ToList();
            if (behaviorNames.Contains("ButtonBehavior"))
            {
                ElementSaveExtensions.RegisterGueInstantiationType(
                    component.Name,
                    typeof(DefaultFromFileButtonRuntime), overwriteIfAlreadyExists: false);
            }
            else if (behaviorNames.Contains("CheckBoxBehavior"))
            {
                ElementSaveExtensions.RegisterGueInstantiationType(
                    component.Name,
                    typeof(DefaultFromFileCheckBoxRuntime), overwriteIfAlreadyExists: false);
            }
            else if (categoryNames.Contains("ComboBoxCategory") || behaviorNames.Contains("ComboBoxBehavior"))
            {
                ElementSaveExtensions.RegisterGueInstantiationType(
                    component.Name,
                    typeof(DefaultFromFileComboBoxRuntime), overwriteIfAlreadyExists: false);
            }
            else if (behaviorNames.Contains("ItemsControlBehavior"))
            {
                ElementSaveExtensions.RegisterGueInstantiationType(
                    component.Name,
                    typeof(DefaultFromFileItemsControlRuntime), overwriteIfAlreadyExists: false);
            }
            else if (categoryNames.Contains("LabelCategory") || behaviorNames.Contains("LabelBehavior"))
            {
                if (component.BaseType == "Text")
                {
                    ElementSaveExtensions.RegisterGueInstantiationType(
                        component.Name,
                        typeof(DefaultFromFileLabelTextRuntime), overwriteIfAlreadyExists: false);
                }
                else
                {
                    ElementSaveExtensions.RegisterGueInstantiationType(
                        component.Name,
                        typeof(DefaultFromFileLabelRuntime), overwriteIfAlreadyExists: false);
                }
            }
            else if (behaviorNames.Contains("ListBoxBehavior"))
            {
                ElementSaveExtensions.RegisterGueInstantiationType(
                    component.Name,
                    typeof(DefaultFromFileListBoxRuntime), overwriteIfAlreadyExists: false);
            }
            else if (behaviorNames.Contains("ListBoxItemBehavior"))
            {
                ElementSaveExtensions.RegisterGueInstantiationType(
                    component.Name,
                    typeof(DefaultFromFileListBoxItemRuntime), overwriteIfAlreadyExists: false);
            }
            else if (behaviorNames.Contains("MenuBehavior"))
            {
#if !RAYLIB
                ElementSaveExtensions.RegisterGueInstantiationType(
                    component.Name,
                    typeof(DefaultFromFileMenuRuntime), overwriteIfAlreadyExists: false);
#endif
            }
            else if (behaviorNames.Contains("MenuItemBehavior"))
            {
#if !RAYLIB
                ElementSaveExtensions.RegisterGueInstantiationType(
                    component.Name,
                    typeof(DefaultFromFileMenuItemRuntime), overwriteIfAlreadyExists: false);
#endif
            }
            else if (behaviorNames.Contains("PanelBehavior"))
            {
                ElementSaveExtensions.RegisterGueInstantiationType(
                    component.Name,
                    typeof(DefaultFromFilePanelRuntime), overwriteIfAlreadyExists: false);
            }
            else if (behaviorNames.Contains("PasswordBoxBehavior"))
            {
#if !RAYLIB
                ElementSaveExtensions.RegisterGueInstantiationType(
                    component.Name,
                    typeof(DefaultFromFilePasswordBoxRuntime), overwriteIfAlreadyExists: false);
#endif
            }
            else if (behaviorNames.Contains("RadioButtonBehavior"))
            {
                ElementSaveExtensions.RegisterGueInstantiationType(
                    component.Name,
                    typeof(DefaultFromFileRadioButtonRuntime), overwriteIfAlreadyExists: false);
            }
            else if (behaviorNames.Contains("ScrollBarBehavior"))
            {
                ElementSaveExtensions.RegisterGueInstantiationType(
                    component.Name,
                    typeof(DefaultFromFileScrollBarRuntime), overwriteIfAlreadyExists: false);
            }
            else if (behaviorNames.Contains("ScrollViewerBehavior"))
            {
                ElementSaveExtensions.RegisterGueInstantiationType(
                    component.Name,
                    typeof(DefaultFromFileScrollViewerRuntime), overwriteIfAlreadyExists: false);
            }
            else if (behaviorNames.Contains("SliderBehavior"))
            {
                ElementSaveExtensions.RegisterGueInstantiationType(
                    component.Name,
                    typeof(DefaultFromFileSliderRuntime), overwriteIfAlreadyExists: false);
            }
            else if (behaviorNames.Contains("SplitterBehavior"))
            {
                ElementSaveExtensions.RegisterGueInstantiationType(
                    component.Name,
                    typeof(DefaultFromFileSplitterRuntime), overwriteIfAlreadyExists: false);
            }
            else if (behaviorNames.Contains("StackPanelBehavior"))
            {
                ElementSaveExtensions.RegisterGueInstantiationType(
                    component.Name,
                    typeof(DefaultFromFileStackPanelRuntime), overwriteIfAlreadyExists: false);
            }
            else if (behaviorNames.Contains("TextBoxBehavior"))
            {
                ElementSaveExtensions.RegisterGueInstantiationType(
                    component.Name,
                    typeof(DefaultFromFileTextBoxRuntime), overwriteIfAlreadyExists: false);
            }
            else if (behaviorNames.Contains("WindowBehavior"))
            {
                ElementSaveExtensions.RegisterGueInstantiationType(
                    component.Name,
                    typeof(DefaultFromFileWindowRuntime), overwriteIfAlreadyExists: false);
            }
        }
    }
}
