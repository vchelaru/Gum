using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Wireframe;

namespace MonoGameGumThemesShowcase.Screens;

public class AllControlsScreen : ShowcaseScreen
{
    Window _demoWindow;

    // Cell layout
    const int CellW = 330;
    const int CellH = 195;
    const int Gap = 10;
    const int StartX = 15;
    const int StartY = 45;
    const int HeaderHeight = 24;

    public override void Build()
    {
        BuildMenu();

        // Row 0
        PlaceButton(col: 0, row: 0);
        PlaceCheckBoxes(col: 1, row: 0);
        PlaceRadioButtons(col: 2, row: 0);
        PlaceLabel(col: 3, row: 0);

        // Row 1
        PlaceTextBox(col: 0, row: 1);
        PlacePasswordBox(col: 1, row: 1);
        PlaceComboBox(col: 2, row: 1);
        PlaceSlider(col: 3, row: 1);

        // Row 2
        PlaceScrollBar(col: 0, row: 2);
        PlaceWindowLauncher(col: 1, row: 2);
        PlaceListBox(col: 2, row: 2);
        PlaceScrollViewer(col: 3, row: 2);

        // Row 3
        PlaceToggleButton(col: 0, row: 3);
        PlaceSplitter(col: 1, row: 3, colSpan: 3);
    }

    // ---------- cell placement helpers ----------

    static int CellX(int col) => StartX + col * (CellW + Gap);
    static int CellY(int row) => StartY + row * (CellH + Gap);
    static int CellWidth(int colSpan) => CellW * colSpan + Gap * (colSpan - 1);

    void AddHeader(string text, int col, int row)
    {
        var header = new Label();
        header.Text = text;
        header.X = CellX(col);
        header.Y = CellY(row);
        AddToScreenRoot(header);
    }

    // Position a control inside a cell (top-aligned under the header).
    void Position(FrameworkElement control, int col, int row, int colSpan = 1)
    {
        control.X = CellX(col);
        control.Y = CellY(row) + HeaderHeight;
        AddToScreenRoot(control);
        var maxWidth = CellWidth(colSpan);
        if (control.Width > maxWidth) control.Width = maxWidth;
    }

    // ---------- per-control sections ----------

    void PlaceButton(int col, int row)
    {
        AddHeader("Button (with Tooltip)", col, row);

        var stack = new StackPanel();
        stack.Width = CellW;
        stack.Spacing = 6;

        var button = new Button();
        button.Text = "Click Me";
        button.Width = 200;
        button.Height = 40;
        button.ToolTip = "Hover to see this tooltip";
        int clickCount = 0;
        button.Click += (_, _) => button.Text = $"Clicked {++clickCount} time(s)";
        button.IsFocused = true;

        var disabledButton = new Button();
        disabledButton.Text = "Disabled";
        disabledButton.Width = 200;
        disabledButton.Height = 40;
        disabledButton.IsEnabled = false;

        stack.AddChild(button);
        stack.AddChild(disabledButton);
        Position(stack, col, row);
    }

    void PlaceToggleButton(int col, int row)
    {
        AddHeader("ToggleButton (click to toggle on/off)", col, row);

        var stack = new StackPanel();
        stack.Width = CellW;
        stack.Spacing = 6;

        var toggle = new ToggleButton();
        toggle.Text = "Bold";
        toggle.Width = 120;
        toggle.Height = 32;

        var togglePreChecked = new ToggleButton();
        togglePreChecked.Text = "Italic";
        togglePreChecked.Width = 120;
        togglePreChecked.Height = 32;
        togglePreChecked.IsChecked = true;

        var toggleDisabledOff = new ToggleButton();
        toggleDisabledOff.Text = "Disabled Off";
        toggleDisabledOff.Width = 120;
        toggleDisabledOff.Height = 32;
        toggleDisabledOff.IsEnabled = false;

        var toggleDisabledOn = new ToggleButton();
        toggleDisabledOn.Text = "Disabled On";
        toggleDisabledOn.Width = 120;
        toggleDisabledOn.Height = 32;
        toggleDisabledOn.IsChecked = true;
        toggleDisabledOn.IsEnabled = false;

        stack.AddChild(toggle);
        stack.AddChild(togglePreChecked);
        stack.AddChild(toggleDisabledOff);
        stack.AddChild(toggleDisabledOn);
        Position(stack, col, row);
    }

