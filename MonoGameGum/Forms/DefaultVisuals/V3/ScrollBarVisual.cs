using Gum.Converters;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gum.DataTypes.Variables;
using Gum.DataTypes;




#if RAYLIB
using Gum.GueDeriving;
using Raylib_cs;
#else
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum.GueDeriving;
#endif

using Gum.Forms.Controls;
namespace Gum.Forms.DefaultVisuals.V3;

public class ScrollBarVisual : InteractiveGue
{
    public ButtonVisual UpButtonInstance { get; private set; }
    public SpriteRuntime UpButtonIcon { get; private set; }
    public ButtonVisual DownButtonInstance { get; private set; }
    public SpriteRuntime DownButtonIcon { get; private set; }
    public ContainerRuntime ThumbContainer {  get; private set; }
    public NineSliceRuntime TrackInstance { get; private set; }
    public ButtonVisual ThumbInstance { get; private set; }

    public class ScrollBarStates
    {
        public OrientationStates OrientationStates { get; set; } = new();
    }

    public class OrientationStates
    {
        public StateSave Vertical { get; set; } = new StateSave() { Name = FrameworkElement.VerticalStateName };
        public StateSave Horizontal { get; set; } = new StateSave() { Name = FrameworkElement.HorizontalStateName };
    }

    public ScrollBarStates States;

    public StateSaveCategory OrientationCategory { get; private set; }
    public StateSaveCategory ScrollBarCategory { get; private set; }

    Color _trackBackgroundColor;
    public Color TrackBackgroundColor
    {
        get => _trackBackgroundColor;
        set
        {
            if (!value.Equals(_trackBackgroundColor))
            {
                // Just in case FormsControl hasn't been set yet, do ?. to check for null
                // UpdateState forcefully applies the current state, so it will work regardless of whether this is
                // Highlighted or Disabled etc
                _trackBackgroundColor = value;
                FormsControl?.UpdateState();
            }
        }
    }

    Color _scrollArrowColor;
    public Color ScrollArrowColor
    {
        get => _scrollArrowColor;
        set
        {
            if (!value.Equals(_scrollArrowColor))
            {
                _scrollArrowColor = value;
                FormsControl?.UpdateState();
            }
        }
    }

