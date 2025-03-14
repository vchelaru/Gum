//Code for Controls/ButtonStandardIcon (Container)
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
    public partial class ButtonStandardIconRuntime:ContainerRuntime
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/ButtonStandardIcon", typeof(ButtonStandardIconRuntime));
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
                            Background.SetProperty("ColorCategoryState", "Primary");
                            this.FocusedIndicator.Visible = false;
                            Icon.SetProperty("IconColor", "PrimaryDark");
                            TextInstance.SetProperty("ColorCategoryState", "White");
                            break;
                        case ButtonCategory.Disabled:
                            Background.SetProperty("ColorCategoryState", "DarkGray");
                            this.FocusedIndicator.Visible = false;
                            Icon.SetProperty("IconColor", "Gray");
                            TextInstance.SetProperty("ColorCategoryState", "Gray");
                            break;
                        case ButtonCategory.Highlighted:
                            Background.SetProperty("ColorCategoryState", "PrimaryLight");
                            this.FocusedIndicator.Visible = false;
                            Icon.SetProperty("IconColor", "Primary");
                            TextInstance.SetProperty("ColorCategoryState", "White");
                            break;
                        case ButtonCategory.Pushed:
                            Background.SetProperty("ColorCategoryState", "PrimaryDark");
                            this.FocusedIndicator.Visible = false;
                            Icon.SetProperty("IconColor", "Primary");
                            TextInstance.SetProperty("ColorCategoryState", "White");
                            break;
                        case ButtonCategory.HighlightedFocused:
                            Background.SetProperty("ColorCategoryState", "PrimaryLight");
                            this.FocusedIndicator.Visible = true;
                            Icon.SetProperty("IconColor", "Primary");
                            TextInstance.SetProperty("ColorCategoryState", "White");
                            break;
                        case ButtonCategory.Focused:
                            Background.SetProperty("ColorCategoryState", "Primary");
                            this.FocusedIndicator.Visible = true;
                            Icon.SetProperty("IconColor", "PrimaryDark");
                            TextInstance.SetProperty("ColorCategoryState", "White");
                            break;
                        case ButtonCategory.DisabledFocused:
                            Background.SetProperty("ColorCategoryState", "DarkGray");
                            this.FocusedIndicator.Visible = true;
                            Icon.SetProperty("IconColor", "Gray");
                            TextInstance.SetProperty("ColorCategoryState", "Gray");
                            break;
                    }
                }
            }
        }
        public NineSliceRuntime Background { get; protected set; }
        public TextRuntime TextInstance { get; protected set; }
        public IconRuntime Icon { get; protected set; }
        public NineSliceRuntime FocusedIndicator { get; protected set; }

        public IconRuntime.IconCategory ButtonIcon
        {
            set => Icon.IconCategoryState = value;
        }

        public string ButtonDisplayText
        {
            get => TextInstance.Text;
            set => TextInstance.Text = value;
        }

        public ButtonStandardIconRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {
            if(fullInstantiation)
            {
            }

             
            this.Height = 32f;
             
            this.Width = 128f;

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
            TextInstance = new TextRuntime();
            TextInstance.Name = "TextInstance";
            Icon = new IconRuntime();
            Icon.Name = "Icon";
            FocusedIndicator = new NineSliceRuntime();
            FocusedIndicator.Name = "FocusedIndicator";
        }
        protected virtual void AssignParents()
        {
            this.Children.Add(Background);
            this.Children.Add(TextInstance);
            this.Children.Add(Icon);
            this.Children.Add(FocusedIndicator);
        }
        private void ApplyDefaultVariables()
        {
Background.SetProperty("ColorCategoryState", "Primary");
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

TextInstance.SetProperty("ColorCategoryState", "White");
TextInstance.SetProperty("StyleCategoryState", "Strong");
            this.TextInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.TextInstance.HorizontalAlignment = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            this.TextInstance.VerticalAlignment = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.TextInstance.Width = -32f;
            this.TextInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.TextInstance.X = 32f;

Icon.SetProperty("IconColor", "PrimaryDark");

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
