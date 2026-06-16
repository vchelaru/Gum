using Gum.Converters;
using Gum.DataTypes;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using Raylib_cs;
using RenderingLibrary.Graphics;
using System.Diagnostics;
using WireframeDock = Gum.Wireframe.Dock;

namespace Examples.Shapes;

internal class FormsControlsScreen : FrameworkElement
{
    private readonly FrameworkElement _initialFocusControl;

    public FormsControlsScreen() : base(new ContainerRuntime())
    {
        Dock(Gum.Wireframe.Dock.Fill);

        // Must be an InteractiveGue (ContainerRuntime), not a raw GraphicalUiElement: focus
        // tabbing walks the focused control's parent via `Parent as InteractiveGue`, so a
        // non-interactive parent yields no siblings and gamepad/keyboard navigation no-ops.
        var container = new ContainerRuntime();
        this.AddChild(container);
        container.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        container.WrapsChildren = true;
        container.StackSpacing = 5;
        container.Width = 0;
        container.WidthUnits = DimensionUnitType.RelativeToParent;
        container.Height = 0;
        container.HeightUnits = DimensionUnitType.RelativeToParent;
        container.ClipsChildren = true;

        var button = new Button();
        button.Width = 200;
        button.Text = "I'm a button";
        container.AddChild(button.Visual);

        var textBox = new TextBox();
        container.AddChild(textBox.Visual);
        textBox.Width = 250;
        textBox.Placeholder = "Type here…";

        // The TextBox is the starting control given focus when this screen is shown (its
        // blinking caret makes the current gamepad focus easy to see). Program applies the
        // focus after the screen is added — see FocusInitialControl and
        // https://docs.flatredball.com/gum/code/events-and-interactivity/gamepad-support.
        _initialFocusControl = textBox;

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
        scrollViewer.Visual.WidthUnits = DimensionUnitType.Absolute;
        scrollViewer.Visual.HeightUnits = DimensionUnitType.Absolute;
        scrollViewer.InnerPanel.StackSpacing = 2;

        for (int i = 0; i < 30; i++)
        {
            var innerButton = new Button();
            scrollViewer.AddChild(innerButton);
            innerButton.Visual.WidthUnits = DimensionUnitType.RelativeToParent;
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

        // Menu / MenuItem / PasswordBox / Image route through the V3 default visuals that
        // were enabled on raylib in issue #3174 — included here so the gallery exercises them.
        var menu = new Menu();
        container.AddChild(menu.Visual);
        var fileMenuItem = new MenuItem { Header = "File" };
        fileMenuItem.Items.Add(new MenuItem { Header = "New" });
        fileMenuItem.Items.Add(new MenuItem { Header = "Open" });
        var recentMenuItem = new MenuItem { Header = "Recent" };
        recentMenuItem.Items.Add(new MenuItem { Header = "Project A" });
        recentMenuItem.Items.Add(new MenuItem { Header = "Project B" });
        fileMenuItem.Items.Add(recentMenuItem);
        menu.Items.Add(fileMenuItem);
        var editMenuItem = new MenuItem { Header = "Edit" };
        editMenuItem.Items.Add(new MenuItem { Header = "Copy" });
        editMenuItem.Items.Add(new MenuItem { Header = "Paste" });
        menu.Items.Add(editMenuItem);

        var passwordBox = new PasswordBox();
        container.AddChild(passwordBox.Visual);
        passwordBox.Width = 250;
        passwordBox.Placeholder = "Password…";

        var image = new Gum.Forms.Controls.Image();
        container.AddChild(image.Visual);
        image.Source = "resources\\gum-logo-normal-64.png";

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
        splitter.Dock(WireframeDock.FillHorizontally);
        splitter.Height = 5;

        var listBox2 = new ListBox();
        splitterStackPanel.AddChild(listBox2);
        for (int i = 0; i < 10; i++)
        {
            listBox2.Items.Add("List Item " + i);
        }

        var window = new Window();
        window.Anchor(Gum.Wireframe.Anchor.Center);
        window.Width = 300;
        window.Height = 200;
        this.AddChild(window.Visual);

        var windowLabel = new Label();
        windowLabel.Dock(Gum.Wireframe.Dock.Top);
        windowLabel.Y = 24;
        windowLabel.Text = "Hello I am a message box";
        window.AddChild(windowLabel);

        var windowButton = new Button();
        windowButton.Anchor(Gum.Wireframe.Anchor.Bottom);
        windowButton.Y = -10;
        windowButton.Text = "Close";
        window.AddChild(windowButton.Visual);
        windowButton.Click += (_, _) =>
        {
            window.Visual.Parent = null;
        };

        AddSwitchHint();
    }

    /// <summary>
    /// Gives this screen's starting control input focus. Called by Program after the screen
    /// has been added to the root and the current frame's input has been processed, so a
    /// connected gamepad has a control to begin navigating from.
    /// </summary>
    public void FocusInitialControl()
    {
        _initialFocusControl.IsFocused = true;
    }

    private void AddSwitchHint()
    {
        var hint = new TextRuntime();
        hint.Text = "Gamepad: D-pad / left stick navigates, A activates";
        hint.XOrigin = HorizontalAlignment.Left;
        hint.YOrigin = VerticalAlignment.Bottom;
        hint.XUnits = GeneralUnitType.PixelsFromSmall;
        hint.YUnits = GeneralUnitType.PixelsFromLarge;
        hint.X = 8;
        hint.Y = -8;
        this.AddChild(hint);
    }
}
