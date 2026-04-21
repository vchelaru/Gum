using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using SokolGum;

namespace SokolGumSample;

/// <summary>
/// Empty screen — fill in with custom code.
/// </summary>
public class Screen2 : ContainerRuntime
{
    public Screen2()
    {
        Width = 0; WidthUnits = DimensionUnitType.RelativeToParent;
        Height = 0; HeightUnits = DimensionUnitType.RelativeToParent;
        Name = "Screen2";


        var panel = new StackPanel();
        panel.Spacing = 16;
        // Parent the panel under this screen (not Root) so the screen's
        // AddToRoot / RemoveFromRoot lifecycle moves the whole subtree.
        Children.Add(panel.Visual);

        // Status label — updated by controls below
        var label = new Label();
        label.Text = "Interact with any control:";
        panel.AddChild(label);

        // Button
        var button = new Button();
        button.Text = "Click Me";
        button.Width = 200;
        button.Click += (_, _) =>
            label.Text = $"Button clicked @ {DateTime.Now:HH:mm:ss}";
        panel.AddChild(button);

        for(int i = 0; i < 2; i++)
        {
            // TextBox
            var textBox = new TextBox();
            textBox.Placeholder = "Enter text here...";
            textBox.Width = 200;
            textBox.TextChanged += (_, _) =>
                label.Text = $"TextBox: {textBox.Text}";
            panel.AddChild(textBox);
        }

        // CheckBox
        var checkBox = new CheckBox();
        checkBox.Text = "Check me";
        checkBox.Checked += (_, _) => label.Text = "CheckBox checked";
        checkBox.Unchecked += (_, _) => label.Text = "CheckBox unchecked";
        panel.AddChild(checkBox);

        // Slider
        var slider = new Slider();
        slider.Width = 200;
        slider.Minimum = 0;
        slider.Maximum = 100;
        slider.ValueChanged += (_, _) =>
            label.Text = $"Slider: {slider.Value:0.0}";
        panel.AddChild(slider);

        // ComboBox
        var comboBox = new ComboBox();
        for (int i = 0; i < 10; i++)
            comboBox.Items.Add($"Option {i}");
        comboBox.SelectionChanged += (_, _) =>
            label.Text = "ComboBox: " + comboBox.SelectedObject;
        panel.AddChild(comboBox);

        // ListBox
        var listBox = new ListBox();
        listBox.Visual.Width = 200;
        listBox.Visual.Height = 120;
        for (int i = 0; i < 10; i++)
            listBox.Items.Add($"Item {i}");
        listBox.SelectionChanged += (_, _) =>
            label.Text = $"ListBox: {listBox.SelectedObject} (index {listBox.SelectedIndex})";
        panel.AddChild(listBox);

        // Radio buttons
        var radioGroup = new StackPanel();
        panel.AddChild(radioGroup);

        var radioA = new RadioButton();
        radioA.Text = "Option A";
        radioA.Checked += (_, _) => label.Text = "Radio: Option A";
        radioGroup.AddChild(radioA);

        var radioB = new RadioButton();
        radioB.Text = "Option B";
        radioB.Checked += (_, _) => label.Text = "Radio: Option B";
        radioGroup.AddChild(radioB);

        var radioC = new RadioButton();
        radioC.Text = "Option C";
        radioC.Checked += (_, _) => label.Text = "Radio: Option C";
        radioGroup.AddChild(radioC);
    }
}
