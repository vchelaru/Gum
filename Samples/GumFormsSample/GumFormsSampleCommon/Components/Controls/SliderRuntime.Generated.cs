//Code for Controls/Slider (Container)
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
    public partial class SliderRuntime:ContainerRuntime
    {
        public MonoGameGum.Forms.Controls.Slider FormsControl => FormsControlAsObject as MonoGameGum.Forms.Controls.Slider;
        public enum SliderCategory
        {
            Enabled,
            Focused,
            Highlighted,
            HighlightedFocused,
        }

        SliderCategory mSliderCategoryState;
        public SliderCategory SliderCategoryState
        {
            get => mSliderCategoryState;
            set
            {
                mSliderCategoryState = value;
                var appliedDynamically = false;
                if(!appliedDynamically)
                {
                    switch (value)
                    {
                        case SliderCategory.Enabled:
                            this.FocusedIndicator.Visible = false;
                            this.ThumbInstance.ButtonCategoryState = ButtonStandardRuntime.ButtonCategory.Enabled;
                            break;
                        case SliderCategory.Focused:
                            this.FocusedIndicator.Visible = true;
                            this.ThumbInstance.ButtonCategoryState = ButtonStandardRuntime.ButtonCategory.Enabled;
                            break;
                        case SliderCategory.Highlighted:
                            this.FocusedIndicator.Visible = false;
                            this.ThumbInstance.ButtonCategoryState = ButtonStandardRuntime.ButtonCategory.Highlighted;
                            break;
                        case SliderCategory.HighlightedFocused:
                            this.FocusedIndicator.Visible = true;
                            this.ThumbInstance.ButtonCategoryState = ButtonStandardRuntime.ButtonCategory.Highlighted;
                            break;
                    }
                }
            }
        }
        public ContainerRuntime TrackInstance { get; protected set; }
        public NineSliceRuntime NineSliceInstance { get; protected set; }
        public ButtonStandardRuntime ThumbInstance { get; protected set; }
        public NineSliceRuntime FocusedIndicator { get; protected set; }

        public float SliderPercent
        {
            get => ThumbInstance.X;
            set => ThumbInstance.X = value;
        }

        public SliderRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {
            if(fullInstantiation)
            {
            }

            this.Height = 24f;
             
            this.Width = 128f;

            InitializeInstances();

            ApplyDefaultVariables();
            AssignParents();
            CustomInitialize();
        }
        protected virtual void InitializeInstances()
        {
            TrackInstance = new ContainerRuntime();
            TrackInstance.Name = "TrackInstance";
            NineSliceInstance = new NineSliceRuntime();
            NineSliceInstance.Name = "NineSliceInstance";
            ThumbInstance = new ButtonStandardRuntime();
            ThumbInstance.Name = "ThumbInstance";
            FocusedIndicator = new NineSliceRuntime();
            FocusedIndicator.Name = "FocusedIndicator";
        }
        protected virtual void AssignParents()
        {
            this.Children.Add(TrackInstance);
            TrackInstance.Children.Add(NineSliceInstance);
            TrackInstance.Children.Add(ThumbInstance);
            this.Children.Add(FocusedIndicator);
        }
        private void ApplyDefaultVariables()
        {
            this.TrackInstance.Height = 0f;
            this.TrackInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.TrackInstance.Width = -32f;
            this.TrackInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.TrackInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.TrackInstance.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.TrackInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.TrackInstance.YUnits = GeneralUnitType.PixelsFromMiddle;

NineSliceInstance.SetProperty("ColorCategoryState", "DarkGray");
NineSliceInstance.SetProperty("StyleCategoryState", "Bordered");
            this.NineSliceInstance.Height = 8f;
            this.NineSliceInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            this.NineSliceInstance.Width = 0f;

            this.ThumbInstance.ButtonDisplayText = @"";
            this.ThumbInstance.Height = 24f;
            this.ThumbInstance.Width = 32f;
            this.ThumbInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.ThumbInstance.XUnits = GeneralUnitType.Percentage;
            this.ThumbInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.ThumbInstance.YUnits = GeneralUnitType.PixelsFromMiddle;

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
