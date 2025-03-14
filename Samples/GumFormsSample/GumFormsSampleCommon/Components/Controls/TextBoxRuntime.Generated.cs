//Code for Controls/TextBox (Container)
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
    public partial class TextBoxRuntime:ContainerRuntime
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/TextBox", typeof(TextBoxRuntime));
        }
        public MonoGameGum.Forms.Controls.TextBox FormsControl => FormsControlAsObject as MonoGameGum.Forms.Controls.TextBox;
        public enum TextBoxCategory
        {
            Enabled,
            Disabled,
            Highlighted,
            Selected,
        }
        public enum LineModeCategory
        {
            Single,
            Multi,
        }

        TextBoxCategory mTextBoxCategoryState;
        public TextBoxCategory TextBoxCategoryState
        {
            get => mTextBoxCategoryState;
            set
            {
                mTextBoxCategoryState = value;
                var appliedDynamically = false;
                if(!appliedDynamically)
                {
                    switch (value)
                    {
                        case TextBoxCategory.Enabled:
                            Background.SetProperty("ColorCategoryState", "DarkGray");
                            this.FocusedIndicator.Visible = false;
                            PlaceholderTextInstance.SetProperty("ColorCategoryState", "Gray");
                            TextInstance.SetProperty("ColorCategoryState", "White");
                            break;
                        case TextBoxCategory.Disabled:
                            Background.SetProperty("ColorCategoryState", "DarkGray");
                            this.FocusedIndicator.Visible = false;
                            PlaceholderTextInstance.SetProperty("ColorCategoryState", "Gray");
                            TextInstance.SetProperty("ColorCategoryState", "Gray");
                            break;
                        case TextBoxCategory.Highlighted:
                            Background.SetProperty("ColorCategoryState", "Gray");
                            this.FocusedIndicator.Visible = false;
                            PlaceholderTextInstance.SetProperty("ColorCategoryState", "DarkGray");
                            TextInstance.SetProperty("ColorCategoryState", "White");
                            break;
                        case TextBoxCategory.Selected:
                            Background.SetProperty("ColorCategoryState", "DarkGray");
                            this.FocusedIndicator.Visible = true;
                            PlaceholderTextInstance.SetProperty("ColorCategoryState", "Gray");
                            TextInstance.SetProperty("ColorCategoryState", "White");
                            break;
                    }
                }
            }
        }

        LineModeCategory mLineModeCategoryState;
        public LineModeCategory LineModeCategoryState
        {
            get => mLineModeCategoryState;
            set
            {
                mLineModeCategoryState = value;
                var appliedDynamically = false;
                if(!appliedDynamically)
                {
                    switch (value)
                    {
                        case LineModeCategory.Single:
                            break;
                        case LineModeCategory.Multi:
                            break;
                    }
                }
            }
        }
        public NineSliceRuntime Background { get; protected set; }
        public NineSliceRuntime SelectionInstance { get; protected set; }
        public TextRuntime TextInstance { get; protected set; }
        public TextRuntime PlaceholderTextInstance { get; protected set; }
        public NineSliceRuntime FocusedIndicator { get; protected set; }
        public SpriteRuntime CaretInstance { get; protected set; }

        public string PlaceholderText
        {
            get => PlaceholderTextInstance.Text;
            set => PlaceholderTextInstance.Text = value;
        }

        public TextBoxRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {
            if(fullInstantiation)
            {
            }

            this.ClipsChildren = true;
            this.Height = 24f;
             
            this.Width = 256f;

            InitializeInstances();

            ApplyDefaultVariables();
            AssignParents();
            if(tryCreateFormsObject)
            {
                if (FormsControl == null)
                {
                    FormsControlAsObject = new MonoGameGum.Forms.Controls.TextBox(this);
                }
            }
            CustomInitialize();
        }
        protected virtual void InitializeInstances()
        {
            Background = new NineSliceRuntime();
            Background.Name = "Background";
            SelectionInstance = new NineSliceRuntime();
            SelectionInstance.Name = "SelectionInstance";
            TextInstance = new TextRuntime();
            TextInstance.Name = "TextInstance";
            PlaceholderTextInstance = new TextRuntime();
            PlaceholderTextInstance.Name = "PlaceholderTextInstance";
            FocusedIndicator = new NineSliceRuntime();
            FocusedIndicator.Name = "FocusedIndicator";
            CaretInstance = new SpriteRuntime();
            CaretInstance.Name = "CaretInstance";
        }
        protected virtual void AssignParents()
        {
            this.Children.Add(Background);
            this.Children.Add(SelectionInstance);
            this.Children.Add(TextInstance);
            this.Children.Add(PlaceholderTextInstance);
            this.Children.Add(FocusedIndicator);
            this.Children.Add(CaretInstance);
        }
        private void ApplyDefaultVariables()
        {
Background.SetProperty("ColorCategoryState", "DarkGray");
Background.SetProperty("StyleCategoryState", "Bordered");

SelectionInstance.SetProperty("ColorCategoryState", "Accent");
            this.SelectionInstance.Height = -4f;
            this.SelectionInstance.Width = 7f;
            this.SelectionInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            this.SelectionInstance.X = 15f;
            this.SelectionInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            this.SelectionInstance.XUnits = GeneralUnitType.PixelsFromSmall;
            this.SelectionInstance.Y = 0f;

TextInstance.SetProperty("ColorCategoryState", "White");
TextInstance.SetProperty("StyleCategoryState", "Normal");
            this.TextInstance.Height = -4f;
            this.TextInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.TextInstance.HorizontalAlignment = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            this.TextInstance.Text = @"";
            this.TextInstance.VerticalAlignment = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.TextInstance.Width = 0f;
            this.TextInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.TextInstance.X = 4f;
            this.TextInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            this.TextInstance.XUnits = GeneralUnitType.PixelsFromSmall;
            this.TextInstance.Y = 0f;
            this.TextInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.TextInstance.YUnits = GeneralUnitType.PixelsFromMiddle;

PlaceholderTextInstance.SetProperty("ColorCategoryState", "Gray");
            this.PlaceholderTextInstance.Height = -4f;
            this.PlaceholderTextInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.PlaceholderTextInstance.Text = @"Text Placeholder";
            this.PlaceholderTextInstance.VerticalAlignment = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.PlaceholderTextInstance.Width = -8f;
            this.PlaceholderTextInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.PlaceholderTextInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.PlaceholderTextInstance.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.PlaceholderTextInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.PlaceholderTextInstance.YUnits = GeneralUnitType.PixelsFromMiddle;

FocusedIndicator.SetProperty("ColorCategoryState", "Warning");
FocusedIndicator.SetProperty("StyleCategoryState", "Solid");
            this.FocusedIndicator.Height = 2f;
            this.FocusedIndicator.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            this.FocusedIndicator.Visible = false;
            this.FocusedIndicator.Y = 2f;
            this.FocusedIndicator.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            this.FocusedIndicator.YUnits = GeneralUnitType.PixelsFromLarge;

CaretInstance.SetProperty("ColorCategoryState", "Primary");
            this.CaretInstance.Height = 14f;
            this.CaretInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            this.CaretInstance.SourceFileName = @"UISpriteSheet.png";
            this.CaretInstance.TextureAddress = global::Gum.Managers.TextureAddress.Custom;
            this.CaretInstance.TextureHeight = 24;
            this.CaretInstance.TextureLeft = 0;
            this.CaretInstance.TextureTop = 48;
            this.CaretInstance.TextureWidth = 24;
            this.CaretInstance.Width = 1f;
            this.CaretInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            this.CaretInstance.X = 4f;
            this.CaretInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            this.CaretInstance.XUnits = GeneralUnitType.PixelsFromSmall;
            this.CaretInstance.Y = 0f;
            this.CaretInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.CaretInstance.YUnits = GeneralUnitType.PixelsFromMiddle;

        }
        partial void CustomInitialize();
    }
}
