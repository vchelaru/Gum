using Gum.Converters;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.GueDeriving;
using Gum.Renderables;
using Gum.Wireframe;
using Raylib_cs;
using RaylibGum;
using Gum.Input;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using static Raylib_cs.Raylib;

namespace Examples.Shapes;




public class BasicShapes
{

    static Texture2D texture;

    static GumService GumUI => GumService.Default;

    // Top-level "screens" — each one is a ContainerRuntime holding all of its visuals.
    // We swap which one is in the root with the SPACE key.
    static ContainerRuntime? rawVisualsScreen;
    static ContainerRuntime? formsControlsScreen;
    static ContainerRuntime? activeScreen;


    public static void Main()
    {
        // Initialization
        //--------------------------------------------------------------------------------------
        const int screenWidth = 1280;
        const int screenHeight = 720;

        // This tells Gum to use the entire screen
        GraphicalUiElement.CanvasWidth = screenWidth;
        GraphicalUiElement.CanvasHeight = screenHeight;


        InitWindow(screenWidth, screenHeight, "Basic shape and image drawing");

        //Load the image into the cpu
        Image image = LoadImage("resources/gum-logo-normal-64.png");

        //Transform it as a texture
        texture = LoadTextureFromImage(image);



        //Unload/release it
        UnloadImage(image);
        //--------------------------------------------------------------------------------------

        GumUI.Initialize();
        var standardTexture = SystemManagers.Default.LoadEmbeddedTexture2d("UISpriteSheet.png");

        InitializeStyling();

        rawVisualsScreen = BuildRawVisualsScreen();
        formsControlsScreen = BuildFormsControlsScreen();

        ShowScreen(rawVisualsScreen);

        // Main game loop
        while (!WindowShouldClose())
        {
            // Update
            //----------------------------------------------------------------------------------
            if (IsKeyPressed(KeyboardKey.Space))
            {
                ShowScreen(activeScreen == rawVisualsScreen ? formsControlsScreen : rawVisualsScreen);
            }
            //----------------------------------------------------------------------------------

            // Draw
            //----------------------------------------------------------------------------------
            BeginDrawing();
            ClearBackground(Color.SkyBlue);

            GumUI.Update(GetTime());

            GumUI.Draw();

            EndDrawing();

            Thread.Sleep(12);
            //----------------------------------------------------------------------------------
        }

        // De-Initialization
        //--------------------------------------------------------------------------------------
        CloseWindow();
        //--------------------------------------------------------------------------------------

    }

    private static void ShowScreen(ContainerRuntime screen)
    {
        if (activeScreen != null)
        {
            activeScreen.RemoveFromRoot();
        }
        activeScreen = screen;
        activeScreen.AddToRoot();
    }

    private static ContainerRuntime CreateScreenContainer()
    {
        var screen = new ContainerRuntime();
        screen.Width = 0;
        screen.Height = 0;
        screen.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        screen.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        return screen;
    }

    private static TextRuntime AddSwitchHint(ContainerRuntime screen)
    {
        var hint = new TextRuntime();
        hint.Text = "Press SPACE to switch screens";
        hint.XOrigin = HorizontalAlignment.Left;
        hint.YOrigin = VerticalAlignment.Bottom;
        hint.XUnits = GeneralUnitType.PixelsFromSmall;
        hint.YUnits = GeneralUnitType.PixelsFromLarge;
        hint.X = 8;
        hint.Y = -8;
        screen.AddChild(hint);
        return hint;
    }

    private static TextRuntime AddSectionLabel(GraphicalUiElement parent, string text)
    {
        var label = new TextRuntime();
        label.Text = text;
        label.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        label.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        label.Width = 0;
        label.Height = 0;
        parent.AddChild(label);
        return label;
    }

    private static ContainerRuntime BuildRawVisualsScreen()
    {
        var screen = CreateScreenContainer();

        var page = NewSection(Gum.Managers.ChildrenLayout.TopToBottomStack, spacing: 16);
        page.X = 16;
        page.Y = 16;
        screen.AddChild(page);

        BuildSpritesRow(page);
        BuildShapesRow(page);
        BuildNineSliceRow(page);

        AddSwitchHint(screen);

        return screen;
    }

