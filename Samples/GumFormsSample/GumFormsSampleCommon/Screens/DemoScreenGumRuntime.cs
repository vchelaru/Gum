using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using MonoGameGum;
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

        var detectResolutionButton = this.GetFrameworkElementByName<Button>("DetectResolutionsButton");

        detectResolutionButton.Visual.RollOver += (_, _) =>
        System.Diagnostics.Debug.WriteLine("Roll Over");
        detectResolutionButton.Visual.Dragging += (_, _) =>
        System.Diagnostics.Debug.WriteLine("Drag Over");

        var musicSlider = this.GetFrameworkElementByName<Slider>("MusicSlider");
        musicSlider.LargeChange = 10;


        detectResolutionButton.Click += (not, used) =>
        {
            ShowPopup();
        };
    }

    private void ShowPopup()
    {
        var element = ObjectFinder.Self.GetElementSave("Controls/MessageBox");
        var popupGue = element.ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);
        popupGue.Parent = FrameworkElement.ModalRoot;

        popupGue.GetFrameworkElementByName<Button>("OkButton").Click += (not, used) =>
        {
            popupGue.RemoveFromManagers();
            popupGue.Parent = null;
        };

        popupGue.GetFrameworkElementByName<Button>("CancelButton").Click += (not, used) =>
        {
            popupGue.RemoveFromManagers();
            popupGue.Parent = null;
        };
    }
}
