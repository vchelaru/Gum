using System.Collections.Generic;

namespace Gum.DataTypes.Behaviors;

public static class StandardFormsBehaviorNames
{
    public const string ButtonBehaviorName = "ButtonBehavior";
    public const string CheckBoxBehaviorName = "CheckBoxBehavior";
    public const string ColorPickerBehaviorName = "ColorPickerBehavior";
    public const string ComboBoxBehaviorName = "ComboBoxBehavior";
    public const string DialogBoxBehaviorName = "DialogBoxBehavior";
    public const string ExpanderBehaviorName = "ExpanderBehavior";
    public const string ItemsControlBehaviorName = "ItemsControlBehavior";
    public const string LabelBehaviorName = "LabelBehavior";
    public const string ListBoxBehaviorName = "ListBoxBehavior";
    public const string ListBoxItemBehaviorName = "ListBoxItemBehavior";
    public const string MenuBehaviorName = "MenuBehavior";
    public const string MenuItemBehaviorName = "MenuItemBehavior";
    /// <summary>
    /// Marks a component as the popup a MenuItem shows its sub-items in. The component has the
    /// ScrollViewer structure (it holds the rows and scrolls); see MenuItem's ScrollViewerVisualTemplate.
    /// </summary>
    public const string MenuItemSubmenuPopupBehaviorName = "MenuItemSubmenuPopupBehavior";
    public const string PanelBehaviorName = "PanelBehavior";
    public const string PasswordBoxBehaviorName = "PasswordBoxBehavior";
    public const string RadioButtonBehaviorName = "RadioButtonBehavior";
    public const string ScrollBarBehaviorName = "ScrollBarBehavior";
    public const string ScrollViewerBehaviorName = "ScrollViewerBehavior";
    public const string SliderBehaviorName = "SliderBehavior";
    public const string SplitterBehaviorName = "SplitterBehavior";
    public const string StackPanelBehaviorName = "StackPanelBehavior";
    public const string TextBoxBehaviorName = "TextBoxBehavior";
    public const string ToggleBehaviorName = "ToggleBehavior";
    public const string TooltipBehaviorName = "TooltipBehavior";
    public const string UserControlBehaviorName = "UserControlBehavior";
    public const string WindowBehaviorName = "WindowBehavior";

    public static readonly HashSet<string> All = new HashSet<string>
    {
        ButtonBehaviorName,
        CheckBoxBehaviorName,
        ColorPickerBehaviorName,
        ComboBoxBehaviorName,
        DialogBoxBehaviorName,
        ExpanderBehaviorName,
        ItemsControlBehaviorName,
        LabelBehaviorName,
        ListBoxBehaviorName,
        ListBoxItemBehaviorName,
        MenuBehaviorName,
        MenuItemBehaviorName,
        MenuItemSubmenuPopupBehaviorName,
        PanelBehaviorName,
        PasswordBoxBehaviorName,
        RadioButtonBehaviorName,
        ScrollBarBehaviorName,
        ScrollViewerBehaviorName,
        SliderBehaviorName,
        SplitterBehaviorName,
        StackPanelBehaviorName,
        TextBoxBehaviorName,
        ToggleBehaviorName,
        TooltipBehaviorName,
        UserControlBehaviorName,
        WindowBehaviorName,
    };
}
