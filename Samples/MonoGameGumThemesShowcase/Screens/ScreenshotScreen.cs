using Gum.Forms.Controls;
using Gum.Wireframe;

namespace MonoGameGumThemesShowcase.Screens;

// Mimics the HTML mockup at C:\Users\vchel\Downloads\Temp\GumHtml\Gum Themes Mockup.html.
// Two-column "Game Settings" panel intended as a per-theme overview screenshot.
public class ScreenshotScreen : ShowcaseScreen
{
    public override void Build()
    {
        //const int panelWidth = 500;
        const int panelWidth = 400;
        const int columnGap = 20;
        const int columnWidth = (panelWidth - columnGap) / 2;
        int panelX = 30;
        int panelY = 30;

        // Title
        var title = new Label();
        title.Text = "Game Settings";
        title.X = panelX;
        title.Y = panelY;
        AddToScreenRoot(title);

        // Columns container
        var columns = new StackPanel();
        columns.Orientation = Orientation.Horizontal;
        columns.Spacing = columnGap;
        columns.X = panelX;
        columns.Y = panelY + 32;

        columns.AddChild(BuildLeftColumn(columnWidth));
        columns.AddChild(BuildRightColumn(columnWidth));

        AddToScreenRoot(columns);
    }

    static StackPanel BuildLeftColumn(int width)
    {
        var col = new StackPanel();
        col.Width = width;
        col.Spacing = 16;

        // Player name
        var nameBox = new TextBox();
        nameBox.Width = width;
        nameBox.Text = "Alex";
        col.AddChild(LabeledField("Player name", nameBox));

        // Volume slider with value readout in the label
        var volumeSlider = new Slider();
        volumeSlider.Width = width;
        volumeSlider.Minimum = 0;
        volumeSlider.Maximum = 100;
        volumeSlider.Value = 75;

        var volumeLabel = new Label();
        volumeLabel.Text = $"Volume: {volumeSlider.Value:0}";
        volumeSlider.ValueChanged += (_, _) => volumeLabel.Text = $"Volume: {volumeSlider.Value:0}";

        var volumeField = new StackPanel();
        volumeField.Spacing = 4;
        volumeField.AddChild(volumeLabel);
        volumeField.AddChild(volumeSlider);
        col.AddChild(volumeField);

        // Graphics quality (horizontal radios)
        var radios = new StackPanel();
        radios.Orientation = Orientation.Horizontal;
        radios.Spacing = 16;
        var low = new RadioButton(); low.Text = "Low"; low.Width = 80;
        var med = new RadioButton(); med.Text = "Medium"; med.Width = 90; med.IsChecked = true;
        var high = new RadioButton(); high.Text = "High"; high.Width = 80;
        radios.AddChild(low);
        radios.AddChild(med);
        radios.AddChild(high);
        col.AddChild(LabeledField("Graphics quality", radios));

        // Display (checkboxes)
        var checks = new StackPanel();
        checks.Spacing = 6;
        var fullscreen = new CheckBox();
        fullscreen.Text = "Fullscreen";
        fullscreen.Width = width;
        fullscreen.IsChecked = true;
        var vsync = new CheckBox();
        vsync.Text = "Vertical sync";
        vsync.Width = width;
        checks.AddChild(fullscreen);
        checks.AddChild(vsync);
        col.AddChild(LabeledField("Display", checks));

        return col;
    }

    static StackPanel BuildRightColumn(int width)
    {
        var col = new StackPanel();
        col.Width = width;
        col.Spacing = 16;
        col.X = -240;

        // Difficulty
        var difficulty = new ComboBox();
        //difficulty.ListBox.Height = 70;
        difficulty.Width = width;
        difficulty.Items.Add("Practice");
        difficulty.Items.Add("Easy");
        difficulty.Items.Add("Normal");
        difficulty.Items.Add("Hard");
        difficulty.Items.Add("Nightmare");
        difficulty.Items.Add("Another Option");
        difficulty.Items.Add("Yet Another");
        difficulty.Items.Add("And One More");
        difficulty.SelectedIndex = 1;
        col.AddChild(LabeledField("Difficulty", difficulty));

        // Buttons row
        var buttonsRow = new StackPanel();
        buttonsRow.Orientation = Orientation.Horizontal;
        buttonsRow.Spacing = 10;
        buttonsRow.Y = 160;
        var cancel = new Button();
        cancel.Text = "Cancel";
        cancel.Width = 100;
        cancel.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        cancel.Height = 32;
        var save = new Button();
        save.Text = "Save";
        save.Width = 100;
        save.Height = 32;
        save.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        buttonsRow.AddChild(cancel);
        buttonsRow.AddChild(save);
        col.AddChild(buttonsRow);

        return col;
    }

    static StackPanel LabeledField(string label, FrameworkElement control)
    {
        var stack = new StackPanel();
        stack.Spacing = 4;
        var l = new Label();
        l.Text = label;
        stack.AddChild(l);
        stack.AddChild(control);
        return stack;
    }
}
