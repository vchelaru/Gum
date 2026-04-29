#if MONOGAME || KNI || FNA
#define XNALIKE
#endif
using Gum.DataTypes.Behaviors;
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

#if XNALIKE
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum.Forms.DefaultVisuals;
using MonoGameGum.GueDeriving;
using MonoGameGum.Input;
#else
using Gum.GueDeriving;
using Gum.Input;
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

#if XNALIKE
        Texture2D uiSpriteSheet = systemManagers.LoadEmbeddedTexture2d("UISpriteSheet.png")!;
#elif RAYLIB
        Texture2D uiSpriteSheet = systemManagers.LoadEmbeddedTexture2d("UISpriteSheet.png").Value;
#elif SOKOL
        Texture2D uiSpriteSheet = systemManagers.LoadEmbeddedTexture2d("UISpriteSheet.png")!;
#endif

        switch (defaultVisualsVersion)
        {
#if XNALIKE
            case DefaultVisualsVersion.V1:
                TryAdd(typeof(Button), (_, c) => new DefaultButtonRuntime(tryCreateFormsObject: c));
                TryAdd(typeof(CheckBox), (_, c) => new DefaultCheckboxRuntime(tryCreateFormsObject: c));
                TryAdd(typeof(ComboBox), (_, c) => new DefaultComboBoxRuntime(tryCreateFormsObject: c));
                TryAdd(typeof(Label), (_, c) => new DefaultLabelRuntime(tryCreateFormsObject: c));
                TryAdd(typeof(ListBox), (_, c) => new DefaultListBoxRuntime(tryCreateFormsObject: c));
                TryAdd(typeof(ListBoxItem), (_, c) => new DefaultListBoxItemRuntime(tryCreateFormsObject: c));
                TryAdd(typeof(Menu), (_, c) => new DefaultMenuRuntime(tryCreateFormsObject: c));
                TryAdd(typeof(MenuItem), (_, c) => new DefaultMenuItemRuntime(tryCreateFormsObject: c));
                TryAdd(typeof(PasswordBox), (_, c) => new DefaultPasswordBoxRuntime(tryCreateFormsObject: c));
                TryAdd(typeof(RadioButton), (_, c) => new DefaultRadioButtonRuntime(tryCreateFormsObject: c));
                TryAdd(typeof(ScrollBar), (_, c) => new DefaultScrollBarRuntime(tryCreateFormsObject: c));
                TryAdd(typeof(ScrollViewer), (_, c) => new DefaultScrollViewerRuntime(tryCreateFormsObject: c));
                TryAdd(typeof(TextBox), (_, c) => new DefaultTextBoxRuntime(tryCreateFormsObject: c));
                TryAdd(typeof(Slider), (_, c) => new DefaultSliderRuntime(tryCreateFormsObject: c));
                TryAdd(typeof(Splitter), (_, c) => new DefaultSplitterRuntime(tryCreateFormsObject: c));
                TryAdd(typeof(Window), (_, c) => new DefaultWindowRuntime(tryCreateFormsObject: c));
                Gum.Forms.DefaultVisuals.Styling.ActiveStyle = new(uiSpriteSheet);
                break;
#endif
            case DefaultVisualsVersion.V2:
                TryAdd(typeof(Button), (_, c) => new DefaultVisuals.ButtonVisual(tryCreateFormsObject: c));
                TryAdd(typeof(CheckBox), (_, c) => new DefaultVisuals.CheckBoxVisual(tryCreateFormsObject: c));
                TryAdd(typeof(ComboBox), (_, c) => new DefaultVisuals.ComboBoxVisual(tryCreateFormsObject: c));
#if XNALIKE || FRB
                TryAdd(typeof(ItemsControl), (_, c) => new DefaultVisuals.ItemsControlVisual(tryCreateFormsObject: c));
#endif
                TryAdd(typeof(Label), (_, c) => new DefaultVisuals.LabelVisual(tryCreateFormsObject: c));
                TryAdd(typeof(ListBox), (_, c) => new DefaultVisuals.ListBoxVisual(tryCreateFormsObject: c));
                TryAdd(typeof(ListBoxItem), (_, c) => new DefaultVisuals.ListBoxItemVisual(tryCreateFormsObject: c));
#if XNALIKE || FRB
                TryAdd(typeof(Menu), (_, c) => new DefaultVisuals.MenuVisual(tryCreateFormsObject: c));
                TryAdd(typeof(MenuItem), (_, c) => new DefaultVisuals.MenuItemVisual(tryCreateFormsObject: c));
                TryAdd(typeof(PasswordBox), (_, c) => new DefaultVisuals.PasswordBoxVisual(tryCreateFormsObject: c));
#endif
                TryAdd(typeof(RadioButton), (_, c) => new DefaultVisuals.RadioButtonVisual(tryCreateFormsObject: c));
                TryAdd(typeof(ScrollBar), (_, c) => new DefaultVisuals.ScrollBarVisual(tryCreateFormsObject: c));
                TryAdd(typeof(ScrollViewer), (_, c) => new DefaultVisuals.ScrollViewerVisual(tryCreateFormsObject: c));
                TryAdd(typeof(Slider), (_, c) => new DefaultVisuals.SliderVisual(tryCreateFormsObject: c));
                TryAdd(typeof(Splitter), (_, c) => new DefaultVisuals.SplitterVisual(tryCreateFormsObject: c));
#if XNALIKE || FRB
                TryAdd(typeof(TextBox), (_, c) => new DefaultVisuals.TextBoxVisual(tryCreateFormsObject: c));
#endif
                TryAdd(typeof(Window), (_, c) => new DefaultVisuals.WindowVisual(tryCreateFormsObject: c));
                Gum.Forms.DefaultVisuals.Styling.ActiveStyle = new(uiSpriteSheet);

                break;

            case DefaultVisualsVersion.V3:
                TryAdd(typeof(Button), (_, c) => new DefaultVisuals.V3.ButtonVisual(tryCreateFormsObject: c));
                TryAdd(typeof(CheckBox), (_, c) => new DefaultVisuals.V3.CheckBoxVisual(tryCreateFormsObject: c));
#if !FRB
                TryAdd(typeof(Gum.Forms.Controls.Games.DialogBox), (_, c) => new DefaultVisuals.V3.DialogBoxVisual(tryCreateFormsObject: c));
#endif
                TryAdd(typeof(ComboBox), (_, c) => new DefaultVisuals.V3.ComboBoxVisual(tryCreateFormsObject: c));
                TryAdd(typeof(ItemsControl), (_, c) => new DefaultVisuals.V3.ItemsControlVisual(tryCreateFormsObject: c));
                TryAdd(typeof(Label), (_, c) => new DefaultVisuals.V3.LabelVisual(tryCreateFormsObject: c));
                TryAdd(typeof(ListBox), (_, c) => new DefaultVisuals.V3.ListBoxVisual(tryCreateFormsObject: c));
                TryAdd(typeof(ListBoxItem), (_, c) => new DefaultVisuals.V3.ListBoxItemVisual(tryCreateFormsObject: c));
#if XNALIKE || FRB
                TryAdd(typeof(Menu), (_, c) => new DefaultVisuals.V3.MenuVisual(tryCreateFormsObject: c));
                TryAdd(typeof(MenuItem), (_, c) => new DefaultVisuals.V3.MenuItemVisual(tryCreateFormsObject: c));
                TryAdd(typeof(PasswordBox), (_, c) => new DefaultVisuals.V3.PasswordBoxVisual(tryCreateFormsObject: c));
#endif
                TryAdd(typeof(RadioButton), (_, c) => new DefaultVisuals.V3.RadioButtonVisual(tryCreateFormsObject: c));
                TryAdd(typeof(ScrollBar), (_, c) => new DefaultVisuals.V3.ScrollBarVisual(tryCreateFormsObject: c));
                TryAdd(typeof(ScrollViewer), (_, c) => new DefaultVisuals.V3.ScrollViewerVisual(tryCreateFormsObject: c));
                TryAdd(typeof(TextBox), (_, c) => new DefaultVisuals.V3.TextBoxVisual(tryCreateFormsObject: c));
                TryAdd(typeof(Slider), (_, c) => new DefaultVisuals.V3.SliderVisual(tryCreateFormsObject: c));
                TryAdd(typeof(Splitter), (_, c) => new DefaultVisuals.V3.SplitterVisual(tryCreateFormsObject: c));
                TryAdd(typeof(ToggleButton), (_, c) => new DefaultVisuals.V3.ToggleButtonVisual(tryCreateFormsObject: c));
                TryAdd(typeof(Window), (_, c) => new DefaultVisuals.V3.WindowVisual(tryCreateFormsObject: c));
                Gum.Forms.DefaultVisuals.V3.Styling.ActiveStyle = new(uiSpriteSheet);

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(defaultVisualsVersion), defaultVisualsVersion, null);
        }

        // Tooltip is registered across all default visuals versions — it's a passive overlay with
        // no V1/V2 equivalent, so the V3 visual is used regardless.
        TryAdd(typeof(Tooltip), (_, c) => new DefaultVisuals.V3.TooltipVisual(tryCreateFormsObject: c));

        void TryAdd(Type formsType, Func<object, bool, GraphicalUiElement> factory)
        {
            if (!FrameworkElement.DefaultFormsTemplates.ContainsKey(formsType))
            {
                FrameworkElement.DefaultFormsTemplates[formsType] = new VisualTemplate(factory);
            }
#if XNALIKE
            // This is needed until MonoGameGum.Forms goes away completely. It's now marked as obsolete with error as of November 2025
            if (formsType.FullName.StartsWith("MonoGameGum.Forms."))
            {
                var baseType = formsType.BaseType;

                if (baseType?.FullName.StartsWith("Gum.Forms.") == true && !FrameworkElement.DefaultFormsTemplates.ContainsKey(baseType))
                {
                    FrameworkElement.DefaultFormsTemplates[baseType] = new VisualTemplate(factory);
                }
            }
#endif
        }

