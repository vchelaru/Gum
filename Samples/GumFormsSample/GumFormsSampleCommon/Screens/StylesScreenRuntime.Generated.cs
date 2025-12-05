//Code for StylesScreen
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
partial class StylesScreenRuntime : Gum.Wireframe.BindableGue
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("StylesScreen", typeof(StylesScreenRuntime));
    }
    public ContainerRuntime TextStyleContainer { get; protected set; }
    public TextRuntime TextTitle { get; protected set; }
    public TextRuntime TextH1 { get; protected set; }
    public TextRuntime TextH2 { get; protected set; }
    public TextRuntime TextH3 { get; protected set; }
    public TextRuntime TextNormal { get; protected set; }
    public TextRuntime TextStrong { get; protected set; }
    public TextRuntime TextEmphasis { get; protected set; }
    public TextRuntime TextSmall { get; protected set; }
    public TextRuntime TextTiny { get; protected set; }
    public NineSliceRuntime Solid { get; protected set; }
    public NineSliceRuntime Bordered { get; protected set; }
    public NineSliceRuntime BracketHorizontal { get; protected set; }
    public NineSliceRuntime BracketVertical { get; protected set; }
    public NineSliceRuntime Tab { get; protected set; }
    public NineSliceRuntime TabBordered { get; protected set; }
    public NineSliceRuntime Outlined { get; protected set; }
    public NineSliceRuntime OutlinedHeavy { get; protected set; }
    public NineSliceRuntime Panel { get; protected set; }
    public ContainerRuntime NineSliceStyleContainer { get; protected set; }
    public ContainerRuntime ButtonsContainer { get; protected set; }
    public ButtonStandardRuntime ButtonStandardInstance { get; protected set; }
    public ButtonStandardIconRuntime ButtonStandardIconInstance { get; protected set; }
    public ButtonTabRuntime ButtonTabInstance { get; protected set; }
    public ButtonIconRuntime ButtonIconInstance { get; protected set; }
    public ButtonConfirmRuntime ButtonConfirmInstance { get; protected set; }
    public ButtonDenyRuntime ButtonDenyInstance { get; protected set; }
    public ButtonCloseRuntime ButtonCloseInstance { get; protected set; }
    public ContainerRuntime ElementsContainer { get; protected set; }
    public PercentBarRuntime PercentBarPrimary { get; protected set; }
    public PercentBarRuntime PercentBarLinesDecor { get; protected set; }
    public PercentBarRuntime PercentBarCautionDecor { get; protected set; }
    public PercentBarIconRuntime PercentBarIconPrimary { get; protected set; }
    public PercentBarIconRuntime PercentBarIconLinesDecor { get; protected set; }
    public PercentBarIconRuntime PercentBarIconCautionDecor { get; protected set; }
    public ContainerRuntime ControlsContainer { get; protected set; }
    public LabelRuntime LabelInstance { get; protected set; }
    public CheckBoxRuntime CheckBoxInstance { get; protected set; }
    public RadioButtonRuntime RadioButtonInstance { get; protected set; }
    public ComboBoxRuntime ComboBoxInstance { get; protected set; }
    public ListBoxRuntime ListBoxInstance { get; protected set; }
    public SliderRuntime SliderInstance { get; protected set; }
    public TextBoxRuntime TextBoxInstance { get; protected set; }
    public PasswordBoxRuntime PasswordBoxInstance { get; protected set; }
    public DividerVerticalRuntime DividerVerticalInstance { get; protected set; }
    public DividerHorizontalRuntime DividerHorizontalInstance { get; protected set; }
    public IconRuntime IconInstance { get; protected set; }
    public CautionLinesRuntime CautionLinesInstance { get; protected set; }
    public VerticalLinesRuntime VerticalLinesInstance { get; protected set; }
    public ContainerRuntime ColorContainer { get; protected set; }
    public TextRuntime TextBlack { get; protected set; }
    public TextRuntime TextDarkGray { get; protected set; }
    public TextRuntime TextGray { get; protected set; }
    public TextRuntime TextLightGray { get; protected set; }
    public TextRuntime TextWhite { get; protected set; }
    public TextRuntime TextPrimaryDark { get; protected set; }
    public TextRuntime TextPrimary { get; protected set; }
    public TextRuntime TextPrimaryLight { get; protected set; }
    public TextRuntime TextAccent { get; protected set; }
    public TextRuntime TextSuccess { get; protected set; }
    public TextRuntime TextWarning { get; protected set; }
    public TextRuntime TextWarning1 { get; protected set; }
    public KeyboardRuntime KeyboardInstance { get; protected set; }
    public ContainerRuntime KeyboardContainer { get; protected set; }
    public TreeViewRuntime TreeViewInstance { get; protected set; }
    public DialogBoxRuntime DialogBoxInstance { get; protected set; }

    public StylesScreenRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
    {
        if(fullInstantiation)
        {
            var element = ObjectFinder.Self.GetElementSave("StylesScreen");
            element?.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
        }



    }
    public override void AfterFullCreation()
    {
        TextStyleContainer = this.GetGraphicalUiElementByName("TextStyleContainer") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        TextTitle = this.GetGraphicalUiElementByName("TextTitle") as global::MonoGameGum.GueDeriving.TextRuntime;
        TextH1 = this.GetGraphicalUiElementByName("TextH1") as global::MonoGameGum.GueDeriving.TextRuntime;
        TextH2 = this.GetGraphicalUiElementByName("TextH2") as global::MonoGameGum.GueDeriving.TextRuntime;
        TextH3 = this.GetGraphicalUiElementByName("TextH3") as global::MonoGameGum.GueDeriving.TextRuntime;
        TextNormal = this.GetGraphicalUiElementByName("TextNormal") as global::MonoGameGum.GueDeriving.TextRuntime;
        TextStrong = this.GetGraphicalUiElementByName("TextStrong") as global::MonoGameGum.GueDeriving.TextRuntime;
        TextEmphasis = this.GetGraphicalUiElementByName("TextEmphasis") as global::MonoGameGum.GueDeriving.TextRuntime;
        TextSmall = this.GetGraphicalUiElementByName("TextSmall") as global::MonoGameGum.GueDeriving.TextRuntime;
        TextTiny = this.GetGraphicalUiElementByName("TextTiny") as global::MonoGameGum.GueDeriving.TextRuntime;
        Solid = this.GetGraphicalUiElementByName("Solid") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        Bordered = this.GetGraphicalUiElementByName("Bordered") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        BracketHorizontal = this.GetGraphicalUiElementByName("BracketHorizontal") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        BracketVertical = this.GetGraphicalUiElementByName("BracketVertical") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        Tab = this.GetGraphicalUiElementByName("Tab") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        TabBordered = this.GetGraphicalUiElementByName("TabBordered") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        Outlined = this.GetGraphicalUiElementByName("Outlined") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        OutlinedHeavy = this.GetGraphicalUiElementByName("OutlinedHeavy") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        Panel = this.GetGraphicalUiElementByName("Panel") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        NineSliceStyleContainer = this.GetGraphicalUiElementByName("NineSliceStyleContainer") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        ButtonsContainer = this.GetGraphicalUiElementByName("ButtonsContainer") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        ButtonStandardInstance = this.GetGraphicalUiElementByName("ButtonStandardInstance") as GumFormsSample.Components.ButtonStandardRuntime;
        ButtonStandardIconInstance = this.GetGraphicalUiElementByName("ButtonStandardIconInstance") as GumFormsSample.Components.ButtonStandardIconRuntime;
        ButtonTabInstance = this.GetGraphicalUiElementByName("ButtonTabInstance") as GumFormsSample.Components.ButtonTabRuntime;
        ButtonIconInstance = this.GetGraphicalUiElementByName("ButtonIconInstance") as GumFormsSample.Components.ButtonIconRuntime;
        ButtonConfirmInstance = this.GetGraphicalUiElementByName("ButtonConfirmInstance") as GumFormsSample.Components.ButtonConfirmRuntime;
        ButtonDenyInstance = this.GetGraphicalUiElementByName("ButtonDenyInstance") as GumFormsSample.Components.ButtonDenyRuntime;
        ButtonCloseInstance = this.GetGraphicalUiElementByName("ButtonCloseInstance") as GumFormsSample.Components.ButtonCloseRuntime;
        ElementsContainer = this.GetGraphicalUiElementByName("ElementsContainer") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        PercentBarPrimary = this.GetGraphicalUiElementByName("PercentBarPrimary") as GumFormsSample.Components.PercentBarRuntime;
        PercentBarLinesDecor = this.GetGraphicalUiElementByName("PercentBarLinesDecor") as GumFormsSample.Components.PercentBarRuntime;
        PercentBarCautionDecor = this.GetGraphicalUiElementByName("PercentBarCautionDecor") as GumFormsSample.Components.PercentBarRuntime;
        PercentBarIconPrimary = this.GetGraphicalUiElementByName("PercentBarIconPrimary") as GumFormsSample.Components.PercentBarIconRuntime;
        PercentBarIconLinesDecor = this.GetGraphicalUiElementByName("PercentBarIconLinesDecor") as GumFormsSample.Components.PercentBarIconRuntime;
        PercentBarIconCautionDecor = this.GetGraphicalUiElementByName("PercentBarIconCautionDecor") as GumFormsSample.Components.PercentBarIconRuntime;
        ControlsContainer = this.GetGraphicalUiElementByName("ControlsContainer") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        LabelInstance = this.GetGraphicalUiElementByName("LabelInstance") as GumFormsSample.Components.LabelRuntime;
        CheckBoxInstance = this.GetGraphicalUiElementByName("CheckBoxInstance") as GumFormsSample.Components.CheckBoxRuntime;
        RadioButtonInstance = this.GetGraphicalUiElementByName("RadioButtonInstance") as GumFormsSample.Components.RadioButtonRuntime;
        ComboBoxInstance = this.GetGraphicalUiElementByName("ComboBoxInstance") as GumFormsSample.Components.ComboBoxRuntime;
        ListBoxInstance = this.GetGraphicalUiElementByName("ListBoxInstance") as GumFormsSample.Components.ListBoxRuntime;
        SliderInstance = this.GetGraphicalUiElementByName("SliderInstance") as GumFormsSample.Components.SliderRuntime;
        TextBoxInstance = this.GetGraphicalUiElementByName("TextBoxInstance") as GumFormsSample.Components.TextBoxRuntime;
        PasswordBoxInstance = this.GetGraphicalUiElementByName("PasswordBoxInstance") as GumFormsSample.Components.PasswordBoxRuntime;
        DividerVerticalInstance = this.GetGraphicalUiElementByName("DividerVerticalInstance") as GumFormsSample.Components.DividerVerticalRuntime;
        DividerHorizontalInstance = this.GetGraphicalUiElementByName("DividerHorizontalInstance") as GumFormsSample.Components.DividerHorizontalRuntime;
        IconInstance = this.GetGraphicalUiElementByName("IconInstance") as GumFormsSample.Components.IconRuntime;
        CautionLinesInstance = this.GetGraphicalUiElementByName("CautionLinesInstance") as GumFormsSample.Components.CautionLinesRuntime;
        VerticalLinesInstance = this.GetGraphicalUiElementByName("VerticalLinesInstance") as GumFormsSample.Components.VerticalLinesRuntime;
        ColorContainer = this.GetGraphicalUiElementByName("ColorContainer") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        TextBlack = this.GetGraphicalUiElementByName("TextBlack") as global::MonoGameGum.GueDeriving.TextRuntime;
        TextDarkGray = this.GetGraphicalUiElementByName("TextDarkGray") as global::MonoGameGum.GueDeriving.TextRuntime;
        TextGray = this.GetGraphicalUiElementByName("TextGray") as global::MonoGameGum.GueDeriving.TextRuntime;
        TextLightGray = this.GetGraphicalUiElementByName("TextLightGray") as global::MonoGameGum.GueDeriving.TextRuntime;
        TextWhite = this.GetGraphicalUiElementByName("TextWhite") as global::MonoGameGum.GueDeriving.TextRuntime;
        TextPrimaryDark = this.GetGraphicalUiElementByName("TextPrimaryDark") as global::MonoGameGum.GueDeriving.TextRuntime;
        TextPrimary = this.GetGraphicalUiElementByName("TextPrimary") as global::MonoGameGum.GueDeriving.TextRuntime;
        TextPrimaryLight = this.GetGraphicalUiElementByName("TextPrimaryLight") as global::MonoGameGum.GueDeriving.TextRuntime;
        TextAccent = this.GetGraphicalUiElementByName("TextAccent") as global::MonoGameGum.GueDeriving.TextRuntime;
        TextSuccess = this.GetGraphicalUiElementByName("TextSuccess") as global::MonoGameGum.GueDeriving.TextRuntime;
        TextWarning = this.GetGraphicalUiElementByName("TextWarning") as global::MonoGameGum.GueDeriving.TextRuntime;
        TextWarning1 = this.GetGraphicalUiElementByName("TextWarning1") as global::MonoGameGum.GueDeriving.TextRuntime;
        KeyboardInstance = this.GetGraphicalUiElementByName("KeyboardInstance") as GumFormsSample.Components.KeyboardRuntime;
        KeyboardContainer = this.GetGraphicalUiElementByName("KeyboardContainer") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        TreeViewInstance = this.GetGraphicalUiElementByName("TreeViewInstance") as GumFormsSample.Components.TreeViewRuntime;
        DialogBoxInstance = this.GetGraphicalUiElementByName("DialogBoxInstance") as GumFormsSample.Components.DialogBoxRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
