//Code for Controls/KeyboardKey (Container)
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
    public partial class KeyboardKeyRuntime:ContainerRuntime
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/KeyboardKey", typeof(KeyboardKeyRuntime));
        }
        public MonoGameGum.Forms.Controls.Button FormsControl => FormsControlAsObject as MonoGameGum.Forms.Controls.Button;
        public enum ButtonCategory
        {
            Enabled,
            Disabled,
            Highlighted,
            Pushed,
            Focused,
            HighlightedFocused,
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
                            TextInstance.SetProperty("ColorCategoryState", "White");
                            break;
                        case ButtonCategory.Disabled:
                            Background.SetProperty("ColorCategoryState", "DarkGray");
                            this.FocusedIndicator.Visible = false;
                            TextInstance.SetProperty("ColorCategoryState", "Gray");
                            break;
                        case ButtonCategory.Highlighted:
                            Background.SetProperty("ColorCategoryState", "PrimaryLight");
                            this.FocusedIndicator.Visible = false;
                            TextInstance.SetProperty("ColorCategoryState", "White");
                            break;
                        case ButtonCategory.Pushed:
                            Background.SetProperty("ColorCategoryState", "PrimaryDark");
                            this.FocusedIndicator.Visible = false;
                            TextInstance.SetProperty("ColorCategoryState", "White");
                            break;
                        case ButtonCategory.Focused:
                            Background.SetProperty("ColorCategoryState", "Primary");
                            this.FocusedIndicator.Visible = true;
                            TextInstance.SetProperty("ColorCategoryState", "White");
                            break;
                        case ButtonCategory.HighlightedFocused:
                            Background.SetProperty("ColorCategoryState", "PrimaryLight");
                            this.FocusedIndicator.Visible = true;
                            TextInstance.SetProperty("ColorCategoryState", "White");
                            break;
                        case ButtonCategory.DisabledFocused:
                            Background.SetProperty("ColorCategoryState", "DarkGray");
                            this.FocusedIndicator.Visible = true;
                            TextInstance.SetProperty("ColorCategoryState", "Gray");
                            break;
                    }
                }
            }
        }
        public NineSliceRuntime Background { get; protected set; }
        public TextRuntime TextInstance { get; protected set; }
        public NineSliceRuntime FocusedIndicator { get; protected set; }

        public string Text
        {
            get => TextInstance.Text;
            set => TextInstance.Text = value;
        }

        public KeyboardKeyRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {
            if(fullInstantiation)
            {
            }

             
            this.Height = 20f;
             
             
            this.Width = 20f;

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
            FocusedIndicator = new NineSliceRuntime();
            FocusedIndicator.Name = "FocusedIndicator";
        }
        protected virtual void AssignParents()
        {
            this.Children.Add(Background);
            this.Children.Add(TextInstance);
            this.Children.Add(FocusedIndicator);
        }
        private void ApplyDefaultVariables()
        {
Background.SetProperty("ColorCategoryState", "Primary");
Background.SetProperty("StyleCategoryState", "Bordered");
            this.Background.Height = -2f;
            this.Background.Width = -2f;

TextInstance.SetProperty("ColorCategoryState", "White");
TextInstance.SetProperty("StyleCategoryState", "Normal");
            this.TextInstance.Height = 0f;
            this.TextInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.TextInstance.HorizontalAlignment = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.TextInstance.Text = @"A";
            this.TextInstance.VerticalAlignment = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.TextInstance.Width = 0f;
            this.TextInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.TextInstance.X = 0f;
            this.TextInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.TextInstance.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.TextInstance.Y = 0f;
            this.TextInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.TextInstance.YUnits = GeneralUnitType.PixelsFromMiddle;

FocusedIndicator.SetProperty("ColorCategoryState", "Warning");
FocusedIndicator.SetProperty("StyleCategoryState", "Solid");
            this.FocusedIndicator.Height = 2f;
            this.FocusedIndicator.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            this.FocusedIndicator.Visible = false;
            this.FocusedIndicator.Width = -4f;
            this.FocusedIndicator.Y = -4f;
            this.FocusedIndicator.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            this.FocusedIndicator.YUnits = GeneralUnitType.PixelsFromLarge;

        }
        partial void CustomInitialize();
    }
}