    void PlaceCheckBoxes(int col, int row)
    {
        AddHeader("CheckBox", col, row);

        var stack = new StackPanel();
        stack.Width = CellW;
        stack.Spacing = 6;

        var cb1 = new CheckBox();
        cb1.Text = "Enable feature";
        cb1.Width = CellW;
        cb1.IsChecked = true;
        stack.AddChild(cb1);

        var cb2 = new CheckBox();
        cb2.Text = "Three-state checkbox";
        cb2.Width = CellW;
        cb2.IsThreeState = true;
        stack.AddChild(cb2);

        var cb3 = new CheckBox();
        cb3.Text = "Disabled, unchecked";
        cb3.Width = CellW;
        cb3.IsEnabled = false;
        stack.AddChild(cb3);

        var cb4 = new CheckBox();
        cb4.Text = "Disabled, checked";
        cb4.Width = CellW;
        cb4.IsChecked = true;
        cb4.IsEnabled = false;
        stack.AddChild(cb4);

        Position(stack, col, row);
    }

    void PlaceRadioButtons(int col, int row)
    {
        AddHeader("RadioButton (Difficulty)", col, row);

        var stack = new StackPanel();
        stack.Width = CellW;
        stack.Spacing = 4;

        var easy = new RadioButton(); easy.Text = "Easy"; easy.Width = CellW; easy.IsChecked = true;
        var medium = new RadioButton(); medium.Text = "Medium"; medium.Width = CellW;
        var hard = new RadioButton(); hard.Text = "Hard"; hard.Width = CellW;
        stack.AddChild(easy);
        stack.AddChild(medium);
        stack.AddChild(hard);

        var disabledUnselected = new RadioButton();
        disabledUnselected.Text = "Disabled, unselected";
        disabledUnselected.Width = CellW;
        disabledUnselected.IsEnabled = false;
        stack.AddChild(disabledUnselected);

        var disabledSelected = new RadioButton();
        disabledSelected.Text = "Disabled, selected";
        disabledSelected.Width = CellW;
        disabledSelected.IsChecked = true;
        disabledSelected.IsEnabled = false;
        stack.AddChild(disabledSelected);

        Position(stack, col, row);
    }

    void PlaceLabel(int col, int row)
    {
        AddHeader("Label", col, row);

        var stack = new StackPanel();
        stack.Width = CellW;
        stack.Spacing = 6;

        var label = new Label();
        label.Text = "Labels are read-only display text.";
        stack.AddChild(label);

        var disabledLabel = new Label();
        disabledLabel.Text = "Disabled label (IsEnabled = false).";
        disabledLabel.IsEnabled = false;
        stack.AddChild(disabledLabel);

        Position(stack, col, row);
    }

    void PlaceTextBox(int col, int row)
    {
        AddHeader("TextBox", col, row);

        var stack = new StackPanel();
        stack.Width = CellW;
        stack.Spacing = 6;

        var textBox = new TextBox();
        textBox.Width = CellW;
        textBox.Placeholder = "Type something here...";
        stack.AddChild(textBox);

        var disabledTextBox = new TextBox();
        disabledTextBox.Width = CellW;
        disabledTextBox.Text = "Disabled with text";
        disabledTextBox.IsEnabled = false;
        stack.AddChild(disabledTextBox);

        Position(stack, col, row);
    }

    void PlacePasswordBox(int col, int row)
    {
        AddHeader("PasswordBox", col, row);

        var stack = new StackPanel();
        stack.Width = CellW;
        stack.Spacing = 6;

        var pw = new PasswordBox();
        pw.Width = CellW;
        pw.Placeholder = "Enter password";
        stack.AddChild(pw);

        var disabledPw = new PasswordBox();
        disabledPw.Width = CellW;
        disabledPw.Password = "secret123";
        disabledPw.IsEnabled = false;
        stack.AddChild(disabledPw);

        Position(stack, col, row);
    }

