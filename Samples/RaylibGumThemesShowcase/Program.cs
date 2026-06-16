using System;
using Gum;
using Gum.Forms.Controls;
using Gum.Themes.Editor;
using Gum.Wireframe;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace RaylibGumThemesShowcase;

/// <summary>
/// Minimal raylib host that smoke-tests the raylib variant of a Gum theme. It applies
/// <see cref="EditorTheme"/> and lays out a representative set of Forms controls so the styled
/// visuals and KernSmith-generated fonts can be eyeballed on raylib. The theme's <c>Apply()</c> is
/// the same parameterless call used on every backend — the host is otherwise stock raylib + Gum.
/// </summary>
public static class Program
{
    public static void Main()
    {
        const int screenWidth = 1280;
        const int screenHeight = 800;

        GumService.Default.CanvasWidth = screenWidth;
        GumService.Default.CanvasHeight = screenHeight;

        SetConfigFlags(ConfigFlags.Msaa4xHint | ConfigFlags.ResizableWindow);
        InitWindow(screenWidth, screenHeight, "Gum raylib - Editor Theme Showcase");

        GumService.Default.Initialize();
        GumService.Default.UseKeyboardDefaults();

        // One parameterless call applies the theme: it wires KernSmith for in-memory font
        // generation (no .fnt files shipped) and installs the Editor visuals as the defaults.
        EditorTheme.Apply();

        BuildControls();

        while (!WindowShouldClose())
        {
            BeginDrawing();
            ClearBackground(new Color(40, 40, 40, 255));

            GumService.Default.Update(GetTime());
            GumService.Default.Draw();

            EndDrawing();
        }

        CloseWindow();
    }

    private static void BuildControls()
    {
        const int columnWidth = 300;
        const int gap = 20;

        var columns = new StackPanel();
        columns.Orientation = Orientation.Horizontal;
        columns.Spacing = gap;
        columns.X = 20;
        columns.Y = 20;
        columns.AddToRoot();

        // --- Column 1: buttons, toggles, checks, radios ---
        var column1 = NewColumn(columnWidth);
        columns.AddChild(column1);

        var button = new Button();
        button.Text = "Click Me";
        button.Width = columnWidth;
        button.Height = 40;
        int clicks = 0;
        button.Click += (_, _) => button.Text = $"Clicked {++clicks}x";
        column1.AddChild(button);

        var disabledButton = new Button();
        disabledButton.Text = "Disabled";
        disabledButton.Width = columnWidth;
        disabledButton.Height = 40;
        disabledButton.IsEnabled = false;
        column1.AddChild(disabledButton);

        var toggle = new ToggleButton();
        toggle.Text = "Toggle";
        toggle.Width = columnWidth;
        toggle.Height = 32;
        column1.AddChild(toggle);

        var checkBox = new CheckBox();
        checkBox.Text = "Enable feature";
        checkBox.Width = columnWidth;
        checkBox.IsChecked = true;
        column1.AddChild(checkBox);

        for (int i = 1; i <= 2; i++)
        {
            var radio = new RadioButton();
            radio.Text = $"Choice {i}";
            radio.Width = columnWidth;
            radio.IsChecked = i == 1;
            column1.AddChild(radio);
        }

        // --- Column 2: text input, combo, slider, label ---
        var column2 = NewColumn(columnWidth);
        columns.AddChild(column2);

        var textBox = new TextBox();
        textBox.Width = columnWidth;
        textBox.Placeholder = "Type something...";
        column2.AddChild(textBox);

        var passwordBox = new PasswordBox();
        passwordBox.Width = columnWidth;
        passwordBox.Placeholder = "Password";
        column2.AddChild(passwordBox);

        var comboBox = new ComboBox();
        comboBox.Width = columnWidth;
        for (int i = 1; i <= 6; i++)
        {
            comboBox.Items.Add($"Option {i}");
        }
        comboBox.SelectedIndex = 0;
        column2.AddChild(comboBox);

        var slider = new Slider();
        slider.Width = columnWidth;
        slider.Minimum = 0;
        slider.Maximum = 100;
        slider.Value = 35;
        column2.AddChild(slider);

        var label = new Label();
        label.Text = "Labels are read-only text.";
        column2.AddChild(label);

        // --- Column 3: list and scrolling content ---
        var column3 = NewColumn(columnWidth);
        columns.AddChild(column3);

        var listBox = new ListBox();
        listBox.Width = columnWidth;
        listBox.Height = 200;
        for (int i = 1; i <= 15; i++)
        {
            listBox.Items.Add($"List item {i}");
        }
        column3.AddChild(listBox);

        var scrollViewer = new ScrollViewer();
        scrollViewer.Width = columnWidth;
        scrollViewer.Height = 200;
        for (int i = 0; i < 12; i++)
        {
            var scrollButton = new Button();
            scrollButton.Text = $"Scrollable item {i}";
            scrollButton.Height = 36;
            scrollViewer.AddChild(scrollButton);
        }
        column3.AddChild(scrollViewer);
    }

    private static StackPanel NewColumn(int width)
    {
        var column = new StackPanel();
        column.Width = width;
        column.Spacing = 8;
        return column;
    }
}
