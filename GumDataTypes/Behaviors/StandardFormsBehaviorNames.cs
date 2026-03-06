using System.Collections.Generic;

namespace Gum.DataTypes.Behaviors;

public static class StandardFormsBehaviorNames
{
    public const string ButtonBehaviorName = "ButtonBehavior";
    public const string CheckBoxBehaviorName = "CheckBoxBehavior";
    public const string ComboBoxBehaviorName = "ComboBoxBehavior";
    public const string ItemsControlBehaviorName = "ItemsControlBehavior";
    public const string LabelBehaviorName = "LabelBehavior";
    public const string ListBoxBehaviorName = "ListBoxBehavior";
    public const string ListBoxItemBehaviorName = "ListBoxItemBehavior";
    public const string MenuBehaviorName = "MenuBehavior";
    public const string MenuItemBehaviorName = "MenuItemBehavior";
    public const string PanelBehaviorName = "PanelBehavior";
    public const string PasswordBoxBehaviorName = "PasswordBoxBehavior";
    public const string RadioButtonBehaviorName = "RadioButtonBehavior";
    public const string ScrollBarBehaviorName = "ScrollBarBehavior";
    public const string ScrollViewerBehaviorName = "ScrollViewerBehavior";
    public const string SliderBehaviorName = "SliderBehavior";
    public const string SplitterBehaviorName = "SplitterBehavior";
    public const string StackPanelBehaviorName = "StackPanelBehavior";
    public const string TextBoxBehaviorName = "TextBoxBehavior";
    public const string WindowBehaviorName = "WindowBehavior";

    public static readonly HashSet<string> All = new HashSet<string>
    {
        ButtonBehaviorName,
        CheckBoxBehaviorName,
        ComboBoxBehaviorName,
        ItemsControlBehaviorName,
        LabelBehaviorName,
        ListBoxBehaviorName,
        ListBoxItemBehaviorName,
        MenuBehaviorName,
        MenuItemBehaviorName,
        PanelBehaviorName,
        PasswordBoxBehaviorName,
        RadioButtonBehaviorName,
        ScrollBarBehaviorName,
        ScrollViewerBehaviorName,
        SliderBehaviorName,
        SplitterBehaviorName,
        StackPanelBehaviorName,
        TextBoxBehaviorName,
        WindowBehaviorName,
    };
}
