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

    Color _backgroundColor;
    public Color BackgroundColor
    {
        get => _backgroundColor;
        set
        {
            if (value != _backgroundColor)
            {
                // Just in case FormsControl hasn't been set yet, do ?. to check for null
                // UpdateState forcefully applies the current state, so it will work regardless of whether this is
                // Highlighted or Disabled etc
                _backgroundColor = value;
                (FormsControlAsObject as TextBoxBase)?.UpdateState();
            }
        }
    }
    Color _foregroundColor;
    public Color ForegroundColor
    {
        get => _foregroundColor;
        set
        {
            if (value != _foregroundColor)
            {
                // Just in case FormsControl hasn't been set yet, do ?. to check for null
                // UpdateState forcefully applies the current state, so it will work regardless of whether this is
                // Highlighted or Disabled etc
                _foregroundColor = value;
                (FormsControlAsObject as TextBoxBase)?.UpdateState();
            }
        }
    }

    public ScrollBarVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable()) 
    {
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
        UpButtonIcon.Color = Styling.ActiveStyle.Colors.IconDefault;
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
        DownButtonIcon.Color = Styling.ActiveStyle.Colors.IconDefault;
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
        TrackInstance.Color = Styling.ActiveStyle.Colors.SurfaceVariant;
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

        void AddVariable(StateSave state, string name, object value)
        {
            state.Variables.Add(new VariableSave
            {
                Name = name,
                Value = value
            });
        }


        OrientationCategory.States.Add(States.OrientationStates.Horizontal);
        AddVariable(States.OrientationStates.Horizontal, nameof(this.Height), 24f);
        AddVariable(States.OrientationStates.Horizontal, nameof(this.HeightUnits), DimensionUnitType.Absolute);
        AddVariable(States.OrientationStates.Horizontal, nameof(this.Width), 128f);
        AddVariable(States.OrientationStates.Horizontal, nameof(this.WidthUnits), DimensionUnitType.Absolute);

        AddVariable(States.OrientationStates.Horizontal, "UpButtonIcon.Rotation", 180f);

        AddVariable(States.OrientationStates.Horizontal, "UpButtonInstance.XUnits", GeneralUnitType.PixelsFromSmall);
        AddVariable(States.OrientationStates.Horizontal, "UpButtonInstance.YUnits", GeneralUnitType.PixelsFromMiddle);
        AddVariable(States.OrientationStates.Horizontal, "UpButtonInstance.XOrigin", HorizontalAlignment.Left);
        AddVariable(States.OrientationStates.Horizontal, "UpButtonInstance.YOrigin", VerticalAlignment.Center);
        AddVariable(States.OrientationStates.Horizontal, "UpButtonInstance.Width", 24f);
        AddVariable(States.OrientationStates.Horizontal, "UpButtonInstance.WidthUnits", DimensionUnitType.Absolute);

        AddVariable(States.OrientationStates.Horizontal, "ThumbContainer.Height", 0f);
        AddVariable(States.OrientationStates.Horizontal, "ThumbContainer.HeightUnits", DimensionUnitType.RelativeToParent);
        AddVariable(States.OrientationStates.Horizontal, "ThumbContainer.Width", -48f);
        AddVariable(States.OrientationStates.Horizontal, "ThumbContainer.WidthUnits", DimensionUnitType.RelativeToParent);

        AddVariable(States.OrientationStates.Horizontal, "ThumbInstance.Height", 0f);
        AddVariable(States.OrientationStates.Horizontal, "ThumbInstance.HeightUnits", DimensionUnitType.RelativeToParent);
        AddVariable(States.OrientationStates.Horizontal, "ThumbInstance.Y", 0f);
        AddVariable(States.OrientationStates.Horizontal, "ThumbInstance.YUnits", GeneralUnitType.PixelsFromMiddle);
        AddVariable(States.OrientationStates.Horizontal, "ThumbInstance.YOrigin", VerticalAlignment.Center);

        AddVariable(States.OrientationStates.Horizontal, "DownButtonIcon.Rotation", 0f);
        AddVariable(States.OrientationStates.Horizontal, "DownButtonInstance.XUnits", GeneralUnitType.PixelsFromLarge);
        AddVariable(States.OrientationStates.Horizontal,"DownButtonInstance.XOrigin", HorizontalAlignment.Right);
        AddVariable(States.OrientationStates.Horizontal, "DownButtonInstance.Width", 24f);
        AddVariable(States.OrientationStates.Horizontal, "DownButtonInstance.WidthUnits", DimensionUnitType.Absolute);

        OrientationCategory.States.Add(States.OrientationStates.Vertical);
        AddVariable(States.OrientationStates.Vertical, nameof(this.Height), 128f);
        AddVariable(States.OrientationStates.Vertical, nameof(this.HeightUnits), DimensionUnitType.Absolute);
        AddVariable(States.OrientationStates.Vertical, nameof(this.Width), 24f);
        AddVariable(States.OrientationStates.Vertical, nameof(this.WidthUnits), DimensionUnitType.Absolute);

        AddVariable(States.OrientationStates.Vertical, "UpButtonIcon.Rotation", 90f);

        AddVariable(States.OrientationStates.Vertical, "UpButtonInstance.XUnits", GeneralUnitType.PixelsFromMiddle);
        AddVariable(States.OrientationStates.Vertical, "UpButtonInstance.YUnits", GeneralUnitType.PixelsFromSmall);
        AddVariable(States.OrientationStates.Vertical, "UpButtonInstance.XOrigin", VerticalAlignment.Center);
        AddVariable(States.OrientationStates.Vertical, "UpButtonInstance.YOrigin", VerticalAlignment.Top);
        AddVariable(States.OrientationStates.Vertical, "UpButtonInstance.Height", 24f);
        AddVariable(States.OrientationStates.Vertical, "UpButtonInstance.HeightUnits", DimensionUnitType.Absolute);

        AddVariable(States.OrientationStates.Vertical, "ThumbContainer.Height", -48f);
        AddVariable(States.OrientationStates.Vertical, "ThumbContainer.HeightUnits", DimensionUnitType.RelativeToParent);
        AddVariable(States.OrientationStates.Vertical, "ThumbContainer.Width", 0f);
        AddVariable(States.OrientationStates.Vertical, "ThumbContainer.WidthtUnits", DimensionUnitType.RelativeToParent);

        AddVariable(States.OrientationStates.Vertical, "ThumbInstance.Width", 0f);
        AddVariable(States.OrientationStates.Vertical, "ThumbInstance.WidthUnits", DimensionUnitType.RelativeToParent);
        AddVariable(States.OrientationStates.Vertical, "ThumbInstance.X", 0f);
        AddVariable(States.OrientationStates.Vertical, "ThumbInstance.XUnits", GeneralUnitType.PixelsFromMiddle);
        AddVariable(States.OrientationStates.Vertical, "ThumbInstance.XOrigin", VerticalAlignment.Center);

        AddVariable(States.OrientationStates.Vertical, "DownButtonIcon.Rotation", -90f);
        AddVariable(States.OrientationStates.Vertical, "DownButtonInstance.YUnits", GeneralUnitType.PixelsFromLarge);
        AddVariable(States.OrientationStates.Vertical, "DownButtonInstance.YOrigin", HorizontalAlignment.Right);
        AddVariable(States.OrientationStates.Vertical, "DownButtonInstance.Height", 24f);
        AddVariable(States.OrientationStates.Vertical, "DownButtonInstance.HeightUnits", DimensionUnitType.Absolute);

        if (tryCreateFormsObject)
        {
            this.FormsControlAsObject = new ScrollBar(this);
        }
    }

    public ScrollBar FormsControl => this.FormsControlAsObject as ScrollBar;
}
