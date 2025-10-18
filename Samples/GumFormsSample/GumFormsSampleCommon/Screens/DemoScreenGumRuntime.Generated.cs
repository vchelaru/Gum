//Code for DemoScreenGum
using GumRuntime;
using System.Linq;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using GumFormsSample.Components;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
namespace GumFormsSample.Screens;
partial class DemoScreenGumRuntime : Gum.Wireframe.BindableGue
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("DemoScreenGum", typeof(DemoScreenGumRuntime));
    }
    public MenuRuntime MenuInstance { get; protected set; }
    public ContainerRuntime DemoSettingsMenu { get; protected set; }
    public NineSliceRuntime Background { get; protected set; }
    public ContainerRuntime MenuTitle { get; protected set; }
    public ContainerRuntime MenuTitle1 { get; protected set; }
    public ContainerRuntime MenuItems { get; protected set; }
    public TextRuntime TitleText { get; protected set; }
    public TextRuntime TitleText1 { get; protected set; }
    public ButtonCloseRuntime ButtonCloseInstance { get; protected set; }
    public ButtonCloseRuntime ButtonCloseInstance1 { get; protected set; }
    public DividerHorizontalRuntime DividerInstance { get; protected set; }
    public DividerHorizontalRuntime DividerInstance4 { get; protected set; }
    public LabelRuntime ResolutionLabel { get; protected set; }
    public ListBoxRuntime ResolutionBox { get; protected set; }
    public CheckBoxRuntime FullScreenCheckbox { get; protected set; }
    public ButtonStandardRuntime DetectResolutionsButton { get; protected set; }
    public ButtonStandardRuntime ClearResolutionsButton { get; protected set; }
    public ContainerRuntime ContainerInstance3 { get; protected set; }
    public DividerHorizontalRuntime DividerInstance1 { get; protected set; }
    public LabelRuntime MusicLabel { get; protected set; }
    public SliderRuntime MusicSlider { get; protected set; }
    public LabelRuntime SoundLabel { get; protected set; }
    public SliderRuntime SoundSlider { get; protected set; }
    public DividerHorizontalRuntime DividerInstance2 { get; protected set; }
    public LabelRuntime ControlLabel { get; protected set; }
    public RadioButtonRuntime KeyboardRadioButton { get; protected set; }
    public RadioButtonRuntime GamepadRadioButton { get; protected set; }
    public RadioButtonRuntime TouchScreenRadioButton { get; protected set; }
    public DividerHorizontalRuntime DividerInstance3 { get; protected set; }
    public LabelRuntime DifficultyLabel { get; protected set; }
    public NineSliceRuntime Background1 { get; protected set; }
    public ComboBoxRuntime ComboBoxInstance { get; protected set; }
    public ContainerRuntime ButtonContainer { get; protected set; }
    public CheckBoxRuntime BindingCheckbox { get; protected set; }
    public ButtonStandardRuntime BindingButton { get; protected set; }
    public ButtonDenyRuntime ButtonDenyInstance { get; protected set; }
    public ButtonConfirmRuntime ButtonConfirmInstance { get; protected set; }
    public ContainerRuntime DemoDialog { get; protected set; }
    public ContainerRuntime MarginContainer { get; protected set; }
    public LabelRuntime LabelInstance { get; protected set; }
    public TextBoxRuntime TextBoxInstance { get; protected set; }
    public PasswordBoxRuntime TextBoxInstance1 { get; protected set; }
    public ButtonConfirmRuntime ButtonWithNoEvents { get; protected set; }
    public ButtonConfirmRuntime ButtonConfirmInstance1 { get; protected set; }
    public NineSliceRuntime DemoHud { get; protected set; }
    public TextRuntime TitleText2 { get; protected set; }
    public ContainerRuntime MenuTitle2 { get; protected set; }
    public PercentBarRuntime PercentBarInstance { get; protected set; }
    public PercentBarRuntime HitpointsBar1 { get; protected set; }
    public PercentBarRuntime HitpointsBar2 { get; protected set; }
    public PercentBarRuntime HitpointsBar3 { get; protected set; }
    public PercentBarRuntime PercentBarInstance1 { get; protected set; }
    public PercentBarRuntime PercentBarInstance2 { get; protected set; }
    public DividerHorizontalRuntime DividerInstance5 { get; protected set; }
    public ButtonCloseRuntime ButtonCloseInstance2 { get; protected set; }
    public PercentBarRuntime PercentBarInstance3 { get; protected set; }
    public PercentBarRuntime PercentBarInstance4 { get; protected set; }
    public PercentBarIconRuntime PercentBarInstance5 { get; protected set; }
    public ContainerRuntime ContainerInstance { get; protected set; }
    public ContainerRuntime DemoTabbedMenu { get; protected set; }
    public ButtonStandardRuntime ShowToastButton { get; protected set; }
    public NineSliceRuntime TabMenuBackground { get; protected set; }
    public ContainerRuntime TabMarginContainer { get; protected set; }
    public ButtonTabRuntime Tab1 { get; protected set; }
    public ButtonTabRuntime Tab2 { get; protected set; }
    public ButtonTabRuntime Tab3 { get; protected set; }
    public ButtonCloseRuntime TabMenuClose { get; protected set; }
    public DividerHorizontalRuntime TabHeaderDivider { get; protected set; }
    public ContainerRuntime ListContainer { get; protected set; }
    public ButtonStandardIconRuntime PlayButton { get; protected set; }
    public ButtonStandardIconRuntime VideoSettingsButton { get; protected set; }
    public ButtonStandardIconRuntime AudioSettingsButton { get; protected set; }
    public ButtonStandardIconRuntime CreditsButton { get; protected set; }
    public ButtonStandardIconRuntime ExitButton { get; protected set; }
    public ButtonStandardRuntime ShowDialogButton { get; protected set; }
    public TreeViewRuntime TreeViewInstance { get; protected set; }
    public DialogBoxRuntime DialogBoxInstance { get; protected set; }
    public ContainerRuntime ContainerInstance1 { get; protected set; }
    public ContainerRuntime ContainerInstance2 { get; protected set; }
    public MenuItemRuntime MenuItemInstance { get; protected set; }
    public MenuItemRuntime MenuItemInstance1 { get; protected set; }
    public MenuItemRuntime MenuItemInstance2 { get; protected set; }
    public MenuItemRuntime MenuItemInstance3 { get; protected set; }
    public TextBoxRuntime SizedBasedOnContentsTextBox { get; protected set; }

    public DemoScreenGumRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
    {
        if(fullInstantiation)
        {
            var element = ObjectFinder.Self.GetElementSave("DemoScreenGum");
            element?.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
        }



    }
    public override void AfterFullCreation()
    {
        MenuInstance = this.GetGraphicalUiElementByName("MenuInstance") as GumFormsSample.Components.MenuRuntime;
        DemoSettingsMenu = this.GetGraphicalUiElementByName("DemoSettingsMenu") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        Background = this.GetGraphicalUiElementByName("Background") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        MenuTitle = this.GetGraphicalUiElementByName("MenuTitle") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        MenuTitle1 = this.GetGraphicalUiElementByName("MenuTitle1") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        MenuItems = this.GetGraphicalUiElementByName("MenuItems") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        TitleText = this.GetGraphicalUiElementByName("TitleText") as global::MonoGameGum.GueDeriving.TextRuntime;
        TitleText1 = this.GetGraphicalUiElementByName("TitleText1") as global::MonoGameGum.GueDeriving.TextRuntime;
        ButtonCloseInstance = this.GetGraphicalUiElementByName("ButtonCloseInstance") as GumFormsSample.Components.ButtonCloseRuntime;
        ButtonCloseInstance1 = this.GetGraphicalUiElementByName("ButtonCloseInstance1") as GumFormsSample.Components.ButtonCloseRuntime;
        DividerInstance = this.GetGraphicalUiElementByName("DividerInstance") as GumFormsSample.Components.DividerHorizontalRuntime;
        DividerInstance4 = this.GetGraphicalUiElementByName("DividerInstance4") as GumFormsSample.Components.DividerHorizontalRuntime;
        ResolutionLabel = this.GetGraphicalUiElementByName("ResolutionLabel") as GumFormsSample.Components.LabelRuntime;
        ResolutionBox = this.GetGraphicalUiElementByName("ResolutionBox") as GumFormsSample.Components.ListBoxRuntime;
        FullScreenCheckbox = this.GetGraphicalUiElementByName("FullScreenCheckbox") as GumFormsSample.Components.CheckBoxRuntime;
        DetectResolutionsButton = this.GetGraphicalUiElementByName("DetectResolutionsButton") as GumFormsSample.Components.ButtonStandardRuntime;
        ClearResolutionsButton = this.GetGraphicalUiElementByName("ClearResolutionsButton") as GumFormsSample.Components.ButtonStandardRuntime;
        ContainerInstance3 = this.GetGraphicalUiElementByName("ContainerInstance3") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        DividerInstance1 = this.GetGraphicalUiElementByName("DividerInstance1") as GumFormsSample.Components.DividerHorizontalRuntime;
        MusicLabel = this.GetGraphicalUiElementByName("MusicLabel") as GumFormsSample.Components.LabelRuntime;
        MusicSlider = this.GetGraphicalUiElementByName("MusicSlider") as GumFormsSample.Components.SliderRuntime;
        SoundLabel = this.GetGraphicalUiElementByName("SoundLabel") as GumFormsSample.Components.LabelRuntime;
        SoundSlider = this.GetGraphicalUiElementByName("SoundSlider") as GumFormsSample.Components.SliderRuntime;
        DividerInstance2 = this.GetGraphicalUiElementByName("DividerInstance2") as GumFormsSample.Components.DividerHorizontalRuntime;
        ControlLabel = this.GetGraphicalUiElementByName("ControlLabel") as GumFormsSample.Components.LabelRuntime;
        KeyboardRadioButton = this.GetGraphicalUiElementByName("KeyboardRadioButton") as GumFormsSample.Components.RadioButtonRuntime;
        GamepadRadioButton = this.GetGraphicalUiElementByName("GamepadRadioButton") as GumFormsSample.Components.RadioButtonRuntime;
        TouchScreenRadioButton = this.GetGraphicalUiElementByName("TouchScreenRadioButton") as GumFormsSample.Components.RadioButtonRuntime;
        DividerInstance3 = this.GetGraphicalUiElementByName("DividerInstance3") as GumFormsSample.Components.DividerHorizontalRuntime;
        DifficultyLabel = this.GetGraphicalUiElementByName("DifficultyLabel") as GumFormsSample.Components.LabelRuntime;
        Background1 = this.GetGraphicalUiElementByName("Background1") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        ComboBoxInstance = this.GetGraphicalUiElementByName("ComboBoxInstance") as GumFormsSample.Components.ComboBoxRuntime;
        ButtonContainer = this.GetGraphicalUiElementByName("ButtonContainer") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        BindingCheckbox = this.GetGraphicalUiElementByName("BindingCheckbox") as GumFormsSample.Components.CheckBoxRuntime;
        BindingButton = this.GetGraphicalUiElementByName("BindingButton") as GumFormsSample.Components.ButtonStandardRuntime;
        ButtonDenyInstance = this.GetGraphicalUiElementByName("ButtonDenyInstance") as GumFormsSample.Components.ButtonDenyRuntime;
        ButtonConfirmInstance = this.GetGraphicalUiElementByName("ButtonConfirmInstance") as GumFormsSample.Components.ButtonConfirmRuntime;
        DemoDialog = this.GetGraphicalUiElementByName("DemoDialog") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        MarginContainer = this.GetGraphicalUiElementByName("MarginContainer") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        LabelInstance = this.GetGraphicalUiElementByName("LabelInstance") as GumFormsSample.Components.LabelRuntime;
        TextBoxInstance = this.GetGraphicalUiElementByName("TextBoxInstance") as GumFormsSample.Components.TextBoxRuntime;
        TextBoxInstance1 = this.GetGraphicalUiElementByName("TextBoxInstance1") as GumFormsSample.Components.PasswordBoxRuntime;
        ButtonWithNoEvents = this.GetGraphicalUiElementByName("ButtonWithNoEvents") as GumFormsSample.Components.ButtonConfirmRuntime;
        ButtonConfirmInstance1 = this.GetGraphicalUiElementByName("ButtonConfirmInstance1") as GumFormsSample.Components.ButtonConfirmRuntime;
        DemoHud = this.GetGraphicalUiElementByName("DemoHud") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        TitleText2 = this.GetGraphicalUiElementByName("TitleText2") as global::MonoGameGum.GueDeriving.TextRuntime;
        MenuTitle2 = this.GetGraphicalUiElementByName("MenuTitle2") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        PercentBarInstance = this.GetGraphicalUiElementByName("PercentBarInstance") as GumFormsSample.Components.PercentBarRuntime;
        HitpointsBar1 = this.GetGraphicalUiElementByName("HitpointsBar1") as GumFormsSample.Components.PercentBarRuntime;
        HitpointsBar2 = this.GetGraphicalUiElementByName("HitpointsBar2") as GumFormsSample.Components.PercentBarRuntime;
        HitpointsBar3 = this.GetGraphicalUiElementByName("HitpointsBar3") as GumFormsSample.Components.PercentBarRuntime;
        PercentBarInstance1 = this.GetGraphicalUiElementByName("PercentBarInstance1") as GumFormsSample.Components.PercentBarRuntime;
        PercentBarInstance2 = this.GetGraphicalUiElementByName("PercentBarInstance2") as GumFormsSample.Components.PercentBarRuntime;
        DividerInstance5 = this.GetGraphicalUiElementByName("DividerInstance5") as GumFormsSample.Components.DividerHorizontalRuntime;
        ButtonCloseInstance2 = this.GetGraphicalUiElementByName("ButtonCloseInstance2") as GumFormsSample.Components.ButtonCloseRuntime;
        PercentBarInstance3 = this.GetGraphicalUiElementByName("PercentBarInstance3") as GumFormsSample.Components.PercentBarRuntime;
        PercentBarInstance4 = this.GetGraphicalUiElementByName("PercentBarInstance4") as GumFormsSample.Components.PercentBarRuntime;
        PercentBarInstance5 = this.GetGraphicalUiElementByName("PercentBarInstance5") as GumFormsSample.Components.PercentBarIconRuntime;
        ContainerInstance = this.GetGraphicalUiElementByName("ContainerInstance") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        DemoTabbedMenu = this.GetGraphicalUiElementByName("DemoTabbedMenu") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        ShowToastButton = this.GetGraphicalUiElementByName("ShowToastButton") as GumFormsSample.Components.ButtonStandardRuntime;
        TabMenuBackground = this.GetGraphicalUiElementByName("TabMenuBackground") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        TabMarginContainer = this.GetGraphicalUiElementByName("TabMarginContainer") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        Tab1 = this.GetGraphicalUiElementByName("Tab1") as GumFormsSample.Components.ButtonTabRuntime;
        Tab2 = this.GetGraphicalUiElementByName("Tab2") as GumFormsSample.Components.ButtonTabRuntime;
        Tab3 = this.GetGraphicalUiElementByName("Tab3") as GumFormsSample.Components.ButtonTabRuntime;
        TabMenuClose = this.GetGraphicalUiElementByName("TabMenuClose") as GumFormsSample.Components.ButtonCloseRuntime;
        TabHeaderDivider = this.GetGraphicalUiElementByName("TabHeaderDivider") as GumFormsSample.Components.DividerHorizontalRuntime;
        ListContainer = this.GetGraphicalUiElementByName("ListContainer") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        PlayButton = this.GetGraphicalUiElementByName("PlayButton") as GumFormsSample.Components.ButtonStandardIconRuntime;
        VideoSettingsButton = this.GetGraphicalUiElementByName("VideoSettingsButton") as GumFormsSample.Components.ButtonStandardIconRuntime;
        AudioSettingsButton = this.GetGraphicalUiElementByName("AudioSettingsButton") as GumFormsSample.Components.ButtonStandardIconRuntime;
        CreditsButton = this.GetGraphicalUiElementByName("CreditsButton") as GumFormsSample.Components.ButtonStandardIconRuntime;
        ExitButton = this.GetGraphicalUiElementByName("ExitButton") as GumFormsSample.Components.ButtonStandardIconRuntime;
        ShowDialogButton = this.GetGraphicalUiElementByName("ShowDialogButton") as GumFormsSample.Components.ButtonStandardRuntime;
        TreeViewInstance = this.GetGraphicalUiElementByName("TreeViewInstance") as GumFormsSample.Components.TreeViewRuntime;
        DialogBoxInstance = this.GetGraphicalUiElementByName("DialogBoxInstance") as GumFormsSample.Components.DialogBoxRuntime;
        ContainerInstance1 = this.GetGraphicalUiElementByName("ContainerInstance1") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        ContainerInstance2 = this.GetGraphicalUiElementByName("ContainerInstance2") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        MenuItemInstance = this.GetGraphicalUiElementByName("MenuItemInstance") as GumFormsSample.Components.MenuItemRuntime;
        MenuItemInstance1 = this.GetGraphicalUiElementByName("MenuItemInstance1") as GumFormsSample.Components.MenuItemRuntime;
        MenuItemInstance2 = this.GetGraphicalUiElementByName("MenuItemInstance2") as GumFormsSample.Components.MenuItemRuntime;
        MenuItemInstance3 = this.GetGraphicalUiElementByName("MenuItemInstance3") as GumFormsSample.Components.MenuItemRuntime;
        SizedBasedOnContentsTextBox = this.GetGraphicalUiElementByName("SizedBasedOnContentsTextBox") as GumFormsSample.Components.TextBoxRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
