using Gum.Forms.Controls;
using Gum.Forms.DefaultFromFileVisuals;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System.Collections.Specialized;
using Gum.GueDeriving;


#if RAYLIB
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
    Newest = V2,
}

public class FormsUtilities
{
    static ICursor cursor;

    public static Cursor Cursor => cursor as Cursor;

    public static void SetCursor(ICursor cursor)
    {
        FormsUtilities.cursor = cursor;
        FrameworkElement.MainCursor = cursor;
    }

    static Keyboard keyboard;

    public static Keyboard Keyboard => keyboard;

    public static GamePad[] Gamepads { get; private set; } = new GamePad[4];


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
    internal static void InitializeDefaults(SystemManagers? systemManagers = null, DefaultVisualsVersion defaultVisualsVersion = DefaultVisualsVersion.V2)
    {
        systemManagers = systemManagers ?? SystemManagers.Default;

        if (systemManagers == null)
        {
            throw new InvalidOperationException("" +
                "You must call this method after initializing SystemManagers.Default, or you must explicitly specify a SystemsManager instance");
        }

        // Is this needed?
        Texture2D uiSpriteSheet = systemManagers.LoadEmbeddedTexture2d("UISpriteSheet.png").Value;


        switch (defaultVisualsVersion)
        {
            case DefaultVisualsVersion.V2:
                DefaultVisuals.Styling.ActiveStyle = new DefaultVisuals.Styling(uiSpriteSheet);
                TryAdd(typeof(Button), typeof(DefaultVisuals.ButtonVisual));
                TryAdd(typeof(CheckBox), typeof(DefaultVisuals.CheckBoxVisual));
                TryAdd(typeof(ComboBox), typeof(DefaultVisuals.ComboBoxVisual));
                TryAdd(typeof(Label), typeof(DefaultVisuals.LabelVisual));
                TryAdd(typeof(ListBox), typeof(DefaultVisuals.ListBoxVisual));
                TryAdd(typeof(ListBoxItem), typeof(DefaultVisuals.ListBoxItemVisual));
                TryAdd(typeof(RadioButton), typeof(DefaultVisuals.RadioButtonVisual));
                TryAdd(typeof(ScrollBar), typeof(DefaultVisuals.ScrollBarVisual));
                TryAdd(typeof(ScrollViewer), typeof(DefaultVisuals.ScrollViewerVisual));
                TryAdd(typeof(Slider), typeof(DefaultVisuals.SliderVisual));
                TryAdd(typeof(Splitter), typeof(DefaultVisuals.SplitterVisual));
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
                //TryAdd(typeof(Menu), typeof(DefaultVisuals.V3.MenuVisual));
                //TryAdd(typeof(MenuItem), typeof(DefaultVisuals.V3.MenuItemVisual));
                //TryAdd(typeof(PasswordBox), typeof(DefaultVisuals.V3.PasswordBoxVisual));
                TryAdd(typeof(RadioButton), typeof(DefaultVisuals.V3.RadioButtonVisual));
                TryAdd(typeof(ScrollBar), typeof(DefaultVisuals.V3.ScrollBarVisual));
                TryAdd(typeof(ScrollViewer), typeof(DefaultVisuals.V3.ScrollViewerVisual));
                //TryAdd(typeof(TextBox), typeof(DefaultVisuals.V3.TextBoxVisual));
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
        }

        cursor = new Cursor();

        keyboard = new Keyboard();


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

        container.Children.CollectionChanged += (o,e) => HandleRootCollectionChanged (container, e);
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

    public static void Update(float gameTime, GraphicalUiElement rootGue)
    {
        innerRootList.Clear();
        if (rootGue != null)
        {
            innerRootList.Add(rootGue);
        }
        Update(gameTime, innerRootList);
    }
    public static void Update(float gameTime, IEnumerable<GraphicalUiElement> roots)
    {
        var shouldProcess = true;
        
        if (!shouldProcess)
        {
            return;
        }

        var frameworkElementOverBefore =
            cursor.WindowPushed?.FormsControlAsObject as FrameworkElement ??
            cursor.WindowOver?.FormsControlAsObject as FrameworkElement;

        cursor.Activity(gameTime);
        keyboard.Activity(gameTime);
        UpdateGamepads(gameTime);
        innerList.Clear();


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
                if (item is GraphicalUiElement itemAsGue)
                {
                    innerList.Add(itemAsGue);
                    // only the top-most element receives input
                    break;
                }
            }
        }
        else
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
                    if (layer.Renderables.Contains(FrameworkElement.PopupRoot.RenderableComponent) && layer.Renderables.Last() != FrameworkElement.PopupRoot.RenderableComponent)
                    {
                        layer.Remove(FrameworkElement.PopupRoot.RenderableComponent as IRenderableIpso);
                        layer.Add(FrameworkElement.PopupRoot.RenderableComponent as IRenderableIpso);
                    }
                }

                foreach (var item in FrameworkElement.PopupRoot.Children)
                {
                    if (item is GraphicalUiElement itemAsGue)
                    {
                        innerList.Add(itemAsGue);
                    }
                }
            }
        }

        GueInteractiveExtensionMethods.DoUiActivityRecursively(
            innerList, 
            cursor, 
            keyboard, 
            gameTime);


        var frameworkElementOver =
            cursor.WindowPushed?.FormsControlAsObject as FrameworkElement ??
            cursor.WindowOver?.FormsControlAsObject as FrameworkElement;

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

    private static void UpdateGamepads(double time)
    {
        // todo
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
#if !RAYLIB

                ElementSaveExtensions.RegisterGueInstantiationType(
                    component.Name,
                    typeof(DefaultFromFileTextBoxRuntime), overwriteIfAlreadyExists: false);
#endif
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
