//Code for DemoScreenGum
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
    public partial class DemoScreenGumRuntime:Gum.Wireframe.BindableGue
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
            MenuInstance = this.GetGraphicalUiElementByName("MenuInstance") as MenuRuntime;
            DemoSettingsMenu = this.GetGraphicalUiElementByName("DemoSettingsMenu") as ContainerRuntime;
            Background = this.GetGraphicalUiElementByName("Background") as NineSliceRuntime;
            MenuTitle = this.GetGraphicalUiElementByName("MenuTitle") as ContainerRuntime;
            MenuTitle1 = this.GetGraphicalUiElementByName("MenuTitle1") as ContainerRuntime;
            MenuItems = this.GetGraphicalUiElementByName("MenuItems") as ContainerRuntime;
            TitleText = this.GetGraphicalUiElementByName("TitleText") as TextRuntime;
            TitleText1 = this.GetGraphicalUiElementByName("TitleText1") as TextRuntime;
            ButtonCloseInstance = this.GetGraphicalUiElementByName("ButtonCloseInstance") as ButtonCloseRuntime;
            ButtonCloseInstance1 = this.GetGraphicalUiElementByName("ButtonCloseInstance1") as ButtonCloseRuntime;
            DividerInstance = this.GetGraphicalUiElementByName("DividerInstance") as DividerHorizontalRuntime;
            DividerInstance4 = this.GetGraphicalUiElementByName("DividerInstance4") as DividerHorizontalRuntime;
            ResolutionLabel = this.GetGraphicalUiElementByName("ResolutionLabel") as LabelRuntime;
            ResolutionBox = this.GetGraphicalUiElementByName("ResolutionBox") as ListBoxRuntime;
            FullScreenCheckbox = this.GetGraphicalUiElementByName("FullScreenCheckbox") as CheckBoxRuntime;
            DetectResolutionsButton = this.GetGraphicalUiElementByName("DetectResolutionsButton") as ButtonStandardRuntime;
            ContainerInstance3 = this.GetGraphicalUiElementByName("ContainerInstance3") as ContainerRuntime;
            DividerInstance1 = this.GetGraphicalUiElementByName("DividerInstance1") as DividerHorizontalRuntime;
            MusicLabel = this.GetGraphicalUiElementByName("MusicLabel") as LabelRuntime;
            MusicSlider = this.GetGraphicalUiElementByName("MusicSlider") as SliderRuntime;
            SoundLabel = this.GetGraphicalUiElementByName("SoundLabel") as LabelRuntime;
            SoundSlider = this.GetGraphicalUiElementByName("SoundSlider") as SliderRuntime;
            DividerInstance2 = this.GetGraphicalUiElementByName("DividerInstance2") as DividerHorizontalRuntime;
            ControlLabel = this.GetGraphicalUiElementByName("ControlLabel") as LabelRuntime;
            KeyboardRadioButton = this.GetGraphicalUiElementByName("KeyboardRadioButton") as RadioButtonRuntime;
            GamepadRadioButton = this.GetGraphicalUiElementByName("GamepadRadioButton") as RadioButtonRuntime;
            TouchScreenRadioButton = this.GetGraphicalUiElementByName("TouchScreenRadioButton") as RadioButtonRuntime;
            DividerInstance3 = this.GetGraphicalUiElementByName("DividerInstance3") as DividerHorizontalRuntime;
            DifficultyLabel = this.GetGraphicalUiElementByName("DifficultyLabel") as LabelRuntime;
            Background1 = this.GetGraphicalUiElementByName("Background1") as NineSliceRuntime;
            ComboBoxInstance = this.GetGraphicalUiElementByName("ComboBoxInstance") as ComboBoxRuntime;
            ButtonContainer = this.GetGraphicalUiElementByName("ButtonContainer") as ContainerRuntime;
            BindingCheckbox = this.GetGraphicalUiElementByName("BindingCheckbox") as CheckBoxRuntime;
            BindingButton = this.GetGraphicalUiElementByName("BindingButton") as ButtonStandardRuntime;
            ButtonDenyInstance = this.GetGraphicalUiElementByName("ButtonDenyInstance") as ButtonDenyRuntime;
            ButtonConfirmInstance = this.GetGraphicalUiElementByName("ButtonConfirmInstance") as ButtonConfirmRuntime;
            DemoDialog = this.GetGraphicalUiElementByName("DemoDialog") as ContainerRuntime;
            MarginContainer = this.GetGraphicalUiElementByName("MarginContainer") as ContainerRuntime;
            LabelInstance = this.GetGraphicalUiElementByName("LabelInstance") as LabelRuntime;
            TextBoxInstance = this.GetGraphicalUiElementByName("TextBoxInstance") as TextBoxRuntime;
            TextBoxInstance1 = this.GetGraphicalUiElementByName("TextBoxInstance1") as PasswordBoxRuntime;
            ButtonWithNoEvents = this.GetGraphicalUiElementByName("ButtonWithNoEvents") as ButtonConfirmRuntime;
            ButtonConfirmInstance1 = this.GetGraphicalUiElementByName("ButtonConfirmInstance1") as ButtonConfirmRuntime;
            DemoHud = this.GetGraphicalUiElementByName("DemoHud") as NineSliceRuntime;
            TitleText2 = this.GetGraphicalUiElementByName("TitleText2") as TextRuntime;
            MenuTitle2 = this.GetGraphicalUiElementByName("MenuTitle2") as ContainerRuntime;
            PercentBarInstance = this.GetGraphicalUiElementByName("PercentBarInstance") as PercentBarRuntime;
            HitpointsBar1 = this.GetGraphicalUiElementByName("HitpointsBar1") as PercentBarRuntime;
            HitpointsBar2 = this.GetGraphicalUiElementByName("HitpointsBar2") as PercentBarRuntime;
            HitpointsBar3 = this.GetGraphicalUiElementByName("HitpointsBar3") as PercentBarRuntime;
            PercentBarInstance1 = this.GetGraphicalUiElementByName("PercentBarInstance1") as PercentBarRuntime;
            PercentBarInstance2 = this.GetGraphicalUiElementByName("PercentBarInstance2") as PercentBarRuntime;
            DividerInstance5 = this.GetGraphicalUiElementByName("DividerInstance5") as DividerHorizontalRuntime;
            ButtonCloseInstance2 = this.GetGraphicalUiElementByName("ButtonCloseInstance2") as ButtonCloseRuntime;
            PercentBarInstance3 = this.GetGraphicalUiElementByName("PercentBarInstance3") as PercentBarRuntime;
            PercentBarInstance4 = this.GetGraphicalUiElementByName("PercentBarInstance4") as PercentBarRuntime;
            PercentBarInstance5 = this.GetGraphicalUiElementByName("PercentBarInstance5") as PercentBarIconRuntime;
            ContainerInstance = this.GetGraphicalUiElementByName("ContainerInstance") as ContainerRuntime;
            DemoTabbedMenu = this.GetGraphicalUiElementByName("DemoTabbedMenu") as ContainerRuntime;
            ShowToastButton = this.GetGraphicalUiElementByName("ShowToastButton") as ButtonStandardRuntime;
            TabMenuBackground = this.GetGraphicalUiElementByName("TabMenuBackground") as NineSliceRuntime;
            TabMarginContainer = this.GetGraphicalUiElementByName("TabMarginContainer") as ContainerRuntime;
            Tab1 = this.GetGraphicalUiElementByName("Tab1") as ButtonTabRuntime;
            Tab2 = this.GetGraphicalUiElementByName("Tab2") as ButtonTabRuntime;
            Tab3 = this.GetGraphicalUiElementByName("Tab3") as ButtonTabRuntime;
            TabMenuClose = this.GetGraphicalUiElementByName("TabMenuClose") as ButtonCloseRuntime;
            TabHeaderDivider = this.GetGraphicalUiElementByName("TabHeaderDivider") as DividerHorizontalRuntime;
            ListContainer = this.GetGraphicalUiElementByName("ListContainer") as ContainerRuntime;
            PlayButton = this.GetGraphicalUiElementByName("PlayButton") as ButtonStandardIconRuntime;
            VideoSettingsButton = this.GetGraphicalUiElementByName("VideoSettingsButton") as ButtonStandardIconRuntime;
            AudioSettingsButton = this.GetGraphicalUiElementByName("AudioSettingsButton") as ButtonStandardIconRuntime;
            CreditsButton = this.GetGraphicalUiElementByName("CreditsButton") as ButtonStandardIconRuntime;
            ExitButton = this.GetGraphicalUiElementByName("ExitButton") as ButtonStandardIconRuntime;
            ShowDialogButton = this.GetGraphicalUiElementByName("ShowDialogButton") as ButtonStandardRuntime;
            TreeViewInstance = this.GetGraphicalUiElementByName("TreeViewInstance") as TreeViewRuntime;
            DialogBoxInstance = this.GetGraphicalUiElementByName("DialogBoxInstance") as DialogBoxRuntime;
            ContainerInstance1 = this.GetGraphicalUiElementByName("ContainerInstance1") as ContainerRuntime;
            ContainerInstance2 = this.GetGraphicalUiElementByName("ContainerInstance2") as ContainerRuntime;
            MenuItemInstance = this.GetGraphicalUiElementByName("MenuItemInstance") as MenuItemRuntime;
            MenuItemInstance1 = this.GetGraphicalUiElementByName("MenuItemInstance1") as MenuItemRuntime;
            MenuItemInstance2 = this.GetGraphicalUiElementByName("MenuItemInstance2") as MenuItemRuntime;
            MenuItemInstance3 = this.GetGraphicalUiElementByName("MenuItemInstance3") as MenuItemRuntime;
            CustomInitialize();
        }
        //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
        partial void CustomInitialize();
    }
}