    private static ContainerRuntime NewSection(Gum.Managers.ChildrenLayout layout, int spacing)
    {
        var section = new ContainerRuntime();
        section.Width = 0;
        section.Height = 0;
        section.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        section.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        section.ChildrenLayout = layout;
        section.StackSpacing = spacing;
        return section;
    }

    private static void BuildSpritesRow(ContainerRuntime page)
    {
        var section = NewSection(Gum.Managers.ChildrenLayout.TopToBottomStack, spacing: 4);
        page.AddChild(section);

        AddSectionLabel(section, "Sprites:");

        var row = NewSection(Gum.Managers.ChildrenLayout.LeftToRightStack, spacing: 8);
        section.AddChild(row);

        var sprite = new SpriteRuntime();
        row.AddChild(sprite);
        sprite.SourceFileName = "resources\\gum-logo-normal-64.png";

        var flippedH = new SpriteRuntime();
        row.AddChild(flippedH);
        flippedH.FlipHorizontal = true;
        flippedH.SourceFileName = "resources\\gum-logo-normal-64.png";

        var flippedV = new SpriteRuntime();
        row.AddChild(flippedV);
        flippedV.FlipVertical = true;
        flippedV.SourceFileName = "resources\\gum-logo-normal-64.png";
    }

    private static void BuildShapesRow(ContainerRuntime page)
    {
        var section = NewSection(Gum.Managers.ChildrenLayout.TopToBottomStack, spacing: 4);
        page.AddChild(section);

        AddSectionLabel(section, "Shapes:");

        var row = NewSection(Gum.Managers.ChildrenLayout.LeftToRightStack, spacing: 16);
        section.AddChild(row);

        var rectangle = new ColoredRectangleRuntime();
        rectangle.Width = 80;
        rectangle.Height = 80;
        rectangle.Color = new Color(80, 160, 220, 255);
        row.AddChild(rectangle);

        var circle = new CircleRuntime();
        circle.Radius = 40;
        circle.Color = new Color(255, 100, 50, 255);
        row.AddChild(circle);

        var bigCircle = new CircleRuntime();
        bigCircle.Radius = 60;
        bigCircle.Color = new Color(50, 180, 80, 255);
        row.AddChild(bigCircle);
    }

    private static void BuildNineSliceRow(ContainerRuntime page)
    {
        var section = NewSection(Gum.Managers.ChildrenLayout.TopToBottomStack, spacing: 4);
        page.AddChild(section);

        AddSectionLabel(section, "NineSlice:");

        var row = NewSection(Gum.Managers.ChildrenLayout.LeftToRightStack, spacing: 16);
        section.AddChild(row);

        var nineSlice = new NineSliceRuntime();
        nineSlice.Width = 160;
        nineSlice.Height = 80;
        nineSlice.SourceFileName = "resources\\ExampleSpriteFrame.png";
        row.AddChild(nineSlice);

        var tinted = new NineSliceRuntime();
        tinted.Width = 120;
        tinted.Height = 120;
        tinted.SourceFileName = "resources\\ExampleSpriteFrame.png";
        tinted.Color = new Color(255, 200, 100, 255);
        row.AddChild(tinted);
    }