    public ScrollBarVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable()) 
    {
        this.HasEvents = true;

        // These values change depending on vertical or horizontal Orientation category state
        Width = 24;
        WidthUnits = DimensionUnitType.Absolute; 
        Height = 128;
        HeightUnits = DimensionUnitType.Absolute;

        States = new ScrollBarStates();

        var uiSpriteSheetTexture = Styling.ActiveStyle.SpriteSheet;

        UpButtonIcon = new SpriteRuntime();
        UpButtonIcon.Name = "UpButtonIcon";
        UpButtonIcon.X = 0f;
        UpButtonIcon.XUnits = GeneralUnitType.PixelsFromMiddle;
        UpButtonIcon.Y = 0f;
        UpButtonIcon.YUnits = GeneralUnitType.PixelsFromMiddle;
        UpButtonIcon.XOrigin = HorizontalAlignment.Center;
        UpButtonIcon.YOrigin = VerticalAlignment.Center;
        UpButtonIcon.Width = 100;
        UpButtonIcon.WidthUnits = DimensionUnitType.PercentageOfSourceFile;
        UpButtonIcon.Height = 100;
        UpButtonIcon.HeightUnits = DimensionUnitType.PercentageOfSourceFile;
        UpButtonIcon.ApplyState(Styling.ActiveStyle.Icons.Arrow1);
        UpButtonIcon.Texture = uiSpriteSheetTexture;
        UpButtonIcon.Visible = true;
        UpButtonIcon.Rotation = 90;

        UpButtonInstance = new ButtonVisual();
        UpButtonInstance.Name = "UpButtonInstance";
        UpButtonInstance.TextInstance.Text = "";
        UpButtonInstance.Height = 24f;
        UpButtonInstance.HeightUnits = DimensionUnitType.Absolute;
        UpButtonInstance.Width = 0;
        UpButtonInstance.WidthUnits = DimensionUnitType.RelativeToParent;
        UpButtonInstance.XOrigin = HorizontalAlignment.Left;
        UpButtonInstance.YOrigin = VerticalAlignment.Top;
        UpButtonInstance.AddChild(UpButtonIcon);
        this.AddChild(UpButtonInstance);

        DownButtonIcon = new SpriteRuntime();
        DownButtonIcon.Name = "DownButtonIcon";
        DownButtonIcon.X = 0f;
        DownButtonIcon.XUnits = GeneralUnitType.PixelsFromMiddle;
        DownButtonIcon.Y = 0f;
        DownButtonIcon.YUnits = GeneralUnitType.PixelsFromMiddle;
        DownButtonIcon.XOrigin = HorizontalAlignment.Center;
        DownButtonIcon.YOrigin = VerticalAlignment.Center;
        DownButtonIcon.Width = 100;
        DownButtonIcon.WidthUnits = DimensionUnitType.PercentageOfSourceFile;
        DownButtonIcon.Height = 100;
        DownButtonIcon.HeightUnits = DimensionUnitType.PercentageOfSourceFile;
        DownButtonIcon.ApplyState(Styling.ActiveStyle.Icons.Arrow1);
        DownButtonIcon.Texture = uiSpriteSheetTexture;
        DownButtonIcon.Visible = true;
        DownButtonIcon.Rotation = -90;

        DownButtonInstance = new ButtonVisual();
        DownButtonInstance.Name = "DownButtonInstance";
        DownButtonInstance.TextInstance.Text = "";
        DownButtonInstance.Height = 24f;
        DownButtonInstance.HeightUnits = DimensionUnitType.Absolute;
        DownButtonInstance.Width = 0f;
        DownButtonInstance.WidthUnits = DimensionUnitType.RelativeToParent;
        DownButtonInstance.XOrigin = HorizontalAlignment.Left;
        DownButtonInstance.YOrigin = VerticalAlignment.Bottom;
        DownButtonInstance.YUnits = GeneralUnitType.PixelsFromLarge;
        DownButtonInstance.AddChild(DownButtonIcon);
        this.AddChild(DownButtonInstance);

        ThumbContainer = new ContainerRuntime();
        ThumbContainer.Name = "ThumbContainer";
        ThumbContainer.Height = -48f;
        ThumbContainer.HeightUnits = DimensionUnitType.RelativeToParent;
        ThumbContainer.Width = 0f;
        ThumbContainer.WidthUnits = DimensionUnitType.RelativeToParent;
        ThumbContainer.XOrigin = HorizontalAlignment.Center;
        ThumbContainer.XUnits = GeneralUnitType.PixelsFromMiddle;
        ThumbContainer.YOrigin = VerticalAlignment.Center;
        ThumbContainer.YUnits = GeneralUnitType.PixelsFromMiddle;
        this.AddChild(ThumbContainer);

        TrackInstance = new NineSliceRuntime();
        TrackInstance.Name = "TrackInstance";
        TrackInstance.Height = 0f;
        TrackInstance.HeightUnits = DimensionUnitType.RelativeToParent;
        TrackInstance.Width = 0f;
        TrackInstance.WidthUnits = DimensionUnitType.RelativeToParent;
        TrackInstance.X = 0f;
        TrackInstance.XOrigin = HorizontalAlignment.Center;
        TrackInstance.XUnits = GeneralUnitType.PixelsFromMiddle;
        TrackInstance.Y = 0f;
        TrackInstance.YOrigin = VerticalAlignment.Center;
        TrackInstance.YUnits = GeneralUnitType.PixelsFromMiddle;
        TrackInstance.ApplyState(Styling.ActiveStyle.NineSlice.Solid);
        TrackInstance.Texture = uiSpriteSheetTexture;
        ThumbContainer.AddChild(TrackInstance);

        ThumbInstance = new ButtonVisual();
        ThumbInstance.Name = "ThumbInstance";
        ThumbInstance.TextInstance.Text = String.Empty;
        ThumbInstance.Width = 0f;
        ThumbInstance.WidthUnits = DimensionUnitType.RelativeToParent;
        ThumbInstance.XOrigin = HorizontalAlignment.Center;
        ThumbInstance.XUnits = GeneralUnitType.PixelsFromMiddle;
        ThumbInstance.YOrigin = VerticalAlignment.Center;
        ThumbInstance.YUnits = GeneralUnitType.PixelsFromMiddle;
        ThumbContainer.AddChild(ThumbInstance);

        OrientationCategory = new StateSaveCategory();
        OrientationCategory.Name = "OrientationCategory";
        this.AddCategory(OrientationCategory);

        TrackBackgroundColor = Styling.ActiveStyle.Colors.SurfaceVariant;
        ScrollArrowColor = Styling.ActiveStyle.Colors.IconDefault;

        OrientationCategory.States.Add(States.OrientationStates.Horizontal);

        States.OrientationStates.Horizontal.Apply = () =>
        {
            Height = 24f;
            HeightUnits =DimensionUnitType.Absolute;
            Width = 128f;
            WidthUnits = DimensionUnitType.Absolute;

            UpButtonIcon.Rotation = 180f;

            UpButtonInstance.XUnits = GeneralUnitType.PixelsFromSmall;
            UpButtonInstance.YUnits = GeneralUnitType.PixelsFromMiddle;
            UpButtonInstance.XOrigin = HorizontalAlignment.Left;
            UpButtonInstance.YOrigin = VerticalAlignment.Center;
            UpButtonInstance.Width = 24f;
            UpButtonInstance.WidthUnits = DimensionUnitType.Absolute;

            ThumbContainer.Height = 0f;
            ThumbContainer.HeightUnits = DimensionUnitType.RelativeToParent;
            ThumbContainer.Width = -48f;
            ThumbContainer.WidthUnits = DimensionUnitType.RelativeToParent;

            ThumbInstance.Height = 0f;
            ThumbInstance.HeightUnits = DimensionUnitType.RelativeToParent;
            ThumbInstance.Y = 0f;
            ThumbInstance.YUnits = GeneralUnitType.PixelsFromMiddle;
            ThumbInstance.YOrigin = VerticalAlignment.Center;

            
            DownButtonIcon.Rotation = 0f;
            DownButtonInstance.XUnits = GeneralUnitType.PixelsFromLarge;
            DownButtonInstance.XOrigin = HorizontalAlignment.Right;
            DownButtonInstance.Width = 24f;
            DownButtonInstance.WidthUnits = DimensionUnitType.Absolute;

            // Local Styling colors
            UpButtonIcon.Color = ScrollArrowColor;
            DownButtonIcon.Color = ScrollArrowColor;
            TrackInstance.Color = TrackBackgroundColor;
        };
        

        OrientationCategory.States.Add(States.OrientationStates.Vertical);
        States.OrientationStates.Vertical.Apply = () =>
        {
            Height = 128f;
            HeightUnits = DimensionUnitType.Absolute;
            Width = 24f;
            WidthUnits = DimensionUnitType.Absolute;

            UpButtonIcon.Color = Styling.ActiveStyle.Colors.IconDefault;
            UpButtonIcon.Rotation = 90f;

            UpButtonInstance.XUnits = GeneralUnitType.PixelsFromMiddle;
            UpButtonInstance.YUnits = GeneralUnitType.PixelsFromSmall;
            UpButtonInstance.XOrigin = HorizontalAlignment.Center;
            UpButtonInstance.YOrigin = VerticalAlignment.Top;
            UpButtonInstance.Height = 24f;
            UpButtonInstance.HeightUnits = DimensionUnitType.Absolute;

            ThumbContainer.Height = -48f;
            ThumbContainer.HeightUnits = DimensionUnitType.RelativeToParent;
            ThumbContainer.Width = 0f;
            ThumbContainer.WidthUnits = DimensionUnitType.RelativeToParent;

            ThumbInstance.Width = 0f;
            ThumbInstance.WidthUnits = DimensionUnitType.RelativeToParent;
            ThumbInstance.X = 0f;
            ThumbInstance.XUnits = GeneralUnitType.PixelsFromMiddle;
            ThumbInstance.XOrigin = HorizontalAlignment.Center;

            DownButtonIcon.Color = Styling.ActiveStyle.Colors.IconDefault;
            DownButtonIcon.Rotation = -90f;
            DownButtonInstance.YUnits = GeneralUnitType.PixelsFromLarge;
            DownButtonInstance.YOrigin = VerticalAlignment.Bottom;
            DownButtonInstance.Height = 24f;
            DownButtonInstance.HeightUnits = DimensionUnitType.Absolute;

            // Local Styling colors
            UpButtonIcon.Color = ScrollArrowColor;
            DownButtonIcon.Color = ScrollArrowColor;
            TrackInstance.Color = TrackBackgroundColor;
        };      


        if (tryCreateFormsObject)
        {
            this.FormsControlAsObject = new ScrollBar(this);
        }
    }

    public ScrollBar FormsControl => (ScrollBar)this.FormsControlAsObject;
}
