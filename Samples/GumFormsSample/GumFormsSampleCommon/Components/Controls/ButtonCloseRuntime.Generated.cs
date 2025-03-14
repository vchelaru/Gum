//Code for Controls/ButtonClose (Container)
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
    public partial class ButtonCloseRuntime:ContainerRuntime
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/ButtonClose", typeof(ButtonCloseRuntime));
        }
        public MonoGameGum.Forms.Controls.Button FormsControl => FormsControlAsObject as MonoGameGum.Forms.Controls.Button;
        public enum ButtonCategory
        {
            Enabled,
            Disabled,
            Highlighted,
            Pushed,
            HighlightedFocused,
            Focused,
            DisabledFocused,
        }

        ButtonCategory mButtonCategoryState;
        public ButtonCategory ButtonCategoryState
        {
            get => mButtonCategoryState;
            set
            {
                mButtonCategoryState = value;
                var appliedDynamically = false;
                if(!appliedDynamically)
                {
                    switch (value)
                    {
                        case ButtonCategory.Enabled:
                            Background.SetProperty("ColorCategoryState", "Danger");
                            this.FocusedIndicator.Visible = false;
                            Icon.SetProperty("IconColor", "White");
                            break;
                        case ButtonCategory.Disabled:
                            Background.SetProperty("ColorCategoryState", "DarkGray");
                            this.FocusedIndicator.Visible = false;
                            Icon.SetProperty("IconColor", "Gray");
                            break;
                        case ButtonCategory.Highlighted:
                            Background.SetProperty("ColorCategoryState", "Warning");
                            this.FocusedIndicator.Visible = false;
                            Icon.SetProperty("IconColor", "White");
                            break;
                        case ButtonCategory.Pushed:
                            Background.SetProperty("ColorCategoryState", "Primary");
                            this.FocusedIndicator.Visible = false;
                            Icon.SetProperty("IconColor", "White");
                            break;
                        case ButtonCategory.HighlightedFocused:
                            Background.SetProperty("ColorCategoryState", "Warning");
                            this.FocusedIndicator.Visible = true;
                            Icon.SetProperty("IconColor", "White");
                            break;
                        case ButtonCategory.Focused:
                            Background.SetProperty("ColorCategoryState", "Danger");
                            this.FocusedIndicator.Visible = true;
                            Icon.SetProperty("IconColor", "White");
                            break;
                        case ButtonCategory.DisabledFocused:
                            Background.SetProperty("ColorCategoryState", "DarkGray");
                            this.FocusedIndicator.Visible = true;
                            Icon.SetProperty("IconColor", "Gray");
                            break;
                    }
                }
            }
        }
        public NineSliceRuntime Background { get; protected set; }
        public IconRuntime Icon { get; protected set; }
        public NineSliceRuntime FocusedIndicator { get; protected set; }

        public ButtonCloseRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {
            if(fullInstantiation)
            {
            }

             
            this.Height = 32f;
            this.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
             
            this.Width = 32f;
            this.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;

            InitializeInstances();

            ApplyDefaultVariables();
            AssignParents();
            if(tryCreateFormsObject)
            {
                if (FormsControl == null)
                {
                    FormsControlAsObject = new MonoGameGum.Forms.Controls.Button(this);
                }
            }
            CustomInitialize();
        }
        protected virtual void InitializeInstances()
        {
            Background = new NineSliceRuntime();
            Background.Name = "Background";
            Icon = new IconRuntime();
            Icon.Name = "Icon";
            FocusedIndicator = new NineSliceRuntime();
            FocusedIndicator.Name = "FocusedIndicator";
        }
        protected virtual void AssignParents()
        {
            this.Children.Add(Background);
            this.Children.Add(Icon);
            this.Children.Add(FocusedIndicator);
        }
        private void ApplyDefaultVariables()
        {
Background.SetProperty("ColorCategoryState", "Danger");
Background.SetProperty("StyleCategoryState", "Bordered");
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

this.Icon.IconCategoryState = IconRuntime.IconCategory.Close;
            this.Icon.X = 0f;
            this.Icon.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.Icon.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.Icon.Y = 0f;
            this.Icon.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.Icon.YUnits = GeneralUnitType.PixelsFromMiddle;

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