    void PlaceComboBox(int col, int row)
    {
        AddHeader("ComboBox", col, row);

        var stack = new StackPanel();
        stack.Width = CellW;
        stack.Spacing = 6;

        var combo = new ComboBox();
        combo.Width = CellW;
        for (int i = 1; i <= 6; i++)
        {
            var toAdd = $"Option {i}";
            combo.Items.Add(toAdd);
        }
        combo.SelectedIndex = 0;
        stack.AddChild(combo);

        var disabledCombo = new ComboBox();
        disabledCombo.Width = CellW;
        for (int i = 1; i <= 3; i++) disabledCombo.Items.Add($"Option {i}");
        disabledCombo.SelectedIndex = 0;
        disabledCombo.IsEnabled = false;
        stack.AddChild(disabledCombo);

        Position(stack, col, row);
    }

    void PlaceSlider(int col, int row)
    {
        AddHeader("Slider", col, row);

        var stack = new StackPanel();
        stack.Width = CellW;
        stack.Spacing = 6;

        var slider = new Slider();
        slider.Width = CellW;
        slider.Minimum = 0;
        slider.Maximum = 100;
        slider.TicksFrequency = 1;
        slider.IsSnapToTickEnabled = true;
        slider.Value = 35;

        var valueLabel = new Label();
        valueLabel.Text = $"Value: {slider.Value:0}";
        slider.ValueChanged += (_, _) => valueLabel.Text = $"Value: {slider.Value:0}";

        var disabledSlider = new Slider();
        disabledSlider.Width = CellW;
        disabledSlider.Minimum = 0;
        disabledSlider.Maximum = 100;
        disabledSlider.Value = 70;
        disabledSlider.IsEnabled = false;

        stack.AddChild(slider);
        stack.AddChild(valueLabel);
        stack.AddChild(disabledSlider);
        Position(stack, col, row);
    }

    void PlaceScrollBar(int col, int row)
    {
        AddHeader("ScrollBar (framed, free-floating)", col, row);

        var rowStack = new StackPanel();
        rowStack.Orientation = Orientation.Horizontal;
        rowStack.Spacing = 12;

        var verticalScrollBar = new ScrollBar();
        verticalScrollBar.Width = 16;
        verticalScrollBar.Height = 130;
        verticalScrollBar.Minimum = 0;
        verticalScrollBar.Maximum = 100;
        verticalScrollBar.SmallChange = 5;
        verticalScrollBar.LargeChange = 20;
        verticalScrollBar.Value = 0;

        var horizontalScrollBar = new ScrollBar();
        horizontalScrollBar.Orientation = Orientation.Horizontal;
        horizontalScrollBar.Width = 200;
        horizontalScrollBar.Height = 16;
        horizontalScrollBar.Minimum = 0;
        horizontalScrollBar.Maximum = 100;
        horizontalScrollBar.SmallChange = 5;
        horizontalScrollBar.LargeChange = 20;
        horizontalScrollBar.Value = 0;

        var valueLabel = new Label();
        valueLabel.Text = "V:0 / H:0";
        verticalScrollBar.ValueChanged += (_, _) =>
            valueLabel.Text = $"V:{verticalScrollBar.Value:0} / H:{horizontalScrollBar.Value:0}";
        horizontalScrollBar.ValueChanged += (_, _) =>
            valueLabel.Text = $"V:{verticalScrollBar.Value:0} / H:{horizontalScrollBar.Value:0}";

        var rightColumn = new StackPanel();
        rightColumn.Spacing = 8;
        rightColumn.AddChild(horizontalScrollBar);
        rightColumn.AddChild(valueLabel);

        rowStack.AddChild(verticalScrollBar);
        rowStack.AddChild(rightColumn);
        Position(rowStack, col, row);
    }

    void PlaceListBox(int col, int row)
    {
        AddHeader("ListBox", col, row);

        var listBox = new ListBox();
        listBox.Width = CellW;
        listBox.Height = CellH - HeaderHeight - 5;
        for (int i = 1; i <= 15; i++)
        {
            var toAdd = $"List item {i}";
            listBox.Items.Add(toAdd);
        }
        Position(listBox, col, row);
    }

