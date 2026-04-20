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




#if XNALIKE
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum.GueDeriving;
#else
using Gum.GueDeriving;
#endif

using Gum.Forms.Controls;
namespace Gum.Forms.DefaultVisuals.V3;

/// <summary>
/// Default V3 visual for a ScrollBar control. Contains up/down (or left/right) arrow buttons,
/// a track, and a draggable thumb. Supports both vertical and horizontal orientations via
/// the OrientationCategory states.
/// </summary>
public class ScrollBarVisual : InteractiveGue
{
    /// <summary>
    /// The button at the start of the scroll bar (top in vertical, left in horizontal orientation).
    /// </summary>
    public ButtonVisual? UpButtonInstance { get; private set; }

    /// <summary>
    /// The arrow icon sprite inside the up/left button.
    /// </summary>
    public SpriteRuntime UpButtonIcon { get; private set; }

    /// <summary>
    /// The button at the end of the scroll bar (bottom in vertical, right in horizontal orientation).
    /// </summary>
    public ButtonVisual? DownButtonInstance { get; private set; }

    /// <summary>
    /// The arrow icon sprite inside the down/right button.
    /// </summary>
    public SpriteRuntime DownButtonIcon { get; private set; }

    /// <summary>
    /// The container between the up and down buttons that holds the track and draggable thumb.
    /// </summary>
    public ContainerRuntime ThumbContainer {  get; private set; }

    /// <summary>
    /// The track nine-slice background behind the thumb.
    /// </summary>
    public NineSliceRuntime TrackInstance { get; private set; }

    /// <summary>
    /// The draggable thumb button used to scroll content.
    /// </summary>
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

    /// <summary>
    /// The state category controlling vertical vs horizontal layout.
    /// </summary>
    public StateSaveCategory OrientationCategory { get; private set; }

    /// <summary>
    /// The state category used by the Forms control to apply visual states.
    /// </summary>
    public StateSaveCategory ScrollBarCategory { get; private set; }

    Color _trackBackgroundColor;
    /// <summary>
    /// The color applied to the track background. Setting this value immediately updates the visual.
    /// </summary>
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
    /// <summary>
    /// The color applied to the up and down arrow icons. Setting this value immediately updates the visual.
    /// </summary>
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

        var upButton = new Button();
        var upButtonVisual = upButton.Visual;

        upButtonVisual.Name = "UpButtonInstance";
        upButton.Text = "";
        upButtonVisual.Height = 24f;
        upButtonVisual.HeightUnits = DimensionUnitType.Absolute;
        upButtonVisual.Width = 0;
        upButtonVisual.WidthUnits = DimensionUnitType.RelativeToParent;
        upButtonVisual.XOrigin = HorizontalAlignment.Left;
        upButtonVisual.YOrigin = VerticalAlignment.Top;
        upButtonVisual.AddChild(UpButtonIcon);
        this.AddChild(upButtonVisual);
        this.UpButtonInstance = upButtonVisual as ButtonVisual;


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

        var downButton = new Button();
        var downButtonVisual = downButton.Visual;

        downButtonVisual.Name = "DownButtonInstance";
        downButton.Text = "";
        downButtonVisual.Height = 24f;
        downButtonVisual.HeightUnits = DimensionUnitType.Absolute;
        downButtonVisual.Width = 0f;
        downButtonVisual.WidthUnits = DimensionUnitType.RelativeToParent;
        downButtonVisual.XOrigin = HorizontalAlignment.Left;
        downButtonVisual.YOrigin = VerticalAlignment.Bottom;
        downButtonVisual.YUnits = GeneralUnitType.PixelsFromLarge;
        downButtonVisual.AddChild(DownButtonIcon);
        this.AddChild(downButtonVisual);
        this.DownButtonInstance = downButtonVisual as ButtonVisual;

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
        ThumbContainer.HasEvents = true;
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
        TrackInstance.HasEvents = false;
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

            upButtonVisual.XUnits = GeneralUnitType.PixelsFromSmall;
            upButtonVisual.YUnits = GeneralUnitType.PixelsFromMiddle;
            upButtonVisual.XOrigin = HorizontalAlignment.Left;
            upButtonVisual.YOrigin = VerticalAlignment.Center;
            upButtonVisual.Width = 24f;
            upButtonVisual.WidthUnits = DimensionUnitType.Absolute;

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
            downButtonVisual.XUnits = GeneralUnitType.PixelsFromLarge;
            downButtonVisual.XOrigin = HorizontalAlignment.Right;
            downButtonVisual.Width = 24f;
            downButtonVisual.WidthUnits = DimensionUnitType.Absolute;

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

            upButtonVisual.XUnits = GeneralUnitType.PixelsFromMiddle;
            upButtonVisual.YUnits = GeneralUnitType.PixelsFromSmall;
            upButtonVisual.XOrigin = HorizontalAlignment.Center;
            upButtonVisual.YOrigin = VerticalAlignment.Top;
            upButtonVisual.Height = 24f;
            upButtonVisual.HeightUnits = DimensionUnitType.Absolute;

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
            downButtonVisual.YUnits = GeneralUnitType.PixelsFromLarge;
            downButtonVisual.YOrigin = VerticalAlignment.Bottom;
            downButtonVisual.Height = 24f;
            downButtonVisual.HeightUnits = DimensionUnitType.Absolute;

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

    /// <summary>
    /// Returns the strongly-typed ScrollBar Forms control backing this visual.
    /// </summary>
    public ScrollBar FormsControl => (ScrollBar)this.FormsControlAsObject;
}
