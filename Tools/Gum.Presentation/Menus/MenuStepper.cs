using System;
using Gum.Mvvm;

namespace Gum.Menus;

/// <summary>
/// A menu row that steps a value up or down and shows it, like the zoom row in Chrome's menu:
/// "Zoom  -  100%  +". A head that can host controls in a menu draws the row and keeps the menu
/// open while it is clicked; a head that cannot (the macOS menu bar) draws three ordinary items
/// from the headers instead.
/// </summary>
public sealed class MenuStepper : ViewModel
{
    /// <summary>Creates a stepper whose three actions are also offered as plain items by the given headers.</summary>
    public MenuStepper(
        string label, string increaseHeader, string decreaseHeader, string resetHeader,
        Action increase, Action decrease, Action reset)
    {
        Label = label;
        IncreaseHeader = increaseHeader;
        DecreaseHeader = decreaseHeader;
        ResetHeader = resetHeader;
        Increase = increase;
        Decrease = decrease;
        Reset = reset;
        ValueText = "";
    }

    /// <summary>The row's caption, such as "Zoom".</summary>
    public string Label { get; }

    /// <summary>The current value as shown between the buttons, such as "100%".</summary>
    public string ValueText { get => Get<string>(); set => Set(value); }

    /// <summary>The plain item that raises the value.</summary>
    public string IncreaseHeader { get; }

    /// <summary>The plain item that lowers the value.</summary>
    public string DecreaseHeader { get; }

    /// <summary>The plain item that puts the value back to its default.</summary>
    public string ResetHeader { get; }

    /// <summary>Raises the value one step.</summary>
    public Action Increase { get; }

    /// <summary>Lowers the value one step.</summary>
    public Action Decrease { get; }

    /// <summary>Puts the value back to its default.</summary>
    public Action Reset { get; }
}
