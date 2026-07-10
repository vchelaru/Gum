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
using Gum.Forms.DefaultVisuals;
using Gum.GueDeriving;
#else
using Gum.GueDeriving;
#endif
// Concrete per-platform input types (Cursor / Keyboard / GamePadDriver) are no longer referenced
// here: input creation is delegated to IGumService (CreateCursor / CreateKeyboard / ApplyGamePadState)
// so this shared file compiles on backends without those types (e.g. Skia). GamePad is used only as
// the fully-qualified platform-neutral Gum.Input.GamePad holder.

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
    [Obsolete("V1 default visuals are legacy. Use DefaultVisualsVersion.V3 (or .Newest). Slated for removal in a future release.")]
    V1,
    /// <summary>
    /// The second version introduced mid 2025. This version uses NineSlices for backgrounds,
    /// and respects a centralized styling.
    /// </summary>
    [Obsolete("V2 default visuals are legacy. Use DefaultVisualsVersion.V3 (or .Newest). Slated for removal in a future release.")]
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

    public static ICursor Cursor => cursor;

    public static void SetCursor(ICursor cursor)
    {
        FormsUtilities.cursor = cursor;
        FrameworkElement.MainCursor = cursor;
    }

    static IInputReceiverKeyboard keyboard;

    public static IInputReceiverKeyboard Keyboard => keyboard;

    // Typed explicitly as Gum.Input.GamePad (the platform-neutral holder in GumCommon) rather
    // than relying on the per-platform `using` so MonoGame, Raylib, and Sokol resolve to the
    // same type. Each platform is fed by its own same-named GamePadDriver.Apply(GamePad, int,
    // double), resolved unqualified via the per-platform `using` block at the top of this file
    // -- see UpdateGamepads below (issue #3559).
    public static Gum.Input.GamePad[] Gamepads { get; private set; } = new Gum.Input.GamePad[4];


    // The V1/V2 default-visual classes and the DefaultVisualsVersion.V1/V2 enum members are now
    // [Obsolete]. InitializeDefaults still supports registering them as legacy defaults, so the
    // default parameter values and the V1/V2 switch cases below intentionally reference the
    // obsolete members. Suppress CS0618 for that legacy-support region.
#pragma warning disable CS0618
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
#if XNALIKE || FRB || RAYLIB
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
#pragma warning restore CS0618

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

        // Input creation is delegated to the active runtime's IGumService so this shared file no
        // longer references any concrete per-platform input type (which lets it compile on backends
        // that have no such types, e.g. Skia). The runtime's override bakes in whatever platform
        // context it needs (e.g. MonoGame's Game/GameWindow for touch-offset math). Default is set
        // before this method runs -- see each runtime's Initialize (ordering is asserted there) --
        // and its input-capable runtimes return a non-null cursor/keyboard, hence the null-forgiveness.
        IGumService service = IGumService.Default!;
        cursor = service.CreateCursor()!;

#if !FRB
        // This was added to MonoGame/raylib on 1/22/2026 to support
        // simplified behavior.
        ICursor.VisualOverBehavior = VisualOverBehavior.IfHasEventsIsTrue;
