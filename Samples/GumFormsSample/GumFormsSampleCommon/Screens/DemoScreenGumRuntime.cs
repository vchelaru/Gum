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

partial class DemoScreenGumRuntime
{

    Button detectResolutionButton;
    MenuItem FileMenuItem;

    
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

        InitializeMenuItem();

        detectResolutionButton = this.GetFrameworkElementByName<Button>("DetectResolutionsButton");

        var musicSlider = this.GetFrameworkElementByName<Slider>("MusicSlider");
        musicSlider.LargeChange = 10;


        detectResolutionButton.Click += (not, used) =>
        {
            ShowPopup();
        };


        var gamepad = FormsUtilities.Gamepads[0];
        //if(gamepad.IsConnected)
        {
            FrameworkElement.GamePadsForUiControl.Add(gamepad);
            detectResolutionButton.IsFocused = true;
        }


    }

    private void InitializeMenuItem()
    {
        var menu = this.GetFrameworkElementByName<Menu>("MenuInstance");

        FileMenuItem = menu.MenuItems.FirstOrDefault(item => item.Header == "File");

        //var fileMenuItem = new MenuItem();
        //fileMenuItem.Header = "File";
        //menu.Items.Add(fileMenuItem);

        //var menuItem = new MenuItem

        var menuItemElement = ObjectFinder.Self.GetElementSave("Controls/MenuItem");
        var newMenuItem = new MenuItem((InteractiveGue)menuItemElement.ToGraphicalUiElement(this.EffectiveManagers, addToManagers:false));
        newMenuItem.Header = "New";
        FileMenuItem.Items.Add(newMenuItem);

        var saveMenuItem = new MenuItem((InteractiveGue)menuItemElement.ToGraphicalUiElement(this.EffectiveManagers, addToManagers: false));
        saveMenuItem.Header = "Save";
        FileMenuItem.Items.Add(saveMenuItem);


        var exitMenuItem = new MenuItem((InteractiveGue)menuItemElement.ToGraphicalUiElement(this.EffectiveManagers, addToManagers: false));
        exitMenuItem.Header = "Exit";
        FileMenuItem.Items.Add(exitMenuItem);


    }

    private void ShowPopup()
    {
        var element = ObjectFinder.Self.GetElementSave("Controls/MessageBox");
        var popupGue = element.ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);
        popupGue.Parent = FrameworkElement.ModalRoot;

        var okButton = popupGue.GetFrameworkElementByName<Button>("OkButton");
        okButton.IsFocused = true;
        okButton.Click += (not, used) =>
        {
            popupGue.RemoveFromManagers();
            detectResolutionButton.IsFocused = true;
            popupGue.Parent = null;
        };

        popupGue.GetFrameworkElementByName<Button>("CancelButton").Click += (not, used) =>
        {
            popupGue.RemoveFromManagers();
            detectResolutionButton.IsFocused = true;
            popupGue.Parent = null;
        };
    }
}
