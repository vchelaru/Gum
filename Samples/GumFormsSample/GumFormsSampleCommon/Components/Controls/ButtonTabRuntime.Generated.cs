//Code for Controls/ButtonTab (Container)
using GumRuntime;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
namespace GumFormsSample.Components
{
    public partial class ButtonTabRuntime:ContainerRuntime
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/ButtonTab", typeof(ButtonTabRuntime));
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
                            TabText.SetProperty("ColorCategoryState", "White");
                            break;
                        case ButtonCategory.Disabled:
                            Background.SetProperty("ColorCategoryState", "DarkGray");
                            this.FocusedIndicator.Visible = false;
                            TabText.SetProperty("ColorCategoryState", "Gray");
                            break;
                        case ButtonCategory.Highlighted:
                            Background.SetProperty("ColorCategoryState", "PrimaryLight");
                            this.FocusedIndicator.Visible = false;
                            TabText.SetProperty("ColorCategoryState", "White");
                            break;
                        case ButtonCategory.Pushed:
                            Background.SetProperty("ColorCategoryState", "PrimaryDark");
                            this.FocusedIndicator.Visible = false;
                            TabText.SetProperty("ColorCategoryState", "White");
                            break;
                        case ButtonCategory.HighlightedFocused:
                            Background.SetProperty("ColorCategoryState", "PrimaryLight");
                            this.FocusedIndicator.Visible = true;
                            TabText.SetProperty("ColorCategoryState", "White");
                            break;
                        case ButtonCategory.Focused:
                            Background.SetProperty("ColorCategoryState", "Primary");
                            this.FocusedIndicator.Visible = true;
                            TabText.SetProperty("ColorCategoryState", "White");
                            break;
                        case ButtonCategory.DisabledFocused:
                            Background.SetProperty("ColorCategoryState", "DarkGray");
                            this.FocusedIndicator.Visible = true;
                            TabText.SetProperty("ColorCategoryState", "Gray");
                            break;
                    }
                }
            }
        }
        public NineSliceRuntime Background { get; protected set; }
        public TextRuntime TabText { get; protected set; }
        public NineSliceRuntime FocusedIndicator { get; protected set; }

        public string TabDisplayText
        {
            get => TabText.Text;
            set => TabText.Text = value;
        }

        public ButtonTabRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {
            if(fullInstantiation)
            {
            }

            this.Height = 32f;
             
            this.Width = 0f;
            this.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Bottom;

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
            TabText = new TextRuntime();
            TabText.Name = "TabText";
            FocusedIndicator = new NineSliceRuntime();
            FocusedIndicator.Name = "FocusedIndicator";
        }
        protected virtual void AssignParents()
        {
            this.Children.Add(Background);
            Background.Children.Add(TabText);
            Background.Children.Add(FocusedIndicator);
        }
        private void ApplyDefaultVariables()
        {
Background.SetProperty("ColorCategoryState", "Primary");
Background.SetProperty("StyleCategoryState", "TabBordered");
            this.Background.Width = 32f;
            this.Background.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;

TabText.SetProperty("ColorCategoryState", "White");
TabText.SetProperty("StyleCategoryState", "Strong");
            this.TabText.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.TabText.HorizontalAlignment = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.TabText.Text = @"Tab 1";
            this.TabText.VerticalAlignment = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.TabText.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.TabText.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.TabText.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.TabText.YUnits = GeneralUnitType.PixelsFromMiddle;

FocusedIndicator.SetProperty("ColorCategoryState", "Warning");
FocusedIndicator.SetProperty("StyleCategoryState", "Solid");
            this.FocusedIndicator.Height = 2f;
            this.FocusedIndicator.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            this.FocusedIndicator.Visible = false;
            this.FocusedIndicator.Width = -8f;
            this.FocusedIndicator.Y = 2f;
            this.FocusedIndicator.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            this.FocusedIndicator.YUnits = GeneralUnitType.PixelsFromLarge;

        }
        partial void CustomInitialize();
    }
}
