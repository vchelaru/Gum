using Gum.Mvvm;
using MonoGameGum.Forms.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GumFormsSample.ViewModels;

enum ControlScheme
{
    KeyboardAndMouse,
    Gamepad,
    Touchscreen
}

internal class DemoScreenViewModel : ViewModel
{
    public bool IsButtonEnabled
    {
        get => Get<bool>();
        set => Set(value);
    }

    public ObservableCollection<string> ComboBoxItems
    {
        get => Get<ObservableCollection<string>>();
        set => Set(value);
    }

    public ObservableCollection<string> ListBoxItems
    {
        get => Get<ObservableCollection<string>>();
        set => Set(value);
    }

    public ControlScheme ControlScheme
    {
        get => Get<ControlScheme>();
        set => Set(value);
    }

    [DependsOn(nameof(ControlScheme))]
    public bool IsKeyboardAndMouseChecked
    {
        get => ControlScheme == ControlScheme.KeyboardAndMouse;
        set
        {
            if (value) ControlScheme = ControlScheme.KeyboardAndMouse;
        }
    }

    [DependsOn(nameof(ControlScheme))]
    public bool IsGamepadChecked
    {
        get => ControlScheme == ControlScheme.Gamepad;
        set
        {
            if (value) ControlScheme = ControlScheme.Gamepad;
        }
    }

    [DependsOn(nameof(ControlScheme))]
    public bool IsTouchscreenChecked
    {
        get => ControlScheme == ControlScheme.Touchscreen;
        set
        {
            if (value) ControlScheme = ControlScheme.Touchscreen;
        }
    }

    public DemoScreenViewModel()
    {
        ComboBoxItems = new ObservableCollection<string>();

        ComboBoxItems.Add("Easy");
        ComboBoxItems.Add("Medium");
        ComboBoxItems.Add("Hard");
        ComboBoxItems.Add("Impossible");

        ListBoxItems = new ObservableCollection<string>();

        ListBoxItems.Add("400x300");
        ListBoxItems.Add("600x800");
        ListBoxItems.Add("1024x768");
        ListBoxItems.Add("1280x720");
        ListBoxItems.Add("1920x1080");
        ListBoxItems.Add("2560x1440");
        ListBoxItems.Add("3840x2160");
        ListBoxItems.Add("7680x4320");

        ControlScheme = ControlScheme.KeyboardAndMouse;
    }
}