#if XNALIKE
        cursor = new Cursor(game?.Window);
#else
        cursor = new Cursor();
#endif

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

    /// <summary>
    /// The list of root elements that were tested for events in the most recent Update call.
    /// Used by GetEventFailureReason to provide useful diagnostics in GumBatch scenarios
    /// where elements are not added to managers.
    /// </summary>
    internal static IReadOnlyList<GraphicalUiElement> LastEventRoots => _lastEventRoots;
    static List<GraphicalUiElement> _lastEventRoots = new List<GraphicalUiElement>();

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
        double gameTimeSeconds = gameTime.TotalGameTime.TotalSeconds;
#else
        double gameTimeSeconds = gameTime;
#endif
        cursor.Activity(gameTimeSeconds);
        keyboard.Activity(gameTimeSeconds);
        UpdateGamepads(gameTimeSeconds);
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

        _lastEventRoots.Clear();
        _lastEventRoots.AddRange(innerList);

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

        ToolTipService.Update(cursor, gameTimeSeconds);

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

    internal static void Uninitialize()
    {
        cursor = null;
        keyboard = null;
        Gamepads = new GamePad[4];
    }

    public static void RegisterFromFileFormRuntimeDefaults()
    {
        ElementSaveExtensions.InitialStateAppliedNotifier = gue =>
        {
            if (gue is InteractiveGue igue && igue.FormsControlAsObject is FrameworkElement fe)
            {
                fe.UpdateState();
            }
        };

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
            if (behaviorNames.Contains(StandardFormsBehaviorNames.ButtonBehaviorName))
            {
                ElementSaveExtensions.RegisterGueInstantiationType(
                    component.Name,
                    typeof(DefaultFromFileButtonRuntime), overwriteIfAlreadyExists: false);
            }
            else if (behaviorNames.Contains(StandardFormsBehaviorNames.CheckBoxBehaviorName))
            {
                ElementSaveExtensions.RegisterGueInstantiationType(
                    component.Name,
                    typeof(DefaultFromFileCheckBoxRuntime), overwriteIfAlreadyExists: false);
            }
            else if (categoryNames.Contains("ComboBoxCategory") || behaviorNames.Contains(StandardFormsBehaviorNames.ComboBoxBehaviorName))
            {
                ElementSaveExtensions.RegisterGueInstantiationType(
                    component.Name,
                    typeof(DefaultFromFileComboBoxRuntime), overwriteIfAlreadyExists: false);
            }
            else if (behaviorNames.Contains(StandardFormsBehaviorNames.ItemsControlBehaviorName))
            {
                ElementSaveExtensions.RegisterGueInstantiationType(
                    component.Name,
                    typeof(DefaultFromFileItemsControlRuntime), overwriteIfAlreadyExists: false);
            }
            else if (categoryNames.Contains("LabelCategory") || behaviorNames.Contains(StandardFormsBehaviorNames.LabelBehaviorName))
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
            else if (behaviorNames.Contains(StandardFormsBehaviorNames.ListBoxBehaviorName))
            {
                ElementSaveExtensions.RegisterGueInstantiationType(
                    component.Name,
                    typeof(DefaultFromFileListBoxRuntime), overwriteIfAlreadyExists: false);
            }
            else if (behaviorNames.Contains(StandardFormsBehaviorNames.ListBoxItemBehaviorName))
            {
                ElementSaveExtensions.RegisterGueInstantiationType(
                    component.Name,
                    typeof(DefaultFromFileListBoxItemRuntime), overwriteIfAlreadyExists: false);
            }
            else if (behaviorNames.Contains(StandardFormsBehaviorNames.MenuBehaviorName))
            {
#if XNALIKE || FRB
                ElementSaveExtensions.RegisterGueInstantiationType(
                    component.Name,
                    typeof(DefaultFromFileMenuRuntime), overwriteIfAlreadyExists: false);
#endif
            }
            else if (behaviorNames.Contains(StandardFormsBehaviorNames.MenuItemBehaviorName))
            {
#if XNALIKE || FRB
                ElementSaveExtensions.RegisterGueInstantiationType(
                    component.Name,
                    typeof(DefaultFromFileMenuItemRuntime), overwriteIfAlreadyExists: false);
#endif
            }
            else if (behaviorNames.Contains(StandardFormsBehaviorNames.PanelBehaviorName))
            {
                ElementSaveExtensions.RegisterGueInstantiationType(
                    component.Name,
                    typeof(DefaultFromFilePanelRuntime), overwriteIfAlreadyExists: false);
            }
            else if (behaviorNames.Contains(StandardFormsBehaviorNames.PasswordBoxBehaviorName))
            {
#if XNALIKE || FRB
                ElementSaveExtensions.RegisterGueInstantiationType(
                    component.Name,
                    typeof(DefaultFromFilePasswordBoxRuntime), overwriteIfAlreadyExists: false);
#endif
            }
            else if (behaviorNames.Contains(StandardFormsBehaviorNames.RadioButtonBehaviorName))
            {
                ElementSaveExtensions.RegisterGueInstantiationType(
                    component.Name,
                    typeof(DefaultFromFileRadioButtonRuntime), overwriteIfAlreadyExists: false);
            }
            else if (behaviorNames.Contains(StandardFormsBehaviorNames.ScrollBarBehaviorName))
            {
                ElementSaveExtensions.RegisterGueInstantiationType(
                    component.Name,
                    typeof(DefaultFromFileScrollBarRuntime), overwriteIfAlreadyExists: false);
            }
            else if (behaviorNames.Contains(StandardFormsBehaviorNames.ScrollViewerBehaviorName))
            {
                ElementSaveExtensions.RegisterGueInstantiationType(
                    component.Name,
                    typeof(DefaultFromFileScrollViewerRuntime), overwriteIfAlreadyExists: false);
            }
            else if (behaviorNames.Contains(StandardFormsBehaviorNames.SliderBehaviorName))
            {
                ElementSaveExtensions.RegisterGueInstantiationType(
                    component.Name,
                    typeof(DefaultFromFileSliderRuntime), overwriteIfAlreadyExists: false);
            }
            else if (behaviorNames.Contains(StandardFormsBehaviorNames.SplitterBehaviorName))
            {
                ElementSaveExtensions.RegisterGueInstantiationType(
                    component.Name,
                    typeof(DefaultFromFileSplitterRuntime), overwriteIfAlreadyExists: false);
            }
            else if (behaviorNames.Contains(StandardFormsBehaviorNames.StackPanelBehaviorName))
            {
                ElementSaveExtensions.RegisterGueInstantiationType(
                    component.Name,
                    typeof(DefaultFromFileStackPanelRuntime), overwriteIfAlreadyExists: false);
            }
            else if (behaviorNames.Contains(StandardFormsBehaviorNames.TextBoxBehaviorName))
            {
                ElementSaveExtensions.RegisterGueInstantiationType(
                    component.Name,
                    typeof(DefaultFromFileTextBoxRuntime), overwriteIfAlreadyExists: false);
            }
            else if (behaviorNames.Contains(StandardFormsBehaviorNames.TooltipBehaviorName))
            {
                ElementSaveExtensions.RegisterGueInstantiationType(
                    component.Name,
                    typeof(DefaultFromFileTooltipRuntime), overwriteIfAlreadyExists: false);
            }
            else if (behaviorNames.Contains(StandardFormsBehaviorNames.WindowBehaviorName))
            {
                ElementSaveExtensions.RegisterGueInstantiationType(
                    component.Name,
                    typeof(DefaultFromFileWindowRuntime), overwriteIfAlreadyExists: false);
            }
        }
    }
}
