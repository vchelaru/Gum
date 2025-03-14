//Code for Controls/CheckBox (Container)
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
    public partial class CheckBoxRuntime:ContainerRuntime
    {
        public MonoGameGum.Forms.Controls.CheckBox FormsControl => FormsControlAsObject as MonoGameGum.Forms.Controls.CheckBox;
        public enum CheckBoxCategory
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

        CheckBoxCategory mCheckBoxCategoryState;
        public CheckBoxCategory CheckBoxCategoryState
        {
            get => mCheckBoxCategoryState;
            set
            {
                mCheckBoxCategoryState = value;
                var appliedDynamically = false;
                if(!appliedDynamically)
                {
                    switch (value)
                    {
                        case CheckBoxCategory.EnabledOn:
                            Check.SetProperty("IconColor", "White");
                            this.Check.Visible = true;
                            CheckboxBackground.SetProperty("ColorCategoryState", "Primary");
                            this.FocusedIndicator.Visible = false;
                            TextInstance.SetProperty("ColorCategoryState", "White");
                            break;
                        case CheckBoxCategory.EnabledOff:
                            Check.SetProperty("IconColor", "White");
                            this.Check.Visible = false;
                            CheckboxBackground.SetProperty("ColorCategoryState", "Primary");
                            this.FocusedIndicator.Visible = false;
                            TextInstance.SetProperty("ColorCategoryState", "White");
                            break;
                        case CheckBoxCategory.DisabledOn:
                            Check.SetProperty("IconColor", "Gray");
                            this.Check.Visible = true;
                            CheckboxBackground.SetProperty("ColorCategoryState", "DarkGray");
                            this.FocusedIndicator.Visible = false;
                            TextInstance.SetProperty("ColorCategoryState", "Gray");
                            break;
                        case CheckBoxCategory.DisabledOff:
                            Check.SetProperty("IconColor", "White");
                            this.Check.Visible = false;
                            CheckboxBackground.SetProperty("ColorCategoryState", "DarkGray");
                            this.FocusedIndicator.Visible = false;
                            TextInstance.SetProperty("ColorCategoryState", "Gray");
                            break;
                        case CheckBoxCategory.HighlightedOn:
                            Check.SetProperty("IconColor", "White");
                            this.Check.Visible = true;
                            CheckboxBackground.SetProperty("ColorCategoryState", "PrimaryLight");
                            this.FocusedIndicator.Visible = false;
                            TextInstance.SetProperty("ColorCategoryState", "White");
                            break;
                        case CheckBoxCategory.HighlightedOff:
                            Check.SetProperty("IconColor", "White");
                            this.Check.Visible = false;
                            CheckboxBackground.SetProperty("ColorCategoryState", "PrimaryLight");
                            this.FocusedIndicator.Visible = false;
                            TextInstance.SetProperty("ColorCategoryState", "White");
                            break;
                        case CheckBoxCategory.PushedOn:
                            Check.SetProperty("IconColor", "White");
                            this.Check.Visible = true;
                            CheckboxBackground.SetProperty("ColorCategoryState", "PrimaryLight");
                            this.FocusedIndicator.Visible = false;
                            TextInstance.SetProperty("ColorCategoryState", "White");
                            break;
                        case CheckBoxCategory.PushedOff:
                            Check.SetProperty("IconColor", "White");
                            this.Check.Visible = false;
                            CheckboxBackground.SetProperty("ColorCategoryState", "PrimaryLight");
                            this.FocusedIndicator.Visible = false;
                            TextInstance.SetProperty("ColorCategoryState", "White");
                            break;
                        case CheckBoxCategory.FocusedOn:
                            Check.SetProperty("IconColor", "White");
                            this.Check.Visible = true;
                            CheckboxBackground.SetProperty("ColorCategoryState", "Primary");
                            this.FocusedIndicator.Visible = true;
                            TextInstance.SetProperty("ColorCategoryState", "White");
                            break;
                        case CheckBoxCategory.FocusedOff:
                            Check.SetProperty("IconColor", "White");
                            this.Check.Visible = false;
                            CheckboxBackground.SetProperty("ColorCategoryState", "Primary");
                            this.FocusedIndicator.Visible = true;
                            TextInstance.SetProperty("ColorCategoryState", "White");
                            break;
                        case CheckBoxCategory.HighlightedFocusedOn:
                            Check.SetProperty("IconColor", "White");
                            this.Check.Visible = true;
                            CheckboxBackground.SetProperty("ColorCategoryState", "PrimaryLight");
                            this.FocusedIndicator.Visible = true;
                            TextInstance.SetProperty("ColorCategoryState", "White");
                            break;
                        case CheckBoxCategory.HighlightedFocusedOff:
                            Check.SetProperty("IconColor", "White");
                            this.Check.Visible = false;
                            CheckboxBackground.SetProperty("ColorCategoryState", "PrimaryLight");
                            this.FocusedIndicator.Visible = true;
                            TextInstance.SetProperty("ColorCategoryState", "White");
                            break;
                        case CheckBoxCategory.DisabledFocusedOn:
                            Check.SetProperty("IconColor", "Gray");
                            this.Check.Visible = true;
                            CheckboxBackground.SetProperty("ColorCategoryState", "DarkGray");
                            this.FocusedIndicator.Visible = true;
                            TextInstance.SetProperty("ColorCategoryState", "Gray");
                            break;
                        case CheckBoxCategory.DisabledFocusedOff:
                            Check.SetProperty("IconColor", "White");
                            this.Check.Visible = false;
                            CheckboxBackground.SetProperty("ColorCategoryState", "DarkGray");
                            this.FocusedIndicator.Visible = true;
                            TextInstance.SetProperty("ColorCategoryState", "Gray");
                            break;
                    }
                }
            }
        }
        public NineSliceRuntime CheckboxBackground { get; protected set; }
        public TextRuntime TextInstance { get; protected set; }
        public IconRuntime Check { get; protected set; }
        public NineSliceRuntime FocusedIndicator { get; protected set; }

        public string CheckboxDisplayText
        {
            get => TextInstance.Text;
            set => TextInstance.Text = value;
        }

        public CheckBoxRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
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
                    FormsControlAsObject = new MonoGameGum.Forms.Controls.CheckBox(this);
                }
            }
            CustomInitialize();
        }
        protected virtual void InitializeInstances()
        {
            CheckboxBackground = new NineSliceRuntime();
            CheckboxBackground.Name = "CheckboxBackground";
            TextInstance = new TextRuntime();
            TextInstance.Name = "TextInstance";
            Check = new IconRuntime();
            Check.Name = "Check";
            FocusedIndicator = new NineSliceRuntime();
            FocusedIndicator.Name = "FocusedIndicator";
        }
        protected virtual void AssignParents()
        {
            this.Children.Add(CheckboxBackground);
            this.Children.Add(TextInstance);
            CheckboxBackground.Children.Add(Check);
            this.Children.Add(FocusedIndicator);
        }
        private void ApplyDefaultVariables()
        {
CheckboxBackground.SetProperty("ColorCategoryState", "Primary");
CheckboxBackground.SetProperty("StyleCategoryState", "Bordered");
            this.CheckboxBackground.Height = 24f;
            this.CheckboxBackground.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            this.CheckboxBackground.Width = 24f;
            this.CheckboxBackground.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            this.CheckboxBackground.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            this.CheckboxBackground.XUnits = GeneralUnitType.PixelsFromSmall;

TextInstance.SetProperty("ColorCategoryState", "White");
TextInstance.SetProperty("StyleCategoryState", "Normal");
            this.TextInstance.Height = 32f;
            this.TextInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            this.TextInstance.HorizontalAlignment = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            this.TextInstance.Text = @"Checkbox Label";
            this.TextInstance.VerticalAlignment = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.TextInstance.Width = -28f;
            this.TextInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.TextInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
            this.TextInstance.XUnits = GeneralUnitType.PixelsFromLarge;
            this.TextInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.TextInstance.YUnits = GeneralUnitType.PixelsFromMiddle;

this.Check.IconCategoryState = IconRuntime.IconCategory.Check;
Check.SetProperty("IconColor", "White");
            this.Check.Height = 0f;
            this.Check.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.Check.Width = 0f;
            this.Check.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;

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
