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
            TextStyleContainer = new ContainerRuntime();
            TextStyleContainer.Name = "TextStyleContainer";
            TextTitle = new TextRuntime();
            TextTitle.Name = "TextTitle";
            TextH1 = new TextRuntime();
            TextH1.Name = "TextH1";
            TextH2 = new TextRuntime();
            TextH2.Name = "TextH2";
            TextH3 = new TextRuntime();
            TextH3.Name = "TextH3";
            TextNormal = new TextRuntime();
            TextNormal.Name = "TextNormal";
            TextStrong = new TextRuntime();
            TextStrong.Name = "TextStrong";
            TextEmphasis = new TextRuntime();
            TextEmphasis.Name = "TextEmphasis";
            TextSmall = new TextRuntime();
            TextSmall.Name = "TextSmall";
            TextTiny = new TextRuntime();
            TextTiny.Name = "TextTiny";
            Solid = new NineSliceRuntime();
            Solid.Name = "Solid";
            Bordered = new NineSliceRuntime();
            Bordered.Name = "Bordered";
            BracketHorizontal = new NineSliceRuntime();
            BracketHorizontal.Name = "BracketHorizontal";
            BracketVertical = new NineSliceRuntime();
            BracketVertical.Name = "BracketVertical";
            Tab = new NineSliceRuntime();
            Tab.Name = "Tab";
            TabBordered = new NineSliceRuntime();
            TabBordered.Name = "TabBordered";
            Outlined = new NineSliceRuntime();
            Outlined.Name = "Outlined";
            OutlinedHeavy = new NineSliceRuntime();
            OutlinedHeavy.Name = "OutlinedHeavy";
            Panel = new NineSliceRuntime();
            Panel.Name = "Panel";
            NineSliceStyleContainer = new ContainerRuntime();
            NineSliceStyleContainer.Name = "NineSliceStyleContainer";
            ButtonsContainer = new ContainerRuntime();
            ButtonsContainer.Name = "ButtonsContainer";
            ButtonStandardInstance = new ButtonStandardRuntime();
            ButtonStandardInstance.Name = "ButtonStandardInstance";
            ButtonStandardIconInstance = new ButtonStandardIconRuntime();
            ButtonStandardIconInstance.Name = "ButtonStandardIconInstance";
            ButtonTabInstance = new ButtonTabRuntime();
            ButtonTabInstance.Name = "ButtonTabInstance";
            ButtonIconInstance = new ButtonIconRuntime();
            ButtonIconInstance.Name = "ButtonIconInstance";
            ButtonConfirmInstance = new ButtonConfirmRuntime();
            ButtonConfirmInstance.Name = "ButtonConfirmInstance";
            ButtonDenyInstance = new ButtonDenyRuntime();
            ButtonDenyInstance.Name = "ButtonDenyInstance";
            ButtonCloseInstance = new ButtonCloseRuntime();
            ButtonCloseInstance.Name = "ButtonCloseInstance";
            ElementsContainer = new ContainerRuntime();
            ElementsContainer.Name = "ElementsContainer";
            PercentBarPrimary = new PercentBarRuntime();
            PercentBarPrimary.Name = "PercentBarPrimary";
            PercentBarLinesDecor = new PercentBarRuntime();
            PercentBarLinesDecor.Name = "PercentBarLinesDecor";
            PercentBarCautionDecor = new PercentBarRuntime();
            PercentBarCautionDecor.Name = "PercentBarCautionDecor";
            PercentBarIconPrimary = new PercentBarIconRuntime();
            PercentBarIconPrimary.Name = "PercentBarIconPrimary";
            PercentBarIconLinesDecor = new PercentBarIconRuntime();
            PercentBarIconLinesDecor.Name = "PercentBarIconLinesDecor";
            PercentBarIconCautionDecor = new PercentBarIconRuntime();
            PercentBarIconCautionDecor.Name = "PercentBarIconCautionDecor";
            ControlsContainer = new ContainerRuntime();
            ControlsContainer.Name = "ControlsContainer";
            LabelInstance = new LabelRuntime();
            LabelInstance.Name = "LabelInstance";
            CheckBoxInstance = new CheckBoxRuntime();
            CheckBoxInstance.Name = "CheckBoxInstance";
            RadioButtonInstance = new RadioButtonRuntime();
            RadioButtonInstance.Name = "RadioButtonInstance";
            ComboBoxInstance = new ComboBoxRuntime();
            ComboBoxInstance.Name = "ComboBoxInstance";
            ListBoxInstance = new ListBoxRuntime();
            ListBoxInstance.Name = "ListBoxInstance";
            SliderInstance = new SliderRuntime();
            SliderInstance.Name = "SliderInstance";
            TextBoxInstance = new TextBoxRuntime();
            TextBoxInstance.Name = "TextBoxInstance";
            PasswordBoxInstance = new PasswordBoxRuntime();
            PasswordBoxInstance.Name = "PasswordBoxInstance";
            DividerVerticalInstance = new DividerVerticalRuntime();
            DividerVerticalInstance.Name = "DividerVerticalInstance";
            DividerHorizontalInstance = new DividerHorizontalRuntime();
            DividerHorizontalInstance.Name = "DividerHorizontalInstance";
            IconInstance = new IconRuntime();
            IconInstance.Name = "IconInstance";
            CautionLinesInstance = new CautionLinesRuntime();
            CautionLinesInstance.Name = "CautionLinesInstance";
            VerticalLinesInstance = new VerticalLinesRuntime();
            VerticalLinesInstance.Name = "VerticalLinesInstance";
            ColorContainer = new ContainerRuntime();
            ColorContainer.Name = "ColorContainer";
            TextBlack = new TextRuntime();
            TextBlack.Name = "TextBlack";
            TextDarkGray = new TextRuntime();
            TextDarkGray.Name = "TextDarkGray";
            TextGray = new TextRuntime();
            TextGray.Name = "TextGray";
            TextLightGray = new TextRuntime();
            TextLightGray.Name = "TextLightGray";
            TextWhite = new TextRuntime();
            TextWhite.Name = "TextWhite";
            TextPrimaryDark = new TextRuntime();
            TextPrimaryDark.Name = "TextPrimaryDark";
            TextPrimary = new TextRuntime();
            TextPrimary.Name = "TextPrimary";
            TextPrimaryLight = new TextRuntime();
            TextPrimaryLight.Name = "TextPrimaryLight";
            TextAccent = new TextRuntime();
            TextAccent.Name = "TextAccent";
            TextSuccess = new TextRuntime();
            TextSuccess.Name = "TextSuccess";
            TextWarning = new TextRuntime();
            TextWarning.Name = "TextWarning";
            TextWarning1 = new TextRuntime();
            TextWarning1.Name = "TextWarning1";
            KeyboardInstance = new KeyboardRuntime();
            KeyboardInstance.Name = "KeyboardInstance";
            KeyboardContainer = new ContainerRuntime();
            KeyboardContainer.Name = "KeyboardContainer";
            TreeViewInstance = new TreeViewRuntime();
            TreeViewInstance.Name = "TreeViewInstance";
            DialogBoxInstance = new DialogBoxRuntime();
            DialogBoxInstance.Name = "DialogBoxInstance";
        }
        protected virtual void AssignParents()
        {
            if(this.Children != null) this.Children.Add(TextStyleContainer);
            else this.WhatThisContains.Add(TextStyleContainer);
            TextStyleContainer.Children.Add(TextTitle);
            TextStyleContainer.Children.Add(TextH1);
            TextStyleContainer.Children.Add(TextH2);
            TextStyleContainer.Children.Add(TextH3);
            TextStyleContainer.Children.Add(TextNormal);
            TextStyleContainer.Children.Add(TextStrong);
            TextStyleContainer.Children.Add(TextEmphasis);
            TextStyleContainer.Children.Add(TextSmall);
            TextStyleContainer.Children.Add(TextTiny);
            NineSliceStyleContainer.Children.Add(Solid);
            NineSliceStyleContainer.Children.Add(Bordered);
            NineSliceStyleContainer.Children.Add(BracketHorizontal);
            NineSliceStyleContainer.Children.Add(BracketVertical);
            NineSliceStyleContainer.Children.Add(Tab);
            NineSliceStyleContainer.Children.Add(TabBordered);
            NineSliceStyleContainer.Children.Add(Outlined);
            NineSliceStyleContainer.Children.Add(OutlinedHeavy);
            NineSliceStyleContainer.Children.Add(Panel);
            if(this.Children != null) this.Children.Add(NineSliceStyleContainer);
            else this.WhatThisContains.Add(NineSliceStyleContainer);
            if(this.Children != null) this.Children.Add(ButtonsContainer);
            else this.WhatThisContains.Add(ButtonsContainer);
            ButtonsContainer.Children.Add(ButtonStandardInstance);
            ButtonsContainer.Children.Add(ButtonStandardIconInstance);
            ButtonsContainer.Children.Add(ButtonTabInstance);
            ButtonsContainer.Children.Add(ButtonIconInstance);
            ButtonsContainer.Children.Add(ButtonConfirmInstance);
            ButtonsContainer.Children.Add(ButtonDenyInstance);
            ButtonsContainer.Children.Add(ButtonCloseInstance);
            if(this.Children != null) this.Children.Add(ElementsContainer);
            else this.WhatThisContains.Add(ElementsContainer);
            ElementsContainer.Children.Add(PercentBarPrimary);
            ElementsContainer.Children.Add(PercentBarLinesDecor);
            ElementsContainer.Children.Add(PercentBarCautionDecor);
            ElementsContainer.Children.Add(PercentBarIconPrimary);
            ElementsContainer.Children.Add(PercentBarIconLinesDecor);
            ElementsContainer.Children.Add(PercentBarIconCautionDecor);
            if(this.Children != null) this.Children.Add(ControlsContainer);
            else this.WhatThisContains.Add(ControlsContainer);
            ControlsContainer.Children.Add(LabelInstance);
            ControlsContainer.Children.Add(CheckBoxInstance);
            ControlsContainer.Children.Add(RadioButtonInstance);
            ControlsContainer.Children.Add(ComboBoxInstance);
            ControlsContainer.Children.Add(ListBoxInstance);
            ControlsContainer.Children.Add(SliderInstance);
            ControlsContainer.Children.Add(TextBoxInstance);
            ControlsContainer.Children.Add(PasswordBoxInstance);
            ElementsContainer.Children.Add(DividerVerticalInstance);
            ElementsContainer.Children.Add(DividerHorizontalInstance);
            ElementsContainer.Children.Add(IconInstance);
            ElementsContainer.Children.Add(CautionLinesInstance);
            ElementsContainer.Children.Add(VerticalLinesInstance);
            if(this.Children != null) this.Children.Add(ColorContainer);
            else this.WhatThisContains.Add(ColorContainer);
            ColorContainer.Children.Add(TextBlack);
            ColorContainer.Children.Add(TextDarkGray);
            ColorContainer.Children.Add(TextGray);
            ColorContainer.Children.Add(TextLightGray);
            ColorContainer.Children.Add(TextWhite);
            ColorContainer.Children.Add(TextPrimaryDark);
            ColorContainer.Children.Add(TextPrimary);
            ColorContainer.Children.Add(TextPrimaryLight);
            ColorContainer.Children.Add(TextAccent);
            ColorContainer.Children.Add(TextSuccess);
            ColorContainer.Children.Add(TextWarning);
            ColorContainer.Children.Add(TextWarning1);
            KeyboardContainer.Children.Add(KeyboardInstance);
            if(this.Children != null) this.Children.Add(KeyboardContainer);
            else this.WhatThisContains.Add(KeyboardContainer);
            ControlsContainer.Children.Add(TreeViewInstance);
            ControlsContainer.Children.Add(DialogBoxInstance);
        }
        private void ApplyDefaultVariables()
        {
            this.TextStyleContainer.ChildrenLayout = global::Gum.Managers.ChildrenLayout.TopToBottomStack;
            this.TextStyleContainer.Height = 0f;
            this.TextStyleContainer.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.TextStyleContainer.Width = 85f;
            this.TextStyleContainer.X = 57f;
            this.TextStyleContainer.Y = 11f;

TextTitle.SetProperty("ColorCategoryState", "White");
TextTitle.SetProperty("StyleCategoryState", "Title");
            this.TextTitle.Text = @"H2";

TextH1.SetProperty("ColorCategoryState", "White");
TextH1.SetProperty("StyleCategoryState", "H1");
            this.TextH1.Text = @"Heading 1";

TextH2.SetProperty("ColorCategoryState", "White");
TextH2.SetProperty("StyleCategoryState", "H2");
            this.TextH2.Text = @"Heading 2";

TextH3.SetProperty("ColorCategoryState", "White");
TextH3.SetProperty("StyleCategoryState", "H3");
            this.TextH3.Text = @"Heading 3";

TextNormal.SetProperty("ColorCategoryState", "White");
TextNormal.SetProperty("StyleCategoryState", "Normal");
            this.TextNormal.Text = @"Normal";

TextStrong.SetProperty("ColorCategoryState", "White");
TextStrong.SetProperty("StyleCategoryState", "Strong");
            this.TextStrong.Text = @"Strong";

TextEmphasis.SetProperty("ColorCategoryState", "White");
TextEmphasis.SetProperty("StyleCategoryState", "Emphasis");
            this.TextEmphasis.Text = @"Emphasis";

TextSmall.SetProperty("ColorCategoryState", "White");
TextSmall.SetProperty("StyleCategoryState", "Small");
            this.TextSmall.Text = @"Small";

TextTiny.SetProperty("ColorCategoryState", "White");
TextTiny.SetProperty("StyleCategoryState", "Tiny");
            this.TextTiny.Text = @"Tiny";

Solid.SetProperty("ColorCategoryState", "Primary");
Solid.SetProperty("StyleCategoryState", "Solid");
            this.Solid.Height = 32f;
            this.Solid.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            this.Solid.Width = 32f;
            this.Solid.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            this.Solid.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            this.Solid.XUnits = GeneralUnitType.PixelsFromSmall;
            this.Solid.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            this.Solid.YUnits = GeneralUnitType.PixelsFromSmall;

Bordered.SetProperty("ColorCategoryState", "Primary");
Bordered.SetProperty("StyleCategoryState", "Bordered");
            this.Bordered.Height = 32f;
            this.Bordered.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            this.Bordered.Width = 32f;
            this.Bordered.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            this.Bordered.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            this.Bordered.XUnits = GeneralUnitType.PixelsFromSmall;
            this.Bordered.Y = 8f;
            this.Bordered.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            this.Bordered.YUnits = GeneralUnitType.PixelsFromSmall;

BracketHorizontal.SetProperty("ColorCategoryState", "Primary");
BracketHorizontal.SetProperty("StyleCategoryState", "BracketHorizontal");
            this.BracketHorizontal.Height = 32f;
            this.BracketHorizontal.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            this.BracketHorizontal.Width = 32f;
            this.BracketHorizontal.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            this.BracketHorizontal.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            this.BracketHorizontal.XUnits = GeneralUnitType.PixelsFromSmall;
            this.BracketHorizontal.Y = 8f;
            this.BracketHorizontal.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            this.BracketHorizontal.YUnits = GeneralUnitType.PixelsFromSmall;

BracketVertical.SetProperty("ColorCategoryState", "Primary");
BracketVertical.SetProperty("StyleCategoryState", "BracketVertical");
            this.BracketVertical.Height = 32f;
            this.BracketVertical.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            this.BracketVertical.Width = 32f;
            this.BracketVertical.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            this.BracketVertical.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            this.BracketVertical.XUnits = GeneralUnitType.PixelsFromSmall;
            this.BracketVertical.Y = 8f;
            this.BracketVertical.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            this.BracketVertical.YUnits = GeneralUnitType.PixelsFromSmall;

Tab.SetProperty("ColorCategoryState", "Primary");
Tab.SetProperty("StyleCategoryState", "Tab");
            this.Tab.Height = 32f;
            this.Tab.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            this.Tab.Width = 32f;
            this.Tab.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            this.Tab.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            this.Tab.XUnits = GeneralUnitType.PixelsFromSmall;
            this.Tab.Y = 8f;
            this.Tab.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            this.Tab.YUnits = GeneralUnitType.PixelsFromSmall;

TabBordered.SetProperty("ColorCategoryState", "Primary");
TabBordered.SetProperty("StyleCategoryState", "TabBordered");
            this.TabBordered.Height = 32f;
            this.TabBordered.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            this.TabBordered.Width = 32f;
            this.TabBordered.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            this.TabBordered.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            this.TabBordered.XUnits = GeneralUnitType.PixelsFromSmall;
            this.TabBordered.Y = 8f;
            this.TabBordered.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            this.TabBordered.YUnits = GeneralUnitType.PixelsFromSmall;

Outlined.SetProperty("ColorCategoryState", "Primary");
Outlined.SetProperty("StyleCategoryState", "Outlined");
            this.Outlined.Height = 32f;
            this.Outlined.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            this.Outlined.Width = 32f;
            this.Outlined.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            this.Outlined.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            this.Outlined.XUnits = GeneralUnitType.PixelsFromSmall;
            this.Outlined.Y = 8f;
            this.Outlined.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            this.Outlined.YUnits = GeneralUnitType.PixelsFromSmall;

OutlinedHeavy.SetProperty("ColorCategoryState", "Primary");
OutlinedHeavy.SetProperty("StyleCategoryState", "OutlinedHeavy");
            this.OutlinedHeavy.Height = 32f;
            this.OutlinedHeavy.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            this.OutlinedHeavy.Width = 32f;
            this.OutlinedHeavy.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            this.OutlinedHeavy.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            this.OutlinedHeavy.XUnits = GeneralUnitType.PixelsFromSmall;
            this.OutlinedHeavy.Y = 8f;
            this.OutlinedHeavy.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            this.OutlinedHeavy.YUnits = GeneralUnitType.PixelsFromSmall;

Panel.SetProperty("ColorCategoryState", "Primary");
Panel.SetProperty("StyleCategoryState", "Panel");
            this.Panel.Height = 32f;
            this.Panel.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            this.Panel.Width = 32f;
            this.Panel.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            this.Panel.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            this.Panel.XUnits = GeneralUnitType.PixelsFromSmall;
            this.Panel.Y = 8f;
            this.Panel.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            this.Panel.YUnits = GeneralUnitType.PixelsFromSmall;

            this.NineSliceStyleContainer.ChildrenLayout = global::Gum.Managers.ChildrenLayout.TopToBottomStack;
            this.NineSliceStyleContainer.Height = 0f;
            this.NineSliceStyleContainer.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.NineSliceStyleContainer.Width = 0f;
            this.NineSliceStyleContainer.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.NineSliceStyleContainer.X = 9f;
            this.NineSliceStyleContainer.Y = 14f;

            this.ButtonsContainer.ChildrenLayout = global::Gum.Managers.ChildrenLayout.TopToBottomStack;
            this.ButtonsContainer.Height = 0f;
            this.ButtonsContainer.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.ButtonsContainer.Width = 0f;
            this.ButtonsContainer.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.ButtonsContainer.X = 159f;
            this.ButtonsContainer.Y = 12f;

            this.ButtonStandardInstance.ButtonDisplayText = @"Standard";

            this.ButtonStandardIconInstance.ButtonDisplayText = @"Standard Icon";
            this.ButtonStandardIconInstance.Y = 8f;

            this.ButtonTabInstance.TabDisplayText = @"Tab";
            this.ButtonTabInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            this.ButtonTabInstance.XUnits = GeneralUnitType.PixelsFromSmall;
            this.ButtonTabInstance.Y = 8f;
            this.ButtonTabInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;

            this.ButtonIconInstance.Y = 8f;

            this.ButtonConfirmInstance.ButtonDisplayText = @"Confirm";
            this.ButtonConfirmInstance.Y = 8f;

            this.ButtonDenyInstance.ButtonDisplayText = @"Deny";
            this.ButtonDenyInstance.Y = 8f;

            this.ButtonCloseInstance.Y = 8f;

            this.ElementsContainer.ChildrenLayout = global::Gum.Managers.ChildrenLayout.TopToBottomStack;
            this.ElementsContainer.Height = 0f;
            this.ElementsContainer.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.ElementsContainer.X = 310f;
            this.ElementsContainer.Y = 14f;

            this.PercentBarPrimary.Width = 0f;
            this.PercentBarPrimary.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;

this.PercentBarLinesDecor.BarDecorCategoryState = PercentBarRuntime.BarDecorCategory.VerticalLines;
            this.PercentBarLinesDecor.Width = 0f;
            this.PercentBarLinesDecor.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.PercentBarLinesDecor.Y = 4f;

this.PercentBarCautionDecor.BarDecorCategoryState = PercentBarRuntime.BarDecorCategory.CautionLines;
            this.PercentBarCautionDecor.Width = 0f;
            this.PercentBarCautionDecor.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.PercentBarCautionDecor.Y = 4f;

PercentBarIconPrimary.SetProperty("BarColor", "Primary");
this.PercentBarIconPrimary.BarDecorCategoryState = PercentBarIconRuntime.BarDecorCategory.None;
this.PercentBarIconPrimary.BarIcon = IconRuntime.IconCategory.Battery;
PercentBarIconPrimary.SetProperty("BarIconColor", "Primary");
            this.PercentBarIconPrimary.Width = 0f;
            this.PercentBarIconPrimary.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.PercentBarIconPrimary.Y = 4f;

PercentBarIconLinesDecor.SetProperty("BarColor", "Primary");
this.PercentBarIconLinesDecor.BarDecorCategoryState = PercentBarIconRuntime.BarDecorCategory.VerticalLines;
this.PercentBarIconLinesDecor.BarIcon = IconRuntime.IconCategory.Battery;
PercentBarIconLinesDecor.SetProperty("BarIconColor", "Primary");
            this.PercentBarIconLinesDecor.Width = 0f;
            this.PercentBarIconLinesDecor.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.PercentBarIconLinesDecor.Y = 4f;

PercentBarIconCautionDecor.SetProperty("BarColor", "Primary");
this.PercentBarIconCautionDecor.BarDecorCategoryState = PercentBarIconRuntime.BarDecorCategory.CautionLines;
this.PercentBarIconCautionDecor.BarIcon = IconRuntime.IconCategory.Battery;
PercentBarIconCautionDecor.SetProperty("BarIconColor", "Primary");
            this.PercentBarIconCautionDecor.Width = 0f;
            this.PercentBarIconCautionDecor.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.PercentBarIconCautionDecor.Y = 4f;

            this.ControlsContainer.ChildrenLayout = global::Gum.Managers.ChildrenLayout.TopToBottomStack;
            this.ControlsContainer.Height = 0f;
            this.ControlsContainer.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.ControlsContainer.Width = 256f;
            this.ControlsContainer.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            this.ControlsContainer.X = 482f;
            this.ControlsContainer.Y = 12f;


            this.CheckBoxInstance.Width = 0f;
            this.CheckBoxInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;

            this.RadioButtonInstance.Width = 0f;
            this.RadioButtonInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.RadioButtonInstance.Y = 4f;

            this.ComboBoxInstance.Width = 0f;
            this.ComboBoxInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.ComboBoxInstance.Y = 4f;

            this.ListBoxInstance.Height = 96f;
            this.ListBoxInstance.Width = 0f;
            this.ListBoxInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.ListBoxInstance.Y = 4f;

            this.SliderInstance.Width = 0f;
            this.SliderInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.SliderInstance.Y = 4f;

            this.TextBoxInstance.Y = 4f;

            this.PasswordBoxInstance.Y = 4f;

            this.DividerVerticalInstance.Height = 24f;
            this.DividerVerticalInstance.Y = 4f;

            this.DividerHorizontalInstance.Width = 0f;
            this.DividerHorizontalInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.DividerHorizontalInstance.Y = 4f;


CautionLinesInstance.SetProperty("LineColor", "Primary");
            this.CautionLinesInstance.Width = 0f;
            this.CautionLinesInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.CautionLinesInstance.Y = 4f;

VerticalLinesInstance.SetProperty("LineColor", "Primary");
            this.VerticalLinesInstance.Width = 0f;
            this.VerticalLinesInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.VerticalLinesInstance.Y = 4f;

            this.ColorContainer.ChildrenLayout = global::Gum.Managers.ChildrenLayout.TopToBottomStack;
            this.ColorContainer.Height = 0f;
            this.ColorContainer.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.ColorContainer.X = 774f;
            this.ColorContainer.Y = 24f;

TextBlack.SetProperty("ColorCategoryState", "Black");
TextBlack.SetProperty("StyleCategoryState", "Normal");
            this.TextBlack.Text = @"Black";
            this.TextBlack.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;

TextDarkGray.SetProperty("ColorCategoryState", "DarkGray");
TextDarkGray.SetProperty("StyleCategoryState", "Normal");
            this.TextDarkGray.Text = @"Dark Gray";
            this.TextDarkGray.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;

TextGray.SetProperty("ColorCategoryState", "Gray");
TextGray.SetProperty("StyleCategoryState", "Normal");
            this.TextGray.Text = @"Gray";
            this.TextGray.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;

TextLightGray.SetProperty("ColorCategoryState", "LightGray");
TextLightGray.SetProperty("StyleCategoryState", "Normal");
            this.TextLightGray.Text = @"Light Gray";
            this.TextLightGray.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;

TextWhite.SetProperty("ColorCategoryState", "White");
TextWhite.SetProperty("StyleCategoryState", "Normal");
            this.TextWhite.Text = @"White";
            this.TextWhite.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;

TextPrimaryDark.SetProperty("ColorCategoryState", "PrimaryDark");
TextPrimaryDark.SetProperty("StyleCategoryState", "Normal");
            this.TextPrimaryDark.Text = @"Primary Dark";
            this.TextPrimaryDark.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;

TextPrimary.SetProperty("ColorCategoryState", "Primary");
TextPrimary.SetProperty("StyleCategoryState", "Normal");
            this.TextPrimary.Text = @"Primary";
            this.TextPrimary.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;

TextPrimaryLight.SetProperty("ColorCategoryState", "PrimaryLight");
TextPrimaryLight.SetProperty("StyleCategoryState", "Normal");
            this.TextPrimaryLight.Text = @"Primary Light";
            this.TextPrimaryLight.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;

TextAccent.SetProperty("ColorCategoryState", "Accent");
TextAccent.SetProperty("StyleCategoryState", "Normal");
            this.TextAccent.Text = @"Accent";
            this.TextAccent.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;

TextSuccess.SetProperty("ColorCategoryState", "Success");
TextSuccess.SetProperty("StyleCategoryState", "Normal");
            this.TextSuccess.Text = @"Success";
            this.TextSuccess.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;

TextWarning.SetProperty("ColorCategoryState", "Warning");
TextWarning.SetProperty("StyleCategoryState", "Normal");
            this.TextWarning.Text = @"Warning";
            this.TextWarning.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;

TextWarning1.SetProperty("ColorCategoryState", "Danger");
TextWarning1.SetProperty("StyleCategoryState", "Normal");
            this.TextWarning1.Text = @"Danger";
            this.TextWarning1.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;

            this.KeyboardInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;

            this.KeyboardContainer.Height = 144f;
            this.KeyboardContainer.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            this.KeyboardContainer.Width = 352f;
            this.KeyboardContainer.X = -326f;
            this.KeyboardContainer.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.KeyboardContainer.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.KeyboardContainer.Y = 77f;
            this.KeyboardContainer.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.KeyboardContainer.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.TreeViewInstance.Width = 0f;
            this.TreeViewInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.TreeViewInstance.Y = 8f;

            this.DialogBoxInstance.Width = 0f;
            this.DialogBoxInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.DialogBoxInstance.Y = 8f;

        }
        partial void CustomInitialize();
    }
}
