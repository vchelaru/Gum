//Code for Controls/ComboBox (Container)
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
    public partial class ComboBoxRuntime:ContainerRuntime
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/ComboBox", typeof(ComboBoxRuntime));
        }
        public MonoGameGum.Forms.Controls.ComboBox FormsControl => FormsControlAsObject as MonoGameGum.Forms.Controls.ComboBox;
        public enum ComboBoxCategory
        {
            Enabled,
            Disabled,
            Highlighted,
            Pushed,
            HighlightedFocused,
            Focused,
            DisabledFocused,
        }

        ComboBoxCategory mComboBoxCategoryState;
        public ComboBoxCategory ComboBoxCategoryState
        {
            get => mComboBoxCategoryState;
            set
            {
                mComboBoxCategoryState = value;
                var appliedDynamically = false;
                if(!appliedDynamically)
                {
                    switch (value)
                    {
                        case ComboBoxCategory.Enabled:
                            this.FocusedIndicator.Visible = false;
                            IconInstance.SetProperty("IconColor", "Primary");
                            TextInstance.SetProperty("ColorCategoryState", "White");
                            break;
                        case ComboBoxCategory.Disabled:
                            this.FocusedIndicator.Visible = false;
                            IconInstance.SetProperty("IconColor", "Gray");
                            TextInstance.SetProperty("ColorCategoryState", "Gray");
                            break;
                        case ComboBoxCategory.Highlighted:
                            this.FocusedIndicator.Visible = false;
                            IconInstance.SetProperty("IconColor", "PrimaryLight");
                            TextInstance.SetProperty("ColorCategoryState", "PrimaryLight");
                            break;
                        case ComboBoxCategory.Pushed:
                            this.FocusedIndicator.Visible = false;
                            IconInstance.SetProperty("IconColor", "PrimaryDark");
                            TextInstance.SetProperty("ColorCategoryState", "PrimaryDark");
                            break;
                        case ComboBoxCategory.HighlightedFocused:
                            this.FocusedIndicator.Visible = true;
                            IconInstance.SetProperty("IconColor", "Primary");
                            TextInstance.SetProperty("ColorCategoryState", "White");
                            break;
                        case ComboBoxCategory.Focused:
                            this.FocusedIndicator.Visible = true;
                            IconInstance.SetProperty("IconColor", "Primary");
                            TextInstance.SetProperty("ColorCategoryState", "White");
                            break;
                        case ComboBoxCategory.DisabledFocused:
                            this.FocusedIndicator.Visible = true;
                            IconInstance.SetProperty("IconColor", "Primary");
                            TextInstance.SetProperty("ColorCategoryState", "White");
                            break;
                    }
                }
            }
        }
        public NineSliceRuntime Background { get; protected set; }
        public TextRuntime TextInstance { get; protected set; }
        public ListBoxRuntime ListBoxInstance { get; protected set; }
        public IconRuntime IconInstance { get; protected set; }
        public NineSliceRuntime FocusedIndicator { get; protected set; }

        public ComboBoxRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {
            if(fullInstantiation)
            {
            }

            this.Height = 24f;
             
            this.Width = 256f;

            InitializeInstances();

            ApplyDefaultVariables();
            AssignParents();
            if(tryCreateFormsObject)
            {
                if (FormsControl == null)
                {
                    FormsControlAsObject = new MonoGameGum.Forms.Controls.ComboBox(this);
                }
            }
            CustomInitialize();
        }
        protected virtual void InitializeInstances()
        {
            Background = new NineSliceRuntime();
            Background.Name = "Background";
            TextInstance = new TextRuntime();
            TextInstance.Name = "TextInstance";
            ListBoxInstance = new ListBoxRuntime();
            ListBoxInstance.Name = "ListBoxInstance";
            IconInstance = new IconRuntime();
            IconInstance.Name = "IconInstance";
            FocusedIndicator = new NineSliceRuntime();
            FocusedIndicator.Name = "FocusedIndicator";
        }
        protected virtual void AssignParents()
        {
            this.Children.Add(Background);
            this.Children.Add(TextInstance);
            this.Children.Add(ListBoxInstance);
            this.Children.Add(IconInstance);
            this.Children.Add(FocusedIndicator);
        }
        private void ApplyDefaultVariables()
        {
Background.SetProperty("ColorCategoryState", "DarkGray");
Background.SetProperty("StyleCategoryState", "Solid");
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

TextInstance.SetProperty("ColorCategoryState", "White");
TextInstance.SetProperty("StyleCategoryState", "Strong");
            this.TextInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.TextInstance.Text = @"Selected Item";
            this.TextInstance.VerticalAlignment = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.TextInstance.Width = -8f;
            this.TextInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.TextInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.TextInstance.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.TextInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.TextInstance.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.ListBoxInstance.Height = 128f;
            this.ListBoxInstance.Visible = false;
            this.ListBoxInstance.Width = 0f;
            this.ListBoxInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.ListBoxInstance.Y = 28f;

this.IconInstance.IconCategoryState = IconRuntime.IconCategory.Arrow2;
IconInstance.SetProperty("IconColor", "Primary");
            this.IconInstance.HasEvents = false;
            this.IconInstance.Height = 24f;
            this.IconInstance.Rotation = -90f;
            this.IconInstance.Width = 24f;
            this.IconInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.IconInstance.XUnits = GeneralUnitType.PixelsFromLarge;
            this.IconInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            this.IconInstance.YUnits = GeneralUnitType.PixelsFromMiddle;

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