    private static ContainerRuntime BuildFormsControlsScreen()
    {
        var screen = CreateScreenContainer();

        var container = new GraphicalUiElement(new InvisibleRenderable());
        screen.AddChild(container);
        container.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        container.WrapsChildren = true;
        container.StackSpacing = 2;
        container.Width = 0;
        container.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;

        container.Height = 0;
        container.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;

        container.ClipsChildren = true;

        var button = new Button();
        button.Width = 200;
        button.Text = "I'm a button";
        container.AddChild(button.Visual);

        var checkbox = new CheckBox();
        checkbox.Width = 200;
        checkbox.Text = "Check me";
        container.AddChild(checkbox.Visual);


        var slider = new Slider();
        container.AddChild(slider.Visual);

        slider.Minimum = 0;
        slider.Maximum = 30;
        slider.TicksFrequency = 1;
        slider.IsSnapToTickEnabled = true;
        slider.Width = 250;
        slider.ValueChanged += (_, _) =>
            Debug.WriteLine($"Value: {slider.Value}");
        slider.ValueChangeCompleted += (_, _) =>
            Debug.WriteLine($"Finished setting Value: {slider.Value}");

        var label = new Label();
        container.AddChild(label.Visual);
        label.Text = "This is a Gum label";

        var scrollViewer = new ScrollViewer();
        container.AddChild(scrollViewer.Visual);
        scrollViewer.Width = 200;
        scrollViewer.Height = 200;
        scrollViewer.Visual.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        scrollViewer.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        scrollViewer.InnerPanel.StackSpacing = 2;

        for (int i = 0; i < 30; i++)
        {
            var innerButton = new Button();
            scrollViewer.AddChild(innerButton);
            innerButton.Visual.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
            innerButton.Width = 0;
            innerButton.Text = "Button " + i;
            innerButton.Click += (_, _) =>
                innerButton.Text = DateTime.Now.ToString();
        }



        var stackPanel = new StackPanel();
        container.AddChild(stackPanel.Visual);

        var easyRadioButton = new RadioButton();
        stackPanel.AddChild(easyRadioButton);
        easyRadioButton.Text = "Easy";

        var mediumRadioButton = new RadioButton();
        stackPanel.AddChild(mediumRadioButton);
        mediumRadioButton.Text = "Medium";

        var hardRadioButton = new RadioButton();
        stackPanel.AddChild(hardRadioButton);
        hardRadioButton.Text = "Hard";

        var listBox = new ListBox();
        container.AddChild(listBox.Visual);
        listBox.Width = 300;
        listBox.Height = 200;

        var addButton = new Button();
        container.AddChild(addButton.Visual);
        addButton.Text = "Add to ListBox";
        addButton.Click += (s, e) =>
        {
            var newItem = $"Item {listBox.Items.Count} @ {DateTime.Now}";
            listBox.Items.Add(newItem);
            listBox.ScrollIntoView(newItem);
        };

        var comboBox = new ComboBox();
        container.AddChild(comboBox.Visual);
        for (int i = 0; i < 10; i++)
        {
            comboBox.Items.Add($"Item {i}");
        }


        var window = new Window();
        window.Anchor(Gum.Wireframe.Anchor.Center);
        window.Width = 300;
        window.Height = 200;
        screen.AddChild(window.Visual);

        var textInstance = new Label();
        textInstance.Dock(Gum.Wireframe.Dock.Top);
        textInstance.Y = 24;
        textInstance.Text = "Hello I am a message box";
        window.AddChild(textInstance);

        var windowButton = new Button();
        windowButton.Anchor(Gum.Wireframe.Anchor.Bottom);
        windowButton.Y = -10;
        windowButton.Text = "Close";
        window.AddChild(windowButton.Visual);
        windowButton.Click += (_, _) =>
        {
            window.Visual.Parent = null;
        };


        var splitterStackPanel = new StackPanel();
        container.AddChild(splitterStackPanel);
        splitterStackPanel.Spacing = 1;

        var listBox1 = new ListBox();
        splitterStackPanel.AddChild(listBox1);
        for (int i = 0; i < 10; i++)
        {
            listBox1.Items.Add("List Item " + i);
        }

        var splitter = new Splitter();
        splitterStackPanel.AddChild(splitter);
        splitter.Dock(Dock.FillHorizontally);
        splitter.Height = 5;

        var listBox2 = new ListBox();
        splitterStackPanel.AddChild(listBox2);
        for (int i = 0; i < 10; i++)
        {
            listBox2.Items.Add("List Item " + i);
        }

        AddSwitchHint(screen);

        return screen;
    }

    private static void InitializeStyling()
    {
        var font = LoadFontEx("resources/04B_30_.TTF", 24, null, 0);
        Styling.ActiveStyle.Text.Normal.SetValue("Font", font);
        Styling.ActiveStyle.Text.Strong.SetValue("Font", font);
        Styling.ActiveStyle.Text.Emphasis.SetValue("Font", font);
    }
}
