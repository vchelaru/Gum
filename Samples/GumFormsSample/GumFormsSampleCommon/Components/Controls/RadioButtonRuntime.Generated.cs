//Code for Controls/RadioButton (Container)
using GumRuntime;
using GumFormsSample.Components;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
namespace GumFormsSample.Components
{
    public partial class RadioButtonRuntime:ContainerRuntime
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/RadioButton", typeof(RadioButtonRuntime));
        }
        public MonoGameGum.Forms.Controls.RadioButton FormsControl => FormsControlAsObject as MonoGameGum.Forms.Controls.RadioButton;
        public enum RadioButtonCategory
        {
            EnabledOn,
            EnabledOff,
            DisabledOn,
            DisabledOff,
            HighlightedOn,
            HighlightedOff,
            PushedOn,
            PushedOff,
            FocusedOn,
            FocusedOff,
            HighlightedFocusedOn,
            HighlightedFocusedOff,
            DisabledFocusedOn,
            DisabledFocusedOff,
        }

        RadioButtonCategory mRadioButtonCategoryState;
        public RadioButtonCategory RadioButtonCategoryState
        {
            get => mRadioButtonCategoryState;
            set
            {
                mRadioButtonCategoryState = value;
                var appliedDynamically = false;
                if(!appliedDynamically)
                {
                    switch (value)
                    {
                        case RadioButtonCategory.EnabledOn:
                            this.FocusedIndicator.Visible = false;
                            Radio.SetProperty("IconColor", "White");
                            this.Radio.Visible = true;
                            RadioBackground.SetProperty("ColorCategoryState", "Primary");
                            TextInstance.SetProperty("ColorCategoryState", "White");
                            break;
                        case RadioButtonCategory.EnabledOff:
                            this.FocusedIndicator.Visible = false;
                            Radio.SetProperty("IconColor", "White");
                            this.Radio.Visible = false;
                            RadioBackground.SetProperty("ColorCategoryState", "Primary");
                            TextInstance.SetProperty("ColorCategoryState", "White");
                            break;
                        case RadioButtonCategory.DisabledOn:
                            this.FocusedIndicator.Visible = false;
                            Radio.SetProperty("IconColor", "Gray");
                            this.Radio.Visible = true;
                            RadioBackground.SetProperty("ColorCategoryState", "DarkGray");
                            TextInstance.SetProperty("ColorCategoryState", "Gray");
                            break;
                        case RadioButtonCategory.DisabledOff:
                            this.FocusedIndicator.Visible = false;
                            Radio.SetProperty("IconColor", "Gray");
                            this.Radio.Visible = false;
                            RadioBackground.SetProperty("ColorCategoryState", "DarkGray");
                            TextInstance.SetProperty("ColorCategoryState", "Gray");
                            break;
                        case RadioButtonCategory.HighlightedOn:
                            this.FocusedIndicator.Visible = false;
                            Radio.SetProperty("IconColor", "White");
                            this.Radio.Visible = true;
                            RadioBackground.SetProperty("ColorCategoryState", "PrimaryLight");
                            TextInstance.SetProperty("ColorCategoryState", "White");
                            break;
                        case RadioButtonCategory.HighlightedOff:
                            this.FocusedIndicator.Visible = false;
                            Radio.SetProperty("IconColor", "White");
                            this.Radio.Visible = false;
                            RadioBackground.SetProperty("ColorCategoryState", "PrimaryLight");
                            TextInstance.SetProperty("ColorCategoryState", "White");
                            break;
                        case RadioButtonCategory.PushedOn:
                            this.FocusedIndicator.Visible = false;
                            Radio.SetProperty("IconColor", "White");
                            this.Radio.Visible = true;
                            RadioBackground.SetProperty("ColorCategoryState", "PrimaryDark");
                            TextInstance.SetProperty("ColorCategoryState", "White");
                            break;
                        case RadioButtonCategory.PushedOff:
                            this.FocusedIndicator.Visible = false;
                            Radio.SetProperty("IconColor", "White");
                            this.Radio.Visible = false;
                            RadioBackground.SetProperty("ColorCategoryState", "PrimaryDark");
                            TextInstance.SetProperty("ColorCategoryState", "White");
                            break;
                        case RadioButtonCategory.FocusedOn:
                            this.FocusedIndicator.Visible = true;
                            Radio.SetProperty("IconColor", "White");
                            this.Radio.Visible = true;
                            RadioBackground.SetProperty("ColorCategoryState", "Primary");
                            TextInstance.SetProperty("ColorCategoryState", "White");
                            break;
                        case RadioButtonCategory.FocusedOff:
                            this.FocusedIndicator.Visible = true;
                            Radio.SetProperty("IconColor", "White");
                            this.Radio.Visible = false;
                            RadioBackground.SetProperty("ColorCategoryState", "Primary");
                            TextInstance.SetProperty("ColorCategoryState", "White");
                            break;
                        case RadioButtonCategory.HighlightedFocusedOn:
                            this.FocusedIndicator.Visible = true;
                            Radio.SetProperty("IconColor", "White");
                            this.Radio.Visible = true;
                            RadioBackground.SetProperty("ColorCategoryState", "PrimaryLight");
                            TextInstance.SetProperty("ColorCategoryState", "White");
                            break;
                        case RadioButtonCategory.HighlightedFocusedOff:
                            this.FocusedIndicator.Visible = true;
                            Radio.SetProperty("IconColor", "White");
                            this.Radio.Visible = false;
                            RadioBackground.SetProperty("ColorCategoryState", "PrimaryLight");
                            TextInstance.SetProperty("ColorCategoryState", "White");
                            break;
                        case RadioButtonCategory.DisabledFocusedOn:
                            this.FocusedIndicator.Visible = true;
                            Radio.SetProperty("IconColor", "Gray");
                            this.Radio.Visible = true;
                            RadioBackground.SetProperty("ColorCategoryState", "DarkGray");
                            TextInstance.SetProperty("ColorCategoryState", "Gray");
                            break;
                        case RadioButtonCategory.DisabledFocusedOff:
                            this.FocusedIndicator.Visible = true;
                            Radio.SetProperty("IconColor", "Gray");
                            this.Radio.Visible = false;
                            RadioBackground.SetProperty("ColorCategoryState", "DarkGray");
                            TextInstance.SetProperty("ColorCategoryState", "Gray");
                            break;
                    }
                }
            }
        }
        public NineSliceRuntime RadioBackground { get; protected set; }
        public IconRuntime Radio { get; protected set; }
        public TextRuntime TextInstance { get; protected set; }
        public NineSliceRuntime FocusedIndicator { get; protected set; }

        public string RadioDisplayText
        {
            get => TextInstance.Text;
            set => TextInstance.Text = value;
        }

        public RadioButtonRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {
            if(fullInstantiation)
            {
            }

            this.Height = 24f;
             
            this.Width = 128f;

            InitializeInstances();

            ApplyDefaultVariables();
            AssignParents();
            if(tryCreateFormsObject)
            {
                if (FormsControl == null)
                {
                    FormsControlAsObject = new MonoGameGum.Forms.Controls.RadioButton(this);
                }
            }
            CustomInitialize();
        }
        protected virtual void InitializeInstances()
        {
            RadioBackground = new NineSliceRuntime();
            RadioBackground.Name = "RadioBackground";
            Radio = new IconRuntime();
            Radio.Name = "Radio";
            TextInstance = new TextRuntime();
            TextInstance.Name = "TextInstance";
            FocusedIndicator = new NineSliceRuntime();
            FocusedIndicator.Name = "FocusedIndicator";
        }
        protected virtual void AssignParents()
        {
            this.Children.Add(RadioBackground);
            RadioBackground.Children.Add(Radio);
            this.Children.Add(TextInstance);
            this.Children.Add(FocusedIndicator);
        }
        private void ApplyDefaultVariables()
        {
RadioBackground.SetProperty("ColorCategoryState", "Primary");
RadioBackground.SetProperty("StyleCategoryState", "Bordered");
            this.RadioBackground.Height = 24f;
            this.RadioBackground.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            this.RadioBackground.Width = 24f;
            this.RadioBackground.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            this.RadioBackground.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            this.RadioBackground.XUnits = GeneralUnitType.PixelsFromSmall;
            this.RadioBackground.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.RadioBackground.YUnits = GeneralUnitType.PixelsFromMiddle;

this.Radio.IconCategoryState = IconRuntime.IconCategory.Circle2;
Radio.SetProperty("IconColor", "White");
            this.Radio.Height = 0f;
            this.Radio.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.Radio.Width = 0f;
            this.Radio.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;

TextInstance.SetProperty("ColorCategoryState", "White");
TextInstance.SetProperty("StyleCategoryState", "Normal");
            this.TextInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.TextInstance.Text = @"Radio Label";
            this.TextInstance.VerticalAlignment = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.TextInstance.Width = -28f;
            this.TextInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.TextInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
            this.TextInstance.XUnits = GeneralUnitType.PixelsFromLarge;

FocusedIndicator.SetProperty("ColorCategoryState", "Warning");
FocusedIndicator.SetProperty("StyleCategoryState", "Solid");
            this.FocusedIndicator.Height = 2f;
            this.FocusedIndicator.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            this.FocusedIndicator.Visible = false;
            this.FocusedIndicator.Y = 2f;
            this.FocusedIndicator.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            this.FocusedIndicator.YUnits = GeneralUnitType.PixelsFromLarge;

        }
        partial void CustomInitialize();
    }
}
