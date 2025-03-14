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
            }

             

            InitializeInstances();

            ApplyDefaultVariables();
            AssignParents();
            if(tryCreateFormsObject)
            {
            }
            CustomInitialize();
        }
        protected virtual void InitializeInstances()
        {
            MenuInstance = new MenuRuntime();
            MenuInstance.Name = "MenuInstance";
            DemoSettingsMenu = new ContainerRuntime();
            DemoSettingsMenu.Name = "DemoSettingsMenu";
            Background = new NineSliceRuntime();
            Background.Name = "Background";
            MenuTitle = new ContainerRuntime();
            MenuTitle.Name = "MenuTitle";
            MenuTitle1 = new ContainerRuntime();
            MenuTitle1.Name = "MenuTitle1";
            MenuItems = new ContainerRuntime();
            MenuItems.Name = "MenuItems";
            TitleText = new TextRuntime();
            TitleText.Name = "TitleText";
            TitleText1 = new TextRuntime();
            TitleText1.Name = "TitleText1";
            ButtonCloseInstance = new ButtonCloseRuntime();
            ButtonCloseInstance.Name = "ButtonCloseInstance";
            ButtonCloseInstance1 = new ButtonCloseRuntime();
            ButtonCloseInstance1.Name = "ButtonCloseInstance1";
            DividerInstance = new DividerHorizontalRuntime();
            DividerInstance.Name = "DividerInstance";
            DividerInstance4 = new DividerHorizontalRuntime();
            DividerInstance4.Name = "DividerInstance4";
            ResolutionLabel = new LabelRuntime();
            ResolutionLabel.Name = "ResolutionLabel";
            ResolutionBox = new ListBoxRuntime();
            ResolutionBox.Name = "ResolutionBox";
            FullScreenCheckbox = new CheckBoxRuntime();
            FullScreenCheckbox.Name = "FullScreenCheckbox";
            DetectResolutionsButton = new ButtonStandardRuntime();
            DetectResolutionsButton.Name = "DetectResolutionsButton";
            ContainerInstance3 = new ContainerRuntime();
            ContainerInstance3.Name = "ContainerInstance3";
            DividerInstance1 = new DividerHorizontalRuntime();
            DividerInstance1.Name = "DividerInstance1";
            MusicLabel = new LabelRuntime();
            MusicLabel.Name = "MusicLabel";
            MusicSlider = new SliderRuntime();
            MusicSlider.Name = "MusicSlider";
            SoundLabel = new LabelRuntime();
            SoundLabel.Name = "SoundLabel";
            SoundSlider = new SliderRuntime();
            SoundSlider.Name = "SoundSlider";
            DividerInstance2 = new DividerHorizontalRuntime();
            DividerInstance2.Name = "DividerInstance2";
            ControlLabel = new LabelRuntime();
            ControlLabel.Name = "ControlLabel";
            KeyboardRadioButton = new RadioButtonRuntime();
            KeyboardRadioButton.Name = "KeyboardRadioButton";
            GamepadRadioButton = new RadioButtonRuntime();
            GamepadRadioButton.Name = "GamepadRadioButton";
            TouchScreenRadioButton = new RadioButtonRuntime();
            TouchScreenRadioButton.Name = "TouchScreenRadioButton";
            DividerInstance3 = new DividerHorizontalRuntime();
            DividerInstance3.Name = "DividerInstance3";
            DifficultyLabel = new LabelRuntime();
            DifficultyLabel.Name = "DifficultyLabel";
            Background1 = new NineSliceRuntime();
            Background1.Name = "Background1";
            ComboBoxInstance = new ComboBoxRuntime();
            ComboBoxInstance.Name = "ComboBoxInstance";
            ButtonContainer = new ContainerRuntime();
            ButtonContainer.Name = "ButtonContainer";
            BindingCheckbox = new CheckBoxRuntime();
            BindingCheckbox.Name = "BindingCheckbox";
            BindingButton = new ButtonStandardRuntime();
            BindingButton.Name = "BindingButton";
            ButtonDenyInstance = new ButtonDenyRuntime();
            ButtonDenyInstance.Name = "ButtonDenyInstance";
            ButtonConfirmInstance = new ButtonConfirmRuntime();
            ButtonConfirmInstance.Name = "ButtonConfirmInstance";
            DemoDialog = new ContainerRuntime();
            DemoDialog.Name = "DemoDialog";
            MarginContainer = new ContainerRuntime();
            MarginContainer.Name = "MarginContainer";
            LabelInstance = new LabelRuntime();
            LabelInstance.Name = "LabelInstance";
            TextBoxInstance = new TextBoxRuntime();
            TextBoxInstance.Name = "TextBoxInstance";
            TextBoxInstance1 = new PasswordBoxRuntime();
            TextBoxInstance1.Name = "TextBoxInstance1";
            ButtonWithNoEvents = new ButtonConfirmRuntime();
            ButtonWithNoEvents.Name = "ButtonWithNoEvents";
            ButtonConfirmInstance1 = new ButtonConfirmRuntime();
            ButtonConfirmInstance1.Name = "ButtonConfirmInstance1";
            DemoHud = new NineSliceRuntime();
            DemoHud.Name = "DemoHud";
            TitleText2 = new TextRuntime();
            TitleText2.Name = "TitleText2";
            MenuTitle2 = new ContainerRuntime();
            MenuTitle2.Name = "MenuTitle2";
            PercentBarInstance = new PercentBarRuntime();
            PercentBarInstance.Name = "PercentBarInstance";
            HitpointsBar1 = new PercentBarRuntime();
            HitpointsBar1.Name = "HitpointsBar1";
            HitpointsBar2 = new PercentBarRuntime();
            HitpointsBar2.Name = "HitpointsBar2";
            HitpointsBar3 = new PercentBarRuntime();
            HitpointsBar3.Name = "HitpointsBar3";
            PercentBarInstance1 = new PercentBarRuntime();
            PercentBarInstance1.Name = "PercentBarInstance1";
            PercentBarInstance2 = new PercentBarRuntime();
            PercentBarInstance2.Name = "PercentBarInstance2";
            DividerInstance5 = new DividerHorizontalRuntime();
            DividerInstance5.Name = "DividerInstance5";
            ButtonCloseInstance2 = new ButtonCloseRuntime();
            ButtonCloseInstance2.Name = "ButtonCloseInstance2";
            PercentBarInstance3 = new PercentBarRuntime();
            PercentBarInstance3.Name = "PercentBarInstance3";
            PercentBarInstance4 = new PercentBarRuntime();
            PercentBarInstance4.Name = "PercentBarInstance4";
            PercentBarInstance5 = new PercentBarIconRuntime();
            PercentBarInstance5.Name = "PercentBarInstance5";
            ContainerInstance = new ContainerRuntime();
            ContainerInstance.Name = "ContainerInstance";
            DemoTabbedMenu = new ContainerRuntime();
            DemoTabbedMenu.Name = "DemoTabbedMenu";
            ShowToastButton = new ButtonStandardRuntime();
            ShowToastButton.Name = "ShowToastButton";
            TabMenuBackground = new NineSliceRuntime();
            TabMenuBackground.Name = "TabMenuBackground";
            TabMarginContainer = new ContainerRuntime();
            TabMarginContainer.Name = "TabMarginContainer";
            Tab1 = new ButtonTabRuntime();
            Tab1.Name = "Tab1";
            Tab2 = new ButtonTabRuntime();
            Tab2.Name = "Tab2";
            Tab3 = new ButtonTabRuntime();
            Tab3.Name = "Tab3";
            TabMenuClose = new ButtonCloseRuntime();
            TabMenuClose.Name = "TabMenuClose";
            TabHeaderDivider = new DividerHorizontalRuntime();
            TabHeaderDivider.Name = "TabHeaderDivider";
            ListContainer = new ContainerRuntime();
            ListContainer.Name = "ListContainer";
            PlayButton = new ButtonStandardIconRuntime();
            PlayButton.Name = "PlayButton";
            VideoSettingsButton = new ButtonStandardIconRuntime();
            VideoSettingsButton.Name = "VideoSettingsButton";
            AudioSettingsButton = new ButtonStandardIconRuntime();
            AudioSettingsButton.Name = "AudioSettingsButton";
            CreditsButton = new ButtonStandardIconRuntime();
            CreditsButton.Name = "CreditsButton";
            ExitButton = new ButtonStandardIconRuntime();
            ExitButton.Name = "ExitButton";
            ShowDialogButton = new ButtonStandardRuntime();
            ShowDialogButton.Name = "ShowDialogButton";
            TreeViewInstance = new TreeViewRuntime();
            TreeViewInstance.Name = "TreeViewInstance";
            DialogBoxInstance = new DialogBoxRuntime();
            DialogBoxInstance.Name = "DialogBoxInstance";
            ContainerInstance1 = new ContainerRuntime();
            ContainerInstance1.Name = "ContainerInstance1";
            ContainerInstance2 = new ContainerRuntime();
            ContainerInstance2.Name = "ContainerInstance2";
            MenuItemInstance = new MenuItemRuntime();
            MenuItemInstance.Name = "MenuItemInstance";
            MenuItemInstance1 = new MenuItemRuntime();
            MenuItemInstance1.Name = "MenuItemInstance1";
            MenuItemInstance2 = new MenuItemRuntime();
            MenuItemInstance2.Name = "MenuItemInstance2";
            MenuItemInstance3 = new MenuItemRuntime();
            MenuItemInstance3.Name = "MenuItemInstance3";
        }
        protected virtual void AssignParents()
        {
            if(this.Children != null) this.Children.Add(MenuInstance);
            else this.WhatThisContains.Add(MenuInstance);
            if(this.Children != null) this.Children.Add(DemoSettingsMenu);
            else this.WhatThisContains.Add(DemoSettingsMenu);
            DemoSettingsMenu.Children.Add(Background);
            MenuItems.Children.Add(MenuTitle);
            MarginContainer.Children.Add(MenuTitle1);
            DemoSettingsMenu.Children.Add(MenuItems);
            MenuTitle.Children.Add(TitleText);
            MenuTitle1.Children.Add(TitleText1);
            MenuTitle.Children.Add(ButtonCloseInstance);
            MenuTitle1.Children.Add(ButtonCloseInstance1);
            MenuTitle.Children.Add(DividerInstance);
            MenuTitle1.Children.Add(DividerInstance4);
            MenuItems.Children.Add(ResolutionLabel);
            MenuItems.Children.Add(ResolutionBox);
            ContainerInstance3.Children.Add(FullScreenCheckbox);
            ContainerInstance3.Children.Add(DetectResolutionsButton);
            MenuItems.Children.Add(ContainerInstance3);
            MenuItems.Children.Add(DividerInstance1);
            MenuItems.Children.Add(MusicLabel);
            MenuItems.Children.Add(MusicSlider);
            MenuItems.Children.Add(SoundLabel);
            MenuItems.Children.Add(SoundSlider);
            MenuItems.Children.Add(DividerInstance2);
            MenuItems.Children.Add(ControlLabel);
            MenuItems.Children.Add(KeyboardRadioButton);
            MenuItems.Children.Add(GamepadRadioButton);
            MenuItems.Children.Add(TouchScreenRadioButton);
            MenuItems.Children.Add(DividerInstance3);
            MenuItems.Children.Add(DifficultyLabel);
            DemoDialog.Children.Add(Background1);
            MenuItems.Children.Add(ComboBoxInstance);
            MenuItems.Children.Add(ButtonContainer);
            ContainerInstance2.Children.Add(BindingCheckbox);
            ContainerInstance2.Children.Add(BindingButton);
            ButtonContainer.Children.Add(ButtonDenyInstance);
            ButtonContainer.Children.Add(ButtonConfirmInstance);
            if(this.Children != null) this.Children.Add(DemoDialog);
            else this.WhatThisContains.Add(DemoDialog);
            DemoDialog.Children.Add(MarginContainer);
            MarginContainer.Children.Add(LabelInstance);
            MarginContainer.Children.Add(TextBoxInstance);
            MarginContainer.Children.Add(TextBoxInstance1);
            ContainerInstance1.Children.Add(ButtonWithNoEvents);
            ContainerInstance1.Children.Add(ButtonConfirmInstance1);
            if(this.Children != null) this.Children.Add(DemoHud);
            else this.WhatThisContains.Add(DemoHud);
            MenuTitle2.Children.Add(TitleText2);
            ContainerInstance.Children.Add(MenuTitle2);
            ContainerInstance.Children.Add(PercentBarInstance);
            DemoHud.Children.Add(HitpointsBar1);
            DemoHud.Children.Add(HitpointsBar2);
            DemoHud.Children.Add(HitpointsBar3);
            ContainerInstance.Children.Add(PercentBarInstance1);
            ContainerInstance.Children.Add(PercentBarInstance2);
            MenuTitle2.Children.Add(DividerInstance5);
            MenuTitle2.Children.Add(ButtonCloseInstance2);
            ContainerInstance.Children.Add(PercentBarInstance3);
            ContainerInstance.Children.Add(PercentBarInstance4);
            ContainerInstance.Children.Add(PercentBarInstance5);
            DemoHud.Children.Add(ContainerInstance);
            if(this.Children != null) this.Children.Add(DemoTabbedMenu);
            else this.WhatThisContains.Add(DemoTabbedMenu);
            if(this.Children != null) this.Children.Add(ShowToastButton);
            else this.WhatThisContains.Add(ShowToastButton);
            DemoTabbedMenu.Children.Add(TabMenuBackground);
            DemoTabbedMenu.Children.Add(TabMarginContainer);
            TabMarginContainer.Children.Add(Tab1);
            TabMarginContainer.Children.Add(Tab2);
            TabMarginContainer.Children.Add(Tab3);
            TabMarginContainer.Children.Add(TabMenuClose);
            TabMarginContainer.Children.Add(TabHeaderDivider);
            TabMarginContainer.Children.Add(ListContainer);
            ListContainer.Children.Add(PlayButton);
            ListContainer.Children.Add(VideoSettingsButton);
            ListContainer.Children.Add(AudioSettingsButton);
            ListContainer.Children.Add(CreditsButton);
            ListContainer.Children.Add(ExitButton);
            if(this.Children != null) this.Children.Add(ShowDialogButton);
            else this.WhatThisContains.Add(ShowDialogButton);
            if(this.Children != null) this.Children.Add(TreeViewInstance);
            else this.WhatThisContains.Add(TreeViewInstance);
            if(this.Children != null) this.Children.Add(DialogBoxInstance);
            else this.WhatThisContains.Add(DialogBoxInstance);
            MarginContainer.Children.Add(ContainerInstance1);
            MenuItems.Children.Add(ContainerInstance2);
            MenuInstance.InnerPanelInstance.Children.Add(MenuItemInstance);
            MenuInstance.InnerPanelInstance.Children.Add(MenuItemInstance1);
            MenuInstance.InnerPanelInstance.Children.Add(MenuItemInstance2);
            MenuInstance.InnerPanelInstance.Children.Add(MenuItemInstance3);
        }
        private void ApplyDefaultVariables()
        {
            this.MenuInstance.Height = 32f;

            this.DemoSettingsMenu.Height = 16f;
            this.DemoSettingsMenu.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.DemoSettingsMenu.Width = 400f;
            this.DemoSettingsMenu.X = 15f;
            this.DemoSettingsMenu.Y = 42f;

Background.SetProperty("ColorCategoryState", "Primary");
Background.SetProperty("StyleCategoryState", "Panel");
            this.Background.Height = 0f;
            this.Background.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.Background.Width = 0f;
            this.Background.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.Background.X = 0f;
            this.Background.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.Background.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.Background.Y = 0f;
            this.Background.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.Background.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.MenuTitle.Height = 8f;
            this.MenuTitle.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.MenuTitle.Width = 0f;
            this.MenuTitle.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;

            this.MenuTitle1.Height = 8f;
            this.MenuTitle1.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.MenuTitle1.Width = 0f;
            this.MenuTitle1.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;

            this.MenuItems.ChildrenLayout = global::Gum.Managers.ChildrenLayout.TopToBottomStack;
            this.MenuItems.Height = 0f;
            this.MenuItems.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.MenuItems.Width = -16f;
            this.MenuItems.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.MenuItems.X = 0f;
            this.MenuItems.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.MenuItems.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.MenuItems.Y = 0f;
            this.MenuItems.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.MenuItems.YUnits = GeneralUnitType.PixelsFromMiddle;

TitleText.SetProperty("ColorCategoryState", "Primary");
TitleText.SetProperty("StyleCategoryState", "Title");
            this.TitleText.Text = @"Settings";

TitleText1.SetProperty("ColorCategoryState", "Primary");
TitleText1.SetProperty("StyleCategoryState", "Title");
            this.TitleText1.Text = @"New Profile";

            this.ButtonCloseInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
            this.ButtonCloseInstance.XUnits = GeneralUnitType.PixelsFromLarge;

            this.ButtonCloseInstance1.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
            this.ButtonCloseInstance1.XUnits = GeneralUnitType.PixelsFromLarge;

            this.DividerInstance.Width = 0f;
            this.DividerInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.DividerInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.DividerInstance.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.DividerInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Bottom;
            this.DividerInstance.YUnits = GeneralUnitType.PixelsFromLarge;

            this.DividerInstance4.Width = 0f;
            this.DividerInstance4.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.DividerInstance4.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.DividerInstance4.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.DividerInstance4.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Bottom;
            this.DividerInstance4.YUnits = GeneralUnitType.PixelsFromLarge;

            this.ResolutionLabel.LabelText = @"Resolution";

            this.ResolutionBox.Height = 128f;
            this.ResolutionBox.Width = 0f;
            this.ResolutionBox.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;

            this.FullScreenCheckbox.CheckboxDisplayText = @"Run Fullscreen";
            this.FullScreenCheckbox.X = 0f;
            this.FullScreenCheckbox.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            this.FullScreenCheckbox.XUnits = GeneralUnitType.PixelsFromSmall;
            this.FullScreenCheckbox.Y = 0f;
            this.FullScreenCheckbox.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            this.FullScreenCheckbox.YUnits = GeneralUnitType.PixelsFromSmall;

            this.DetectResolutionsButton.ButtonDisplayText = @"Detect Resolutions";
            this.DetectResolutionsButton.Height = 24f;
            this.DetectResolutionsButton.Width = 156f;
            this.DetectResolutionsButton.X = 0f;
            this.DetectResolutionsButton.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
            this.DetectResolutionsButton.XUnits = GeneralUnitType.PixelsFromLarge;
            this.DetectResolutionsButton.Y = 0f;
            this.DetectResolutionsButton.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            this.DetectResolutionsButton.YUnits = GeneralUnitType.PixelsFromSmall;

            this.ContainerInstance3.Height = 0f;
            this.ContainerInstance3.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.ContainerInstance3.Width = 0f;
            this.ContainerInstance3.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.ContainerInstance3.X = 0f;
            this.ContainerInstance3.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.ContainerInstance3.XUnits = GeneralUnitType.PixelsFromMiddle;

            this.DividerInstance1.Width = 0f;
            this.DividerInstance1.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.DividerInstance1.Y = 8f;

            this.MusicLabel.LabelText = @"Music Volume";
            this.MusicLabel.Y = 8f;

            this.MusicSlider.Width = 0f;
            this.MusicSlider.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;

            this.SoundLabel.LabelText = @"Sound Volume";

            this.SoundSlider.Width = 0f;
            this.SoundSlider.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;

            this.DividerInstance2.Width = 0f;
            this.DividerInstance2.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.DividerInstance2.Y = 8f;

            this.ControlLabel.LabelText = @"Control Scheme";
            this.ControlLabel.Y = 8f;

this.KeyboardRadioButton.RadioButtonCategoryState = RadioButtonRuntime.RadioButtonCategory.EnabledOff;
            this.KeyboardRadioButton.RadioDisplayText = @"Keyboard & Mouse";
            this.KeyboardRadioButton.Width = 0f;
            this.KeyboardRadioButton.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;

this.GamepadRadioButton.RadioButtonCategoryState = RadioButtonRuntime.RadioButtonCategory.EnabledOn;
            this.GamepadRadioButton.RadioDisplayText = @"Gamepad";
            this.GamepadRadioButton.Width = 0f;
            this.GamepadRadioButton.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.GamepadRadioButton.Y = 4f;

this.TouchScreenRadioButton.RadioButtonCategoryState = RadioButtonRuntime.RadioButtonCategory.EnabledOff;
            this.TouchScreenRadioButton.RadioDisplayText = @"Touchscreen";
            this.TouchScreenRadioButton.Width = 0f;
            this.TouchScreenRadioButton.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.TouchScreenRadioButton.Y = 4f;

            this.DividerInstance3.Width = 0f;
            this.DividerInstance3.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.DividerInstance3.Y = 8f;

            this.DifficultyLabel.LabelText = @"Difficulty";
            this.DifficultyLabel.Y = 8f;

Background1.SetProperty("ColorCategoryState", "Primary");
Background1.SetProperty("StyleCategoryState", "Panel");
            this.Background1.Height = 0f;
            this.Background1.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.Background1.Width = 0f;
            this.Background1.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.Background1.X = 0f;
            this.Background1.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.Background1.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.Background1.Y = 0f;
            this.Background1.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.Background1.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.ComboBoxInstance.Width = 0f;
            this.ComboBoxInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;

            this.ButtonContainer.Height = 32f;
            this.ButtonContainer.Width = 0f;
            this.ButtonContainer.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.ButtonContainer.Y = 16f;

            this.BindingCheckbox.CheckboxDisplayText = @"Is Button Enabled";
            this.BindingCheckbox.Width = 188f;
            this.BindingCheckbox.X = 0f;
            this.BindingCheckbox.Y = 0f;

            this.BindingButton.ButtonDisplayText = @"Enabled/Disabled Button";
            this.BindingButton.Height = 24f;
            this.BindingButton.Width = 187f;
            this.BindingButton.X = 0f;
            this.BindingButton.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
            this.BindingButton.XUnits = GeneralUnitType.PixelsFromLarge;
            this.BindingButton.Y = 0f;
            this.BindingButton.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            this.BindingButton.YUnits = GeneralUnitType.PixelsFromSmall;


            this.ButtonConfirmInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
            this.ButtonConfirmInstance.XUnits = GeneralUnitType.PixelsFromLarge;

            this.DemoDialog.Height = 199f;
            this.DemoDialog.Width = 349f;
            this.DemoDialog.X = 425f;
            this.DemoDialog.Y = 43f;

            this.MarginContainer.ChildrenLayout = global::Gum.Managers.ChildrenLayout.TopToBottomStack;
            this.MarginContainer.Height = -16f;
            this.MarginContainer.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.MarginContainer.Width = -16f;
            this.MarginContainer.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.MarginContainer.X = 0f;
            this.MarginContainer.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.MarginContainer.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.MarginContainer.Y = 0f;
            this.MarginContainer.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.MarginContainer.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.LabelInstance.LabelText = @"New Profile Name";
            this.LabelInstance.Y = 8f;

            this.TextBoxInstance.Width = 0f;
            this.TextBoxInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;

            this.TextBoxInstance1.Width = 0f;
            this.TextBoxInstance1.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.TextBoxInstance1.X = 0f;
            this.TextBoxInstance1.Y = 7f;

            this.ButtonWithNoEvents.ButtonDisplayText = @"No-Event Button";
            this.ButtonWithNoEvents.HasEvents = false;
            this.ButtonWithNoEvents.X = 0f;
            this.ButtonWithNoEvents.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            this.ButtonWithNoEvents.XUnits = GeneralUnitType.PixelsFromSmall;
            this.ButtonWithNoEvents.Y = 0f;
            this.ButtonWithNoEvents.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            this.ButtonWithNoEvents.YUnits = GeneralUnitType.PixelsFromSmall;

            this.ButtonConfirmInstance1.X = 0f;
            this.ButtonConfirmInstance1.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
            this.ButtonConfirmInstance1.XUnits = GeneralUnitType.PixelsFromLarge;
            this.ButtonConfirmInstance1.Y = 0f;
            this.ButtonConfirmInstance1.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            this.ButtonConfirmInstance1.YUnits = GeneralUnitType.PixelsFromSmall;

DemoHud.SetProperty("ColorCategoryState", "Primary");
DemoHud.SetProperty("StyleCategoryState", "Panel");
            this.DemoHud.Height = 207f;
            this.DemoHud.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            this.DemoHud.Width = 279f;
            this.DemoHud.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            this.DemoHud.X = 427f;
            this.DemoHud.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            this.DemoHud.XUnits = GeneralUnitType.PixelsFromSmall;
            this.DemoHud.Y = 249f;
            this.DemoHud.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            this.DemoHud.YUnits = GeneralUnitType.PixelsFromSmall;

TitleText2.SetProperty("ColorCategoryState", "Primary");
TitleText2.SetProperty("StyleCategoryState", "Title");
            this.TitleText2.Text = @"HUD Demo";

            this.MenuTitle2.Height = 8f;
            this.MenuTitle2.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.MenuTitle2.Width = 0f;
            this.MenuTitle2.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;

            this.PercentBarInstance.Width = 0f;
            this.PercentBarInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.PercentBarInstance.Y = 8f;

HitpointsBar1.SetProperty("BarColor", "Success");
this.HitpointsBar1.BarDecorCategoryState = PercentBarRuntime.BarDecorCategory.VerticalLines;
            this.HitpointsBar1.BarPercent = 75f;
            this.HitpointsBar1.Height = 8f;
            this.HitpointsBar1.Width = 24f;
            this.HitpointsBar1.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            this.HitpointsBar1.X = 184f;
            this.HitpointsBar1.Y = 10f;

HitpointsBar2.SetProperty("BarColor", "Warning");
this.HitpointsBar2.BarDecorCategoryState = PercentBarRuntime.BarDecorCategory.VerticalLines;
            this.HitpointsBar2.BarPercent = 50f;
            this.HitpointsBar2.Height = 8f;
            this.HitpointsBar2.Width = 24f;
            this.HitpointsBar2.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            this.HitpointsBar2.X = 184f;
            this.HitpointsBar2.Y = 20f;

HitpointsBar3.SetProperty("BarColor", "Danger");
this.HitpointsBar3.BarDecorCategoryState = PercentBarRuntime.BarDecorCategory.VerticalLines;
            this.HitpointsBar3.BarPercent = 25f;
            this.HitpointsBar3.Height = 8f;
            this.HitpointsBar3.Width = 24f;
            this.HitpointsBar3.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            this.HitpointsBar3.X = 184f;
            this.HitpointsBar3.Y = 31f;

PercentBarInstance1.SetProperty("BarColor", "Success");
this.PercentBarInstance1.BarDecorCategoryState = PercentBarRuntime.BarDecorCategory.CautionLines;
            this.PercentBarInstance1.Width = 0f;
            this.PercentBarInstance1.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.PercentBarInstance1.Y = 8f;

PercentBarInstance2.SetProperty("BarColor", "Warning");
this.PercentBarInstance2.BarDecorCategoryState = PercentBarRuntime.BarDecorCategory.VerticalLines;
            this.PercentBarInstance2.Width = 0f;
            this.PercentBarInstance2.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.PercentBarInstance2.Y = 8f;

            this.DividerInstance5.Width = 0f;
            this.DividerInstance5.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.DividerInstance5.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.DividerInstance5.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.DividerInstance5.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Bottom;
            this.DividerInstance5.YUnits = GeneralUnitType.PixelsFromLarge;

            this.ButtonCloseInstance2.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
            this.ButtonCloseInstance2.XUnits = GeneralUnitType.PixelsFromLarge;

PercentBarInstance3.SetProperty("BarColor", "Danger");
            this.PercentBarInstance3.Width = 0f;
            this.PercentBarInstance3.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.PercentBarInstance3.Y = 8f;

PercentBarInstance4.SetProperty("BarColor", "Accent");
this.PercentBarInstance4.BarDecorCategoryState = PercentBarRuntime.BarDecorCategory.CautionLines;
            this.PercentBarInstance4.Width = 0f;
            this.PercentBarInstance4.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.PercentBarInstance4.Y = 8f;

PercentBarInstance5.SetProperty("BarColor", "Danger");
            this.PercentBarInstance5.Width = 0f;
            this.PercentBarInstance5.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.PercentBarInstance5.Y = 8f;

            this.ContainerInstance.ChildrenLayout = global::Gum.Managers.ChildrenLayout.TopToBottomStack;
            this.ContainerInstance.Height = -16f;
            this.ContainerInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.ContainerInstance.Width = -16f;
            this.ContainerInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.ContainerInstance.X = 0f;
            this.ContainerInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.ContainerInstance.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.ContainerInstance.Y = 0f;
            this.ContainerInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.ContainerInstance.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.DemoTabbedMenu.Height = 256f;
            this.DemoTabbedMenu.Width = 467f;
            this.DemoTabbedMenu.X = 426f;
            this.DemoTabbedMenu.Y = 462f;

            this.ShowToastButton.ButtonDisplayText = @"Show Toast";
            this.ShowToastButton.Height = 24f;
            this.ShowToastButton.X = 788f;
            this.ShowToastButton.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            this.ShowToastButton.XUnits = GeneralUnitType.PixelsFromSmall;
            this.ShowToastButton.Y = 44f;
            this.ShowToastButton.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            this.ShowToastButton.YUnits = GeneralUnitType.PixelsFromSmall;

TabMenuBackground.SetProperty("ColorCategoryState", "Primary");
TabMenuBackground.SetProperty("StyleCategoryState", "Panel");

            this.TabMarginContainer.Height = -16f;
            this.TabMarginContainer.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.TabMarginContainer.Width = -16f;
            this.TabMarginContainer.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.TabMarginContainer.X = 0f;
            this.TabMarginContainer.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.TabMarginContainer.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.TabMarginContainer.Y = 0f;
            this.TabMarginContainer.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.TabMarginContainer.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.Tab1.TabDisplayText = @"Settings";
            this.Tab1.X = 10f;
            this.Tab1.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            this.Tab1.XUnits = GeneralUnitType.PixelsFromSmall;
            this.Tab1.Y = 36f;

            this.Tab2.TabDisplayText = @"Tab 2";
            this.Tab2.X = 71f;
            this.Tab2.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            this.Tab2.XUnits = GeneralUnitType.PixelsFromSmall;
            this.Tab2.Y = 36f;

            this.Tab3.TabDisplayText = @"Tab3";
            this.Tab3.X = 117f;
            this.Tab3.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            this.Tab3.XUnits = GeneralUnitType.PixelsFromSmall;
            this.Tab3.Y = 36f;

            this.TabMenuClose.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
            this.TabMenuClose.XUnits = GeneralUnitType.PixelsFromLarge;

            this.TabHeaderDivider.Width = 0f;
            this.TabHeaderDivider.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.TabHeaderDivider.X = 0f;
            this.TabHeaderDivider.Y = 35f;

            this.ListContainer.ChildrenLayout = global::Gum.Managers.ChildrenLayout.TopToBottomStack;
            this.ListContainer.Height = -45f;
            this.ListContainer.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.ListContainer.Width = 0f;
            this.ListContainer.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.ListContainer.X = 0f;
            this.ListContainer.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.ListContainer.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.ListContainer.Y = 0f;
            this.ListContainer.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Bottom;
            this.ListContainer.YUnits = GeneralUnitType.PixelsFromLarge;

this.PlayButton.ButtonIcon = IconRuntime.IconCategory.Play;
            this.PlayButton.ButtonDisplayText = @"Start Game";
            this.PlayButton.Width = 0f;
            this.PlayButton.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;

this.VideoSettingsButton.ButtonIcon = IconRuntime.IconCategory.Monitor;
            this.VideoSettingsButton.ButtonDisplayText = @"Video Settings";
            this.VideoSettingsButton.Width = 0f;
            this.VideoSettingsButton.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.VideoSettingsButton.Y = 8f;

this.AudioSettingsButton.ButtonIcon = IconRuntime.IconCategory.Music;
            this.AudioSettingsButton.ButtonDisplayText = @"Audio Settings";
            this.AudioSettingsButton.Width = 0f;
            this.AudioSettingsButton.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.AudioSettingsButton.Y = 8f;

this.CreditsButton.ButtonIcon = IconRuntime.IconCategory.Info;
            this.CreditsButton.ButtonDisplayText = @"Credits";
            this.CreditsButton.Width = 0f;
            this.CreditsButton.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.CreditsButton.Y = 8f;

this.ExitButton.ButtonIcon = IconRuntime.IconCategory.Power;
            this.ExitButton.ButtonDisplayText = @"Exit";
            this.ExitButton.Width = 0f;
            this.ExitButton.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.ExitButton.Y = 8f;

            this.ShowDialogButton.ButtonDisplayText = @"Show Dialog Box";
            this.ShowDialogButton.Height = 24f;
            this.ShowDialogButton.X = 722f;
            this.ShowDialogButton.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            this.ShowDialogButton.XUnits = GeneralUnitType.PixelsFromSmall;
            this.ShowDialogButton.Y = 250f;
            this.ShowDialogButton.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            this.ShowDialogButton.YUnits = GeneralUnitType.PixelsFromSmall;

            this.TreeViewInstance.Height = 155f;
            this.TreeViewInstance.Width = 227f;
            this.TreeViewInstance.X = 783f;
            this.TreeViewInstance.Y = 78f;

            this.DialogBoxInstance.X = 722f;
            this.DialogBoxInstance.Y = 278f;

            this.ContainerInstance1.Height = 40f;
            this.ContainerInstance1.Width = 0f;
            this.ContainerInstance1.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.ContainerInstance1.X = 0f;
            this.ContainerInstance1.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.ContainerInstance1.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.ContainerInstance1.Y = 10f;

            this.ContainerInstance2.ChildrenLayout = global::Gum.Managers.ChildrenLayout.Regular;
            this.ContainerInstance2.Height = 0f;
            this.ContainerInstance2.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.ContainerInstance2.Width = 0f;
            this.ContainerInstance2.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.ContainerInstance2.X = 0f;
            this.ContainerInstance2.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.ContainerInstance2.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.ContainerInstance2.Y = 8f;

            this.MenuItemInstance.Header = @"File";
            this.MenuItemInstance.X = 0f;
            this.MenuItemInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            this.MenuItemInstance.XUnits = GeneralUnitType.PixelsFromSmall;
            this.MenuItemInstance.Y = 0f;
            this.MenuItemInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.MenuItemInstance.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.MenuItemInstance1.Header = @"Edit";
            this.MenuItemInstance1.X = 0f;
            this.MenuItemInstance1.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            this.MenuItemInstance1.XUnits = GeneralUnitType.PixelsFromSmall;
            this.MenuItemInstance1.Y = 0f;
            this.MenuItemInstance1.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.MenuItemInstance1.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.MenuItemInstance2.Header = @"View";
            this.MenuItemInstance2.X = 0f;
            this.MenuItemInstance2.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            this.MenuItemInstance2.XUnits = GeneralUnitType.PixelsFromSmall;
            this.MenuItemInstance2.Y = 0f;
            this.MenuItemInstance2.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.MenuItemInstance2.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.MenuItemInstance3.Header = @"Help";
            this.MenuItemInstance3.X = 0f;
            this.MenuItemInstance3.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            this.MenuItemInstance3.XUnits = GeneralUnitType.PixelsFromSmall;
            this.MenuItemInstance3.Y = 0f;
            this.MenuItemInstance3.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.MenuItemInstance3.YUnits = GeneralUnitType.PixelsFromMiddle;

        }
        partial void CustomInitialize();
    }
}
