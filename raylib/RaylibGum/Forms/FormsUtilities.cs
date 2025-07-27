using Gum.Forms.Controls;
using Gum.Wireframe;
using RenderingLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Raylib_cs;
using Gum.Forms.DefaultVisuals;




#if RAYLIB
using Gum.GueDeriving;
using RaylibGum.Input;
#else
using  MonoGameGum.GueDeriving;
#endif

namespace Gum.Forms;

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
    V2
}

public class FormsUtilities
{
    static Cursor cursor;
    public static Cursor Cursor => cursor;

    static Keyboard keyboard;

    public static Keyboard Keyboard => keyboard;

    public static GamePad[] Gamepads { get; private set; } = new GamePad[4];



    internal static void InitializeDefaults(SystemManagers? systemManagers = null, DefaultVisualsVersion defaultVisualsVersion = DefaultVisualsVersion.V2)
    {
        systemManagers = systemManagers ?? SystemManagers.Default;

        if (systemManagers == null)
        {
            throw new InvalidOperationException("" +
                "You must call this method after initializing SystemManagers.Default, or you must explicitly specify a SystemsManager instance");
        }

        switch(defaultVisualsVersion)
        {
            case DefaultVisualsVersion.V2:
                Texture2D uiSpriteSheet = systemManagers.LoadEmbeddedTexture2d("UISpriteSheet.png").Value;
                Styling.ActiveStyle = new Styling(uiSpriteSheet);
                TryAdd(typeof(Button), typeof(ButtonVisual));
                TryAdd(typeof(CheckBox), typeof(CheckBoxVisual));
                TryAdd(typeof(Label), typeof(LabelVisual));
                TryAdd(typeof(ScrollBar), typeof(ScrollBarVisual));
                TryAdd(typeof(ScrollViewer), typeof(ScrollViewerVisual));
                TryAdd(typeof(Slider), typeof(SliderVisual));


                break;
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

        FrameworkElement.MainCursor = cursor;


        FrameworkElement.PopupRoot = CreateFullscreenContainer(nameof(FrameworkElement.PopupRoot), systemManagers);
        FrameworkElement.ModalRoot = CreateFullscreenContainer(nameof(FrameworkElement.ModalRoot), systemManagers);
    }

    static ContainerRuntime CreateFullscreenContainer(string name, SystemManagers systemManagers)
    {
        var container = new ContainerRuntime();

        container.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        container.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        container.Width = GraphicalUiElement.CanvasWidth;
        container.Height = GraphicalUiElement.CanvasHeight;
        container.Name = name;

        container.AddToManagers(systemManagers);

        return container;
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

        innerList.Clear();
        innerList.AddRange(roots);

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
            //cursor.CustomCursor = frameworkElementOver?.CustomCursor;
        }
        else if (didChangeFrameworkElement)
        {
            //cursor.CustomCursor = Cursors.Arrow;
        }
    }
}
