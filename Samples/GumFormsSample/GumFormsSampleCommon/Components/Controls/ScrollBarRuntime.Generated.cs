//Code for Controls/ScrollBar (Container)
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
    public partial class ScrollBarRuntime:ContainerRuntime
    {
        public MonoGameGum.Forms.Controls.ScrollBar FormsControl => FormsControlAsObject as MonoGameGum.Forms.Controls.ScrollBar;
        public enum ScrollBarCategory
        {
        }

        ScrollBarCategory mScrollBarCategoryState;
        public ScrollBarCategory ScrollBarCategoryState
        {
            get => mScrollBarCategoryState;
            set
            {
                mScrollBarCategoryState = value;
                var appliedDynamically = false;
                if(!appliedDynamically)
                {
                    switch (value)
                    {
                    }
                }
            }
        }
        public ButtonIconRuntime UpButtonInstance { get; protected set; }
        public ButtonIconRuntime DownButtonInstance { get; protected set; }
        public ContainerRuntime TrackInstance { get; protected set; }
        public NineSliceRuntime TrackBackground { get; protected set; }
        public ButtonStandardRuntime ThumbInstance { get; protected set; }

        public ScrollBarRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {
            if(fullInstantiation)
            {
            }

            this.Height = 0f;
            this.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
             
            this.Width = 24f;

            InitializeInstances();

            ApplyDefaultVariables();
            AssignParents();
            CustomInitialize();
        }
        protected virtual void InitializeInstances()
        {
            UpButtonInstance = new ButtonIconRuntime();
            UpButtonInstance.Name = "UpButtonInstance";
            DownButtonInstance = new ButtonIconRuntime();
            DownButtonInstance.Name = "DownButtonInstance";
            TrackInstance = new ContainerRuntime();
            TrackInstance.Name = "TrackInstance";
            TrackBackground = new NineSliceRuntime();
            TrackBackground.Name = "TrackBackground";
            ThumbInstance = new ButtonStandardRuntime();
            ThumbInstance.Name = "ThumbInstance";
        }
        protected virtual void AssignParents()
        {
            this.Children.Add(UpButtonInstance);
            this.Children.Add(DownButtonInstance);
            this.Children.Add(TrackInstance);
            TrackInstance.Children.Add(TrackBackground);
            TrackInstance.Children.Add(ThumbInstance);
        }
        private void ApplyDefaultVariables()
        {
this.UpButtonInstance.IconCategory = IconRuntime.IconCategory.Arrow1;
            this.UpButtonInstance.Height = 24f;
            this.UpButtonInstance.Rotation = 90f;
            this.UpButtonInstance.Width = 24f;
            this.UpButtonInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
            this.UpButtonInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;

this.DownButtonInstance.IconCategory = IconRuntime.IconCategory.Arrow1;
            this.DownButtonInstance.Height = 24f;
            this.DownButtonInstance.Rotation = -90f;
            this.DownButtonInstance.Width = 24f;
            this.DownButtonInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
            this.DownButtonInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Bottom;
            this.DownButtonInstance.YUnits = GeneralUnitType.PixelsFromLarge;

            this.TrackInstance.Height = -48f;
            this.TrackInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.TrackInstance.Width = 0f;
            this.TrackInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.TrackInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.TrackInstance.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.TrackInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.TrackInstance.YUnits = GeneralUnitType.PixelsFromMiddle;

TrackBackground.SetProperty("ColorCategoryState", "Gray");
TrackBackground.SetProperty("StyleCategoryState", "Solid");
            this.TrackBackground.Height = 0f;
            this.TrackBackground.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.TrackBackground.Width = 0f;
            this.TrackBackground.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.TrackBackground.X = 0f;
            this.TrackBackground.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.TrackBackground.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.TrackBackground.Y = 0f;
            this.TrackBackground.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.TrackBackground.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.ThumbInstance.ButtonDisplayText = @"";
            this.ThumbInstance.Width = 0f;
            this.ThumbInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.ThumbInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.ThumbInstance.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.ThumbInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.ThumbInstance.YUnits = GeneralUnitType.PixelsFromMiddle;

        }
        partial void CustomInitialize();
    }
}
