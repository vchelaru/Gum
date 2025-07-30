using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using MonoGameGum;
using MonoGameGum.Forms;
using MonoGameGum.Forms.Controls;
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

    Gum.Forms.Controls.MenuItem FileMenuItem;

    
    partial void CustomInitialize()
    {
        var viewModel = new GumFormsSample.ViewModels.DemoScreenViewModel();
        this.BindingContext = viewModel;

        this.BindingButton.FormsControl.SetBinding(
            nameof(Button.IsEnabled),
            nameof(viewModel.IsButtonEnabled));

        this.BindingCheckbox.FormsControl.SetBinding(
            nameof(CheckBox.IsChecked),
            nameof(viewModel.IsButtonEnabled));

        this.ComboBoxInstance.FormsControl.SetBinding(
            nameof(ComboBox.Items),
            nameof(viewModel.ComboBoxItems));

        this.ResolutionBox.FormsControl.SetBinding(
            nameof(ListBox.Items),
            nameof(viewModel.ListBoxItems));

        this.KeyboardRadioButton.FormsControl.SetBinding(
            nameof(RadioButton.IsChecked),
            nameof(viewModel.IsKeyboardAndMouseChecked));

        this.GamepadRadioButton.FormsControl.SetBinding(
            nameof(RadioButton.IsChecked),
            nameof(viewModel.IsGamepadChecked));

        this.TouchScreenRadioButton.FormsControl.SetBinding(
            nameof(RadioButton.IsChecked),
            nameof(viewModel.IsTouchscreenChecked));

        InitializeMenuItem();


        MusicSlider.FormsControl.LargeChange = 10;


        DetectResolutionsButton.Click += (not, used) =>
        {
            ShowPopup();
        };


        var gamepad = FormsUtilities.Gamepads[0];
        //if(gamepad.IsConnected)
        {
            FrameworkElement.GamePadsForUiControl.Add(gamepad);
            DetectResolutionsButton.FormsControl.IsFocused = true;
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
            DetectResolutionsButton.FormsControl.IsFocused = true;
            popupGue.Parent = null;
        };

        popupGue.GetFrameworkElementByName<Button>("CancelButton").Click += (not, used) =>
        {
            popupGue.RemoveFromManagers();
            DetectResolutionsButton.FormsControl.IsFocused = true;
            popupGue.Parent = null;
        };
    }
}
