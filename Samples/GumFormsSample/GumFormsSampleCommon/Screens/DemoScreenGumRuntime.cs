using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using MonoGameGum.Forms;
using MonoGameGum.Forms.Controls;
using MonoGameGum.Forms.DefaultFromFileVisuals;
using RenderingLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace GumFormsSample.Screens;

public class DemoScreenGumRuntime : BindableGue
{
    [ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("DemoScreenGum", typeof(DemoScreenGumRuntime));
    }
    
    public void Initialize()
    {
        var viewModel = new GumFormsSample.ViewModels.DemoScreenViewModel();
        this.BindingContext = viewModel;

        this.GetFrameworkElementByName<Button>("BindingButton").SetBinding(
            nameof(Button.IsEnabled),
            nameof(viewModel.IsButtonEnabled));

        this.GetFrameworkElementByName<CheckBox>("BindingCheckbox").SetBinding(
            nameof(CheckBox.IsChecked),
            nameof(viewModel.IsButtonEnabled));

        this.GetFrameworkElementByName<ComboBox>("ComboBoxInstance").SetBinding(
            nameof(ComboBox.Items),
            nameof(viewModel.ComboBoxItems));

        this.GetFrameworkElementByName<ListBox>("ResolutionBox").SetBinding(
            nameof(ListBox.Items),
            nameof(viewModel.ListBoxItems));

        this.GetFrameworkElementByName<RadioButton>("KeyboardRadioButton").SetBinding(
            nameof(RadioButton.IsChecked),
            nameof(viewModel.IsKeyboardAndMouseChecked));

        this.GetFrameworkElementByName<RadioButton>("GamepadRadioButton").SetBinding(
            nameof(RadioButton.IsChecked),
            nameof(viewModel.IsGamepadChecked));

        this.GetFrameworkElementByName<RadioButton>("TouchScreenRadioButton").SetBinding(
            nameof(RadioButton.IsChecked),
            nameof(viewModel.IsTouchscreenChecked));
    }
}
