using Gum.Forms.Controls;
using MonoGameGum.GueDeriving;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGumInCode.Screens;
internal class FormsScreen : FrameworkElement
{
    public FormsScreen() : base(new ContainerRuntime())
    {
        const int gap = 8;

        Dock(Gum.Wireframe.Dock.Fill);
        

        var stackPanel = new StackPanel();
        stackPanel.Visual.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        stackPanel.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        stackPanel.Visual.Width = 0;
        stackPanel.Visual.Height = 0;
        stackPanel.Visual.WrapsChildren = true;
        float stackMargin = 8;
        stackPanel.Visual.X = stackMargin;
        stackPanel.Visual.Y = stackMargin;
        stackPanel.Visual.Width = -stackMargin * 2;
        stackPanel.Visual.Height = -stackMargin * 2;

        this.AddChild(stackPanel);


        var button = new Button();
        button.Text = "Normal Button";
        stackPanel.AddChild(button);

        var disabledButton = new Button();
        disabledButton.Text = "Disabled Button";
        disabledButton.IsEnabled = false;
        stackPanel.AddChild(disabledButton);

        var checkBox = new CheckBox();
        checkBox.Y = gap;
        checkBox.Text = "Normal Checkbox";
        checkBox.Width = 200;
        stackPanel.AddChild(checkBox);

        var disabledCheckBox = new CheckBox();
        disabledCheckBox.Text = "Disabled Checkbox";
        disabledCheckBox.IsEnabled = false;
        disabledCheckBox.Width = 200;
        stackPanel.AddChild(disabledCheckBox);

        var disabledCheckedCheckBox = new CheckBox();
        disabledCheckedCheckBox.Text = "Disabled Checkbox";
        disabledCheckedCheckBox.IsEnabled = false;
        disabledCheckedCheckBox.IsChecked = true;
        disabledCheckedCheckBox.Width = 200;
        stackPanel.AddChild(disabledCheckedCheckBox);


        var comboBox = new ComboBox();
        comboBox.Y = gap;
        for (int i = 0; i < 10; i++)
        {
            comboBox.Items.Add("Item " + i);
        }
        stackPanel.AddChild(comboBox);

        var disabledComboBox = new ComboBox();
        disabledComboBox.Y = gap;
        disabledComboBox.Text = "Disabled ComboBox";
        disabledComboBox.IsEnabled = false;
        stackPanel.AddChild(disabledComboBox);


        var label = new Label();
        label.Text = "I am a label!";
        label.Y = gap;
        stackPanel.AddChild(label);


        var listBox = new ListBox();
        listBox.Y = gap;
        for (int i = 0; i < 10; i++)
        {
            listBox.Items.Add("Item " + i);
        }
        stackPanel.AddChild(listBox);


        var passwordBox = new PasswordBox();
        passwordBox.Y = gap;
        passwordBox.Placeholder = "Enter your password";
        passwordBox.Width = 200;
        stackPanel.AddChild(passwordBox);

        var disabledPasswordBox = new PasswordBox();
        disabledPasswordBox.Placeholder = "Disabled PasswordBox";
        disabledPasswordBox.IsEnabled = false;
        disabledPasswordBox.Width = 200;
        disabledPasswordBox.Y = gap;
        disabledPasswordBox.Password = "I am a password";
        stackPanel.AddChild(disabledPasswordBox);

        for(int i = 0; i < 4; i++)
        {
            var radioButton = new RadioButton();
            radioButton.Width = 170;
            radioButton.Text = "Radio Button " + i;
            if(i == 0)
            {
                radioButton.Y = gap;
            }
            else if(i == 1)
            {
                radioButton.IsEnabled = false;
            }
            stackPanel.AddChild(radioButton);
        }


        var slider = new Slider();
        slider.Y = gap;
        slider.Width = 200;
        stackPanel.AddChild(slider);

        var disabledSlider = new Slider();
        disabledSlider.Y = gap;
        disabledSlider.IsEnabled = false;
        disabledSlider.Width = 200;
        stackPanel.AddChild(disabledSlider);

        var textBox = new TextBox();
        textBox.Y = gap;
        textBox.Placeholder = "Enter text here";
        textBox.Width = 200;
        stackPanel.AddChild(textBox);

        var disabledTextBox = new TextBox();
        disabledTextBox.Y = gap;
        disabledTextBox.Placeholder = "Disabled TextBox";
        disabledTextBox.IsEnabled = false;
        disabledTextBox.Width = 200;
        stackPanel.AddChild(disabledTextBox);

    }

}
