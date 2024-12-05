using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Gum.Controls;

public enum AboveOrBelow
{
    Above,
    Below
}

/// <summary>
/// Interaction logic for CustomizableTextInputWindow.xaml
/// </summary>
public partial class CustomizableTextInputWindow : Window
{
    #region Fields/Properties

    public string Message
    {
        get => this.Label.Text;
        set => this.Label.Text = value;
    }

    public string Result
    {
        get => TextBox.Text;
        set => TextBox.Text = value;
    }

    public event EventHandler CustomOkClicked;

    public event EventHandler TextEntered;

    #endregion

    public CustomizableTextInputWindow()
    {
        InitializeComponent();

        TextBox.Focus();

        this.WindowStartupLocation = WindowStartupLocation.Manual;

        GumCommands.Self.GuiCommands.MoveToCursor(this);

        ValidationLabel.Visibility = Visibility.Hidden;
    }

    public void HighlightText()
    {
        TextBox.SelectAll();
    }

    public void AddControl(Control control, AboveOrBelow aboveOrBelow = AboveOrBelow.Below)
    {
        if (aboveOrBelow == AboveOrBelow.Above)
        {
            AboveTextBoxStackPanel.Children.Add(control);
        }
        else if (aboveOrBelow == AboveOrBelow.Below)
        {
            BelowTextBoxStackPanel.Children.Add(control);
        }
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        HandleOk();
    }

    private void HandleOk()
    {
        if (CustomOkClicked == null)
        {
            this.DialogResult = true;
        }
        else
        {
            CustomOkClicked(this, null);


        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        this.DialogResult = false;
    }

    private void TextBox_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                this.DialogResult = false;
                e.Handled = true;
                break;
            case Key.Enter:
                HandleOk();
                e.Handled = true;
                break;
            default:
                TextEntered?.Invoke(this, null);
                break;
        }
    }

    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
    }
}
