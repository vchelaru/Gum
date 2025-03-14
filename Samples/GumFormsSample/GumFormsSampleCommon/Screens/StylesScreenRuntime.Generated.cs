//Code for StylesScreen
using GumRuntime;
using GumFormsSample.Components;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
namespace GumFormsSample.Screens
{
    public partial class StylesScreenRuntime:Gum.Wireframe.BindableGue
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
            TextStyleContainer = this.GetGraphicalUiElementByName("TextStyleContainer") as ContainerRuntime;
            TextTitle = this.GetGraphicalUiElementByName("TextTitle") as TextRuntime;
            TextH1 = this.GetGraphicalUiElementByName("TextH1") as TextRuntime;
            TextH2 = this.GetGraphicalUiElementByName("TextH2") as TextRuntime;
            TextH3 = this.GetGraphicalUiElementByName("TextH3") as TextRuntime;
            TextNormal = this.GetGraphicalUiElementByName("TextNormal") as TextRuntime;
            TextStrong = this.GetGraphicalUiElementByName("TextStrong") as TextRuntime;
            TextEmphasis = this.GetGraphicalUiElementByName("TextEmphasis") as TextRuntime;
            TextSmall = this.GetGraphicalUiElementByName("TextSmall") as TextRuntime;
            TextTiny = this.GetGraphicalUiElementByName("TextTiny") as TextRuntime;
            Solid = this.GetGraphicalUiElementByName("Solid") as NineSliceRuntime;
            Bordered = this.GetGraphicalUiElementByName("Bordered") as NineSliceRuntime;
            BracketHorizontal = this.GetGraphicalUiElementByName("BracketHorizontal") as NineSliceRuntime;
            BracketVertical = this.GetGraphicalUiElementByName("BracketVertical") as NineSliceRuntime;
            Tab = this.GetGraphicalUiElementByName("Tab") as NineSliceRuntime;
            TabBordered = this.GetGraphicalUiElementByName("TabBordered") as NineSliceRuntime;
            Outlined = this.GetGraphicalUiElementByName("Outlined") as NineSliceRuntime;
            OutlinedHeavy = this.GetGraphicalUiElementByName("OutlinedHeavy") as NineSliceRuntime;
            Panel = this.GetGraphicalUiElementByName("Panel") as NineSliceRuntime;
            NineSliceStyleContainer = this.GetGraphicalUiElementByName("NineSliceStyleContainer") as ContainerRuntime;
            ButtonsContainer = this.GetGraphicalUiElementByName("ButtonsContainer") as ContainerRuntime;
            ButtonStandardInstance = this.GetGraphicalUiElementByName("ButtonStandardInstance") as ButtonStandardRuntime;
            ButtonStandardIconInstance = this.GetGraphicalUiElementByName("ButtonStandardIconInstance") as ButtonStandardIconRuntime;
            ButtonTabInstance = this.GetGraphicalUiElementByName("ButtonTabInstance") as ButtonTabRuntime;
            ButtonIconInstance = this.GetGraphicalUiElementByName("ButtonIconInstance") as ButtonIconRuntime;
            ButtonConfirmInstance = this.GetGraphicalUiElementByName("ButtonConfirmInstance") as ButtonConfirmRuntime;
            ButtonDenyInstance = this.GetGraphicalUiElementByName("ButtonDenyInstance") as ButtonDenyRuntime;
            ButtonCloseInstance = this.GetGraphicalUiElementByName("ButtonCloseInstance") as ButtonCloseRuntime;
            ElementsContainer = this.GetGraphicalUiElementByName("ElementsContainer") as ContainerRuntime;
            PercentBarPrimary = this.GetGraphicalUiElementByName("PercentBarPrimary") as PercentBarRuntime;
            PercentBarLinesDecor = this.GetGraphicalUiElementByName("PercentBarLinesDecor") as PercentBarRuntime;
            PercentBarCautionDecor = this.GetGraphicalUiElementByName("PercentBarCautionDecor") as PercentBarRuntime;
            PercentBarIconPrimary = this.GetGraphicalUiElementByName("PercentBarIconPrimary") as PercentBarIconRuntime;
            PercentBarIconLinesDecor = this.GetGraphicalUiElementByName("PercentBarIconLinesDecor") as PercentBarIconRuntime;
            PercentBarIconCautionDecor = this.GetGraphicalUiElementByName("PercentBarIconCautionDecor") as PercentBarIconRuntime;
            ControlsContainer = this.GetGraphicalUiElementByName("ControlsContainer") as ContainerRuntime;
            LabelInstance = this.GetGraphicalUiElementByName("LabelInstance") as LabelRuntime;
            CheckBoxInstance = this.GetGraphicalUiElementByName("CheckBoxInstance") as CheckBoxRuntime;
            RadioButtonInstance = this.GetGraphicalUiElementByName("RadioButtonInstance") as RadioButtonRuntime;
            ComboBoxInstance = this.GetGraphicalUiElementByName("ComboBoxInstance") as ComboBoxRuntime;
            ListBoxInstance = this.GetGraphicalUiElementByName("ListBoxInstance") as ListBoxRuntime;
            SliderInstance = this.GetGraphicalUiElementByName("SliderInstance") as SliderRuntime;
            TextBoxInstance = this.GetGraphicalUiElementByName("TextBoxInstance") as TextBoxRuntime;
            PasswordBoxInstance = this.GetGraphicalUiElementByName("PasswordBoxInstance") as PasswordBoxRuntime;
            DividerVerticalInstance = this.GetGraphicalUiElementByName("DividerVerticalInstance") as DividerVerticalRuntime;
            DividerHorizontalInstance = this.GetGraphicalUiElementByName("DividerHorizontalInstance") as DividerHorizontalRuntime;
            IconInstance = this.GetGraphicalUiElementByName("IconInstance") as IconRuntime;
            CautionLinesInstance = this.GetGraphicalUiElementByName("CautionLinesInstance") as CautionLinesRuntime;
            VerticalLinesInstance = this.GetGraphicalUiElementByName("VerticalLinesInstance") as VerticalLinesRuntime;
            ColorContainer = this.GetGraphicalUiElementByName("ColorContainer") as ContainerRuntime;
            TextBlack = this.GetGraphicalUiElementByName("TextBlack") as TextRuntime;
            TextDarkGray = this.GetGraphicalUiElementByName("TextDarkGray") as TextRuntime;
            TextGray = this.GetGraphicalUiElementByName("TextGray") as TextRuntime;
            TextLightGray = this.GetGraphicalUiElementByName("TextLightGray") as TextRuntime;
            TextWhite = this.GetGraphicalUiElementByName("TextWhite") as TextRuntime;
            TextPrimaryDark = this.GetGraphicalUiElementByName("TextPrimaryDark") as TextRuntime;
            TextPrimary = this.GetGraphicalUiElementByName("TextPrimary") as TextRuntime;
            TextPrimaryLight = this.GetGraphicalUiElementByName("TextPrimaryLight") as TextRuntime;
            TextAccent = this.GetGraphicalUiElementByName("TextAccent") as TextRuntime;
            TextSuccess = this.GetGraphicalUiElementByName("TextSuccess") as TextRuntime;
            TextWarning = this.GetGraphicalUiElementByName("TextWarning") as TextRuntime;
            TextWarning1 = this.GetGraphicalUiElementByName("TextWarning1") as TextRuntime;
            KeyboardInstance = this.GetGraphicalUiElementByName("KeyboardInstance") as KeyboardRuntime;
            KeyboardContainer = this.GetGraphicalUiElementByName("KeyboardContainer") as ContainerRuntime;
            TreeViewInstance = this.GetGraphicalUiElementByName("TreeViewInstance") as TreeViewRuntime;
            DialogBoxInstance = this.GetGraphicalUiElementByName("DialogBoxInstance") as DialogBoxRuntime;
            CustomInitialize();
        }
        //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
        partial void CustomInitialize();
    }
}
