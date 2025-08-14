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
using Gum.Commands;
using Gum.Services;

namespace Gum.Controls;

public enum AboveOrBelow
{
    Above,
    Below
}

public enum ControlLocation
{
    Above,
    Below,
    LeftOfTextBox
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
        
        ValidationLabel.Visibility = Visibility.Hidden;
        this.Loaded += HandleLoaded;
    }

    bool shouldSelectAllOnLoaded;
    private void HandleLoaded(object sender, RoutedEventArgs e)
    {
        if(shouldSelectAllOnLoaded)
        {
            TextBox.SelectAll();
        }
    }

    public void HighlightText()
    {
        TextBox.SelectAll();
        shouldSelectAllOnLoaded = true;
    }



    public void AddControl(FrameworkElement control, ControlLocation controlLocation = ControlLocation.Below)
    {
        if (controlLocation == ControlLocation.Above)
        {
            AboveTextBoxStackPanel.Children.Add(control);
        }
        else if (controlLocation == ControlLocation.Below)
        {
            BelowTextBoxStackPanel.Children.Add(control);
        }
        else if (controlLocation == ControlLocation.LeftOfTextBox)
        {
            LeftOfTextBoxStackPanel.Children.Add(control);
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
                break;
        }
    }

    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        TextEntered?.Invoke(this, null);
    }
}