    void PlaceScrollViewer(int col, int row)
    {
        AddHeader("ScrollViewer", col, row);

        var scrollViewer = new ScrollViewer();
        scrollViewer.Width = CellW;
        scrollViewer.Height = CellH - HeaderHeight - 5;
        scrollViewer.InnerPanel.StackSpacing = 2;
        for (int i = 0; i < 20; i++)
        {
            var btn = new Button();
            btn.Text = $"Scrollable item {i}";
            btn.Height = 40;
            scrollViewer.AddChild(btn);
        }
        Position(scrollViewer, col, row);
    }

    void PlaceSplitter(int col, int row, int colSpan)
    {
        AddHeader("Splitter (drag the divider)", col, row);

        int totalWidth = CellWidth(colSpan);
        var splitContainer = new StackPanel();
        splitContainer.Orientation = Orientation.Horizontal;
        splitContainer.Width = totalWidth;
        splitContainer.Height = CellH - HeaderHeight - 5;
        splitContainer.Spacing = 1;

        var leftList = new ListBox();
        leftList.Width = (totalWidth - 5) / 2;
        leftList.Dock(Dock.FillVertically);
        for (int i = 0; i < 12; i++) leftList.Items.Add($"Left item {i}");

        var splitter = new Splitter();
        splitter.Width = 5;
        splitter.Dock(Dock.FillVertically);

        var rightList = new ListBox();
        rightList.Dock(Dock.FillVertically);
        for (int i = 0; i < 12; i++) rightList.Items.Add($"Right item {i}");

        splitContainer.AddChild(leftList);
        splitContainer.AddChild(splitter);
        splitContainer.AddChild(rightList);
        Position(splitContainer, col, row, colSpan);
    }

    void PlaceWindowLauncher(int col, int row)
    {
        AddHeader("Window (movable popup)", col, row);

        var openButton = new Button();
        openButton.Text = "Open Window";
        openButton.Width = 200;
        openButton.Height = 40;
        openButton.Click += (_, _) => ShowDemoWindow();
        Position(openButton, col, row);
    }

    // ---------- menu and popup window ----------

    void BuildMenu()
    {
        var menu = new Menu();
        AddToScreenRoot(menu);

        var fileMenu = new MenuItem(); fileMenu.Header = "File";
        var newItem = new MenuItem(); newItem.Header = "New"; fileMenu.Items.Add(newItem);
        var openItem = new MenuItem(); openItem.Header = "Open"; fileMenu.Items.Add(openItem);
        var saveItem = new MenuItem(); saveItem.Header = "Save"; fileMenu.Items.Add(saveItem);
        var exitItem = new MenuItem(); exitItem.Header = "Exit"; fileMenu.Items.Add(exitItem);
        menu.Items.Add(fileMenu);

        var editMenu = new MenuItem(); editMenu.Header = "Edit";
        var copyItem = new MenuItem(); copyItem.Header = "Copy"; editMenu.Items.Add(copyItem);
        var pasteItem = new MenuItem(); pasteItem.Header = "Paste"; editMenu.Items.Add(pasteItem);
        menu.Items.Add(editMenu);

        var helpMenu = new MenuItem(); helpMenu.Header = "Help";
        var aboutItem = new MenuItem(); aboutItem.Header = "About"; helpMenu.Items.Add(aboutItem);
        menu.Items.Add(helpMenu);
    }

    void ShowDemoWindow()
    {
        if (_demoWindow != null) return;

        var window = new Window();
        window.Anchor(Anchor.Center);
        window.Width = 340;
        window.Height = 200;
        AddToScreenRoot(window);
        _demoWindow = window;

        var label = new Label();
        label.Dock(Dock.Top);
        label.Y = 24;
        label.Text = "Hello — I'm a Window. Drag me!";
        window.AddChild(label);

        var closeButton = new Button();
        closeButton.Anchor(Anchor.Bottom);
        closeButton.Y = -10;
        closeButton.Text = "Close";
        closeButton.Click += (_, _) =>
        {
            window.RemoveFromRoot();
            _demoWindow = null;
        };
        window.AddChild(closeButton);
    }
}
