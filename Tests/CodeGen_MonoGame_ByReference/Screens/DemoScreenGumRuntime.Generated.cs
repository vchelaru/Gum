//Code for DemoScreenGum
using GumRuntime;
using System.Linq;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using CodeGen_MonoGame_ByReference.Components.Controls;
using CodeGen_MonoGame_ByReference.Components.Elements;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

namespace CodeGen_MonoGame_ByReference.Screens;
partial class DemoScreenGumRuntime : Gum.Wireframe.BindableGue
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("DemoScreenGum", typeof(DemoScreenGumRuntime));
    }
    public ContainerRuntime DemoSettingsMenu { get; protected set; }
    public NineSliceRuntime Background { get; protected set; }
    public ContainerRuntime MenuTitle { get; protected set; }
    public ContainerRuntime MenuTitle1 { get; protected set; }
    public ContainerRuntime MenuItems { get; protected set; }
    public TextRuntime TitleText { get; protected set; }
    public TextRuntime TitleText1 { get; protected set; }
    public ButtonCloseRuntime ButtonCloseInstance1 { get; protected set; }
    public DividerHorizontalRuntime DividerInstance { get; protected set; }
    public DividerHorizontalRuntime DividerInstance4 { get; protected set; }
    public LabelRuntime ResolutionLabel { get; protected set; }
    public ListBoxRuntime ResolutionBox { get; protected set; }
    public ButtonStandardRuntime DetectResolutionsButton { get; protected set; }
    public ButtonStandardRuntime ShowDialogButton { get; protected set; }
    public ButtonStandardRuntime ShowToastButton { get; protected set; }
    public CheckBoxRuntime FullScreenCheckbox { get; protected set; }
    public DividerHorizontalRuntime DividerInstance1 { get; protected set; }
    public LabelRuntime MusicLabel { get; protected set; }
    public SliderRuntime MusicSlider { get; protected set; }
    public LabelRuntime SoundLabel { get; protected set; }
    public SliderRuntime SoundSlider { get; protected set; }
    public DividerHorizontalRuntime DividerInstance2 { get; protected set; }
    public LabelRuntime ControlLabel { get; protected set; }
    public RadioButtonRuntime RadioButtonInstance { get; protected set; }
    public RadioButtonRuntime RadioButtonInstance1 { get; protected set; }
    public RadioButtonRuntime RadioButtonInstance2 { get; protected set; }
    public DividerHorizontalRuntime DividerInstance3 { get; protected set; }
    public LabelRuntime DifficultyLabel { get; protected set; }
    public NineSliceRuntime Background1 { get; protected set; }
    public ComboBoxRuntime ComboBoxInstance { get; protected set; }
    public ContainerRuntime ButtonContainer { get; protected set; }
    public ButtonConfirmRuntime ButtonConfirmInstance { get; protected set; }
    public ButtonConfirmRuntime WindowOkButton { get; protected set; }
    public ButtonDenyRuntime ButtonDenyInstance { get; protected set; }
    public ContainerRuntime DemoDialog { get; protected set; }
    public ContainerRuntime MarginContainer { get; protected set; }
    public LabelRuntime LabelInstance { get; protected set; }
    public TextBoxRuntime TextBoxInstance { get; protected set; }
    public PasswordBoxRuntime TextBoxInstance1 { get; protected set; }
    public TextBoxRuntime MultiLineTextBox { get; protected set; }
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
    public TreeViewRuntime TreeViewInstance { get; protected set; }
    public DialogBoxRuntime DialogBoxInstance { get; protected set; }
    public WindowStandardRuntime WindowStandardInstance { get; protected set; }
    public LabelRuntime LabelInstance1 { get; protected set; }

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
        DemoSettingsMenu = this.GetGraphicalUiElementByName("DemoSettingsMenu") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        Background = this.GetGraphicalUiElementByName("Background") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        MenuTitle = this.GetGraphicalUiElementByName("MenuTitle") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        MenuTitle1 = this.GetGraphicalUiElementByName("MenuTitle1") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        MenuItems = this.GetGraphicalUiElementByName("MenuItems") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        TitleText = this.GetGraphicalUiElementByName("TitleText") as global::MonoGameGum.GueDeriving.TextRuntime;
        TitleText1 = this.GetGraphicalUiElementByName("TitleText1") as global::MonoGameGum.GueDeriving.TextRuntime;
        ButtonCloseInstance1 = this.GetGraphicalUiElementByName("ButtonCloseInstance1") as global::MonoGameGum.GueDeriving.ButtonCloseRuntime;
        DividerInstance = this.GetGraphicalUiElementByName("DividerInstance") as global::MonoGameGum.GueDeriving.DividerHorizontalRuntime;
        DividerInstance4 = this.GetGraphicalUiElementByName("DividerInstance4") as global::MonoGameGum.GueDeriving.DividerHorizontalRuntime;
        ResolutionLabel = this.GetGraphicalUiElementByName("ResolutionLabel") as global::MonoGameGum.GueDeriving.LabelRuntime;
        ResolutionBox = this.GetGraphicalUiElementByName("ResolutionBox") as global::MonoGameGum.GueDeriving.ListBoxRuntime;
        DetectResolutionsButton = this.GetGraphicalUiElementByName("DetectResolutionsButton") as global::MonoGameGum.GueDeriving.ButtonStandardRuntime;
        ShowDialogButton = this.GetGraphicalUiElementByName("ShowDialogButton") as global::MonoGameGum.GueDeriving.ButtonStandardRuntime;
        ShowToastButton = this.GetGraphicalUiElementByName("ShowToastButton") as global::MonoGameGum.GueDeriving.ButtonStandardRuntime;
        FullScreenCheckbox = this.GetGraphicalUiElementByName("FullScreenCheckbox") as global::MonoGameGum.GueDeriving.CheckBoxRuntime;
        DividerInstance1 = this.GetGraphicalUiElementByName("DividerInstance1") as global::MonoGameGum.GueDeriving.DividerHorizontalRuntime;
        MusicLabel = this.GetGraphicalUiElementByName("MusicLabel") as global::MonoGameGum.GueDeriving.LabelRuntime;
        MusicSlider = this.GetGraphicalUiElementByName("MusicSlider") as global::MonoGameGum.GueDeriving.SliderRuntime;
        SoundLabel = this.GetGraphicalUiElementByName("SoundLabel") as global::MonoGameGum.GueDeriving.LabelRuntime;
        SoundSlider = this.GetGraphicalUiElementByName("SoundSlider") as global::MonoGameGum.GueDeriving.SliderRuntime;
        DividerInstance2 = this.GetGraphicalUiElementByName("DividerInstance2") as global::MonoGameGum.GueDeriving.DividerHorizontalRuntime;
        ControlLabel = this.GetGraphicalUiElementByName("ControlLabel") as global::MonoGameGum.GueDeriving.LabelRuntime;
        RadioButtonInstance = this.GetGraphicalUiElementByName("RadioButtonInstance") as global::MonoGameGum.GueDeriving.RadioButtonRuntime;
        RadioButtonInstance1 = this.GetGraphicalUiElementByName("RadioButtonInstance1") as global::MonoGameGum.GueDeriving.RadioButtonRuntime;
        RadioButtonInstance2 = this.GetGraphicalUiElementByName("RadioButtonInstance2") as global::MonoGameGum.GueDeriving.RadioButtonRuntime;
        DividerInstance3 = this.GetGraphicalUiElementByName("DividerInstance3") as global::MonoGameGum.GueDeriving.DividerHorizontalRuntime;
        DifficultyLabel = this.GetGraphicalUiElementByName("DifficultyLabel") as global::MonoGameGum.GueDeriving.LabelRuntime;
        Background1 = this.GetGraphicalUiElementByName("Background1") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        ComboBoxInstance = this.GetGraphicalUiElementByName("ComboBoxInstance") as global::MonoGameGum.GueDeriving.ComboBoxRuntime;
        ButtonContainer = this.GetGraphicalUiElementByName("ButtonContainer") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        ButtonConfirmInstance = this.GetGraphicalUiElementByName("ButtonConfirmInstance") as global::MonoGameGum.GueDeriving.ButtonConfirmRuntime;
        WindowOkButton = this.GetGraphicalUiElementByName("WindowOkButton") as global::MonoGameGum.GueDeriving.ButtonConfirmRuntime;
        ButtonDenyInstance = this.GetGraphicalUiElementByName("ButtonDenyInstance") as global::MonoGameGum.GueDeriving.ButtonDenyRuntime;
        DemoDialog = this.GetGraphicalUiElementByName("DemoDialog") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        MarginContainer = this.GetGraphicalUiElementByName("MarginContainer") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        LabelInstance = this.GetGraphicalUiElementByName("LabelInstance") as global::MonoGameGum.GueDeriving.LabelRuntime;
        TextBoxInstance = this.GetGraphicalUiElementByName("TextBoxInstance") as global::MonoGameGum.GueDeriving.TextBoxRuntime;
        TextBoxInstance1 = this.GetGraphicalUiElementByName("TextBoxInstance1") as global::MonoGameGum.GueDeriving.PasswordBoxRuntime;
        MultiLineTextBox = this.GetGraphicalUiElementByName("MultiLineTextBox") as global::MonoGameGum.GueDeriving.TextBoxRuntime;
        DemoHud = this.GetGraphicalUiElementByName("DemoHud") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        TitleText2 = this.GetGraphicalUiElementByName("TitleText2") as global::MonoGameGum.GueDeriving.TextRuntime;
        MenuTitle2 = this.GetGraphicalUiElementByName("MenuTitle2") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        PercentBarInstance = this.GetGraphicalUiElementByName("PercentBarInstance") as global::MonoGameGum.GueDeriving.PercentBarRuntime;
        HitpointsBar1 = this.GetGraphicalUiElementByName("HitpointsBar1") as global::MonoGameGum.GueDeriving.PercentBarRuntime;
        HitpointsBar2 = this.GetGraphicalUiElementByName("HitpointsBar2") as global::MonoGameGum.GueDeriving.PercentBarRuntime;
        HitpointsBar3 = this.GetGraphicalUiElementByName("HitpointsBar3") as global::MonoGameGum.GueDeriving.PercentBarRuntime;
        PercentBarInstance1 = this.GetGraphicalUiElementByName("PercentBarInstance1") as global::MonoGameGum.GueDeriving.PercentBarRuntime;
        PercentBarInstance2 = this.GetGraphicalUiElementByName("PercentBarInstance2") as global::MonoGameGum.GueDeriving.PercentBarRuntime;
        DividerInstance5 = this.GetGraphicalUiElementByName("DividerInstance5") as global::MonoGameGum.GueDeriving.DividerHorizontalRuntime;
        ButtonCloseInstance2 = this.GetGraphicalUiElementByName("ButtonCloseInstance2") as global::MonoGameGum.GueDeriving.ButtonCloseRuntime;
        PercentBarInstance3 = this.GetGraphicalUiElementByName("PercentBarInstance3") as global::MonoGameGum.GueDeriving.PercentBarRuntime;
        PercentBarInstance4 = this.GetGraphicalUiElementByName("PercentBarInstance4") as global::MonoGameGum.GueDeriving.PercentBarRuntime;
        PercentBarInstance5 = this.GetGraphicalUiElementByName("PercentBarInstance5") as global::MonoGameGum.GueDeriving.PercentBarIconRuntime;
        ContainerInstance = this.GetGraphicalUiElementByName("ContainerInstance") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        TreeViewInstance = this.GetGraphicalUiElementByName("TreeViewInstance") as global::MonoGameGum.GueDeriving.TreeViewRuntime;
        DialogBoxInstance = this.GetGraphicalUiElementByName("DialogBoxInstance") as global::MonoGameGum.GueDeriving.DialogBoxRuntime;
        WindowStandardInstance = this.GetGraphicalUiElementByName("WindowStandardInstance") as global::MonoGameGum.GueDeriving.WindowStandardRuntime;
        LabelInstance1 = this.GetGraphicalUiElementByName("LabelInstance1") as global::MonoGameGum.GueDeriving.LabelRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