#endif

        keyboard = service.CreateKeyboard()!;

        for (int i = 0; i < Gamepads.Length; i++)
        {
            Gamepads[i] = new Gum.Input.GamePad();
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

        RefreshPopupModalRootPairs();

        // The global pair is checked first so it keeps priority (matching prior behavior) if a
        // custom pair registered via FrameworkElement.AdditionalPopupRootPairs also has an open modal.
        // A pair is only a candidate winner if it has a visible top child — otherwise (e.g. every
        // child mid fade-out) it wouldn't have received input under the old single-root behavior
        // either, so the search continues to the next pair rather than starving one that does.
        GraphicalUiElement exclusiveModalItem = null;
        for (int i = 0; i < popupModalRootPairs.Count && exclusiveModalItem == null; i++)
        {
            var modalRoot = popupModalRootPairs[i].modalRoot;
            for (int j = modalRoot.Children.Count - 1; j > -1; j--)
            {
                if (modalRoot.Children[j].Visible)
                {
                    exclusiveModalItem = modalRoot.Children[j];
                    break;
                }
            }
        }

        // Raised in reverse priority order so the highest-priority populated modal root (the one
        // that will actually receive input, i.e. exclusiveModalItem's root) is raised last and ends
        // up visually on top — otherwise a lower-priority pair raised after it would cover it up.
        for (int i = popupModalRootPairs.Count - 1; i > -1; i--)
        {
            var modalRoot = popupModalRootPairs[i].modalRoot;
            if (modalRoot.Children.Count > 0)
            {
#if FULL_DIAGNOSTICS
                if (modalRoot.Managers == null)
                {
                    throw new InvalidOperationException("The ModalRoot has a Managers property of null. Did you accidentally call RemoveFromManagers?");
                }
#endif
                SetDimensionsToCanvas(modalRoot);

                // make sure this is the last:
                foreach (var layer in SystemManagers.Default.Renderer.Layers)
                {
                    if (layer.Renderables.Contains(modalRoot.RenderableComponent) && layer.Renderables.Last() != modalRoot.RenderableComponent)
                    {
                        layer.Remove(modalRoot.RenderableComponent as IRenderableIpso);
                        layer.Add(modalRoot.RenderableComponent as IRenderableIpso);
                    }
                }
            }
        }

        if (exclusiveModalItem != null)
        {
            didModalsProcessInput = true;
            // only the top-most element receives input
            innerList.Add(exclusiveModalItem);
        }

        if (!didModalsProcessInput)
        {
            if (roots != null)
            {
                innerList.AddRange(roots);
            }

            // Note: like the modal loop above, a later pair's popup root ends up raised on top of
            // an earlier one's when both have children in the same frame — there's no exclusivity
            // concept for popups (unlike modals), so this ordering is unenforced but benign.
            foreach (var (popupRoot, _) in popupModalRootPairs)
            {
                var isRootInRoots = roots?.Contains(popupRoot) == true;

                if (!isRootInRoots && popupRoot.Children.Count > 0)
                {
#if FULL_DIAGNOSTICS
                    if (popupRoot.Managers == null)
                    {
                        throw new InvalidOperationException("The PopupRoot has a Managers property of null. Did you accidentally call RemoveFromManagers?");
                    }
#endif

                    SetDimensionsToCanvas(popupRoot);
                    // make sure this is the last:
                    foreach (var layer in SystemManagers.Default.Renderer.Layers)
                    {
                        if (layer.Renderables.Contains(popupRoot.RenderableComponent) &&
                            layer.Renderables.Last() != popupRoot.RenderableComponent)
                        {
                            layer.Remove(popupRoot.RenderableComponent as IRenderableIpso);
                            layer.Add(popupRoot.RenderableComponent as IRenderableIpso);
                        }
                    }

                    foreach (var item in popupRoot.Children)
                    {
                        innerList.Add(item);
                    }
                }
            }
        }

        GueInteractiveExtensionMethods.DoUiActivityRecursively(
            innerList,
            cursor,
            keyboard,
            gameTimeSeconds);

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
        for (int i = 0; i < Gamepads.Length; i++)
        {
            // The per-frame gamepad driver is delegated to the active runtime's IGumService so this
            // shared file references no concrete per-platform driver type (issue #3559 / Skia support).
            IGumService.Default!.ApplyGamePadState(Gamepads[i], i, time);
        }

#if FULL_DIAGNOSTICS
        var hashSet = FrameworkElement.GamePadsForUiControl.ToHashSet();

        if (hashSet.Count != FrameworkElement.GamePadsForUiControl.Count)
        {
            throw new InvalidOperationException("The same gamepad has been added to FrameworkElement.GamePadsForUiControl multiple times. " +
                "This should not be done");
        }
#endif
    }

    static readonly List<(InteractiveGue popupRoot, InteractiveGue modalRoot)> popupModalRootPairs = new();

    static void RefreshPopupModalRootPairs()
    {
        popupModalRootPairs.Clear();
        popupModalRootPairs.Add((FrameworkElement.PopupRoot, FrameworkElement.ModalRoot));
        popupModalRootPairs.AddRange(FrameworkElement.AdditionalPopupRootPairs);
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
        Gamepads = new Gum.Input.GamePad[4];
    }

    public static void RegisterFromFileFormRuntimeDefaults()
    {
        ElementSaveExtensions.InitialStateAppliedNotifier = gue =>
        {
            if (gue is InteractiveGue igue && igue.FormsControlAsObject is FrameworkElement fe)
            {
                BehaviorFormsPropertyApplier.Apply(fe, igue);
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
#if XNALIKE || FRB || RAYLIB
                ElementSaveExtensions.RegisterGueInstantiationType(
                    component.Name,
                    typeof(DefaultFromFileMenuRuntime), overwriteIfAlreadyExists: false);
#endif
            }
            else if (behaviorNames.Contains(StandardFormsBehaviorNames.MenuItemBehaviorName))
            {
#if XNALIKE || FRB || RAYLIB
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
#if XNALIKE || FRB || RAYLIB
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
