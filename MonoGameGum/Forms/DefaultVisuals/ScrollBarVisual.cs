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
namespace Gum.Forms.DefaultVisuals;

public class ScrollBarVisual : InteractiveGue
{
    public ButtonVisual UpButtonInstance { get; private set; }
    public SpriteRuntime UpButtonIcon { get; private set; }
    public ButtonVisual DownButtonInstance { get; private set; }
    public SpriteRuntime DownButtonIcon { get; private set; }
    public ContainerRuntime ThumbContainer {  get; private set; }
    public NineSliceRuntime TrackInstance { get; private set; }
    public ButtonVisual ThumbInstance { get; private set; }


    public ScrollBarVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable()) 
    {
        Width = 24;
        WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        Height = 128;
        HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;

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
        UpButtonIcon.WidthUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
        UpButtonIcon.Height = 100;
        UpButtonIcon.HeightUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
        UpButtonIcon.ApplyState(Styling.ActiveStyle.Icons.Arrow1);
        UpButtonIcon.Color = Styling.ActiveStyle.Colors.White;
        UpButtonIcon.Texture = uiSpriteSheetTexture;
        UpButtonIcon.Visible = true;
        UpButtonIcon.Rotation = 90;

        UpButtonInstance = new ButtonVisual();
        UpButtonInstance.Name = "UpButtonInstance";
        UpButtonInstance.TextInstance.Text = "";
        UpButtonInstance.Height = 24f;
        UpButtonInstance.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        UpButtonInstance.Width = 0;
        UpButtonInstance.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        UpButtonInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
        UpButtonInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
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
        DownButtonIcon.WidthUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
        DownButtonIcon.Height = 100;
        DownButtonIcon.HeightUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
        DownButtonIcon.ApplyState(Styling.ActiveStyle.Icons.Arrow1);
        DownButtonIcon.Color = Styling.ActiveStyle.Colors.White;
        DownButtonIcon.Texture = uiSpriteSheetTexture;
        DownButtonIcon.Visible = true;
        DownButtonIcon.Rotation = -90;

        DownButtonInstance = new ButtonVisual();
        DownButtonInstance.Name = "DownButtonInstance";
        DownButtonInstance.TextInstance.Text = "";
        DownButtonInstance.Height = 24f;
        DownButtonInstance.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        DownButtonInstance.Width = 0f;
        DownButtonInstance.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        DownButtonInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
        DownButtonInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Bottom;
        DownButtonInstance.YUnits = GeneralUnitType.PixelsFromLarge;
        DownButtonInstance.AddChild(DownButtonIcon);
        this.AddChild(DownButtonInstance);

        ThumbContainer = new ContainerRuntime();
        ThumbContainer.Name = "ThumbContainer";
        ThumbContainer.Height = -48f;
        ThumbContainer.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        ThumbContainer.Width = 0f;
        ThumbContainer.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        ThumbContainer.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        ThumbContainer.XUnits = GeneralUnitType.PixelsFromMiddle;
        ThumbContainer.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        ThumbContainer.YUnits = GeneralUnitType.PixelsFromMiddle;
        this.AddChild(ThumbContainer);

        TrackInstance = new NineSliceRuntime();
        TrackInstance.Name = "TrackInstance";
        TrackInstance.Height = 0f;
        TrackInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        TrackInstance.Width = 0f;
        TrackInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        TrackInstance.X = 0f;
        TrackInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        TrackInstance.XUnits = GeneralUnitType.PixelsFromMiddle;
        TrackInstance.Y = 0f;
        TrackInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        TrackInstance.YUnits = GeneralUnitType.PixelsFromMiddle;
        TrackInstance.ApplyState(Styling.ActiveStyle.NineSlice.Solid);
        TrackInstance.Color = Styling.ActiveStyle.Colors.Gray;
        TrackInstance.Texture = uiSpriteSheetTexture;
        ThumbContainer.AddChild(TrackInstance);

        ThumbInstance = new ButtonVisual();
        ThumbInstance.Name = "ThumbInstance";
        ThumbInstance.TextInstance.Text = String.Empty;
        ThumbInstance.Width = 0f;
        ThumbInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        ThumbInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        ThumbInstance.XUnits = GeneralUnitType.PixelsFromMiddle;
        ThumbInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        ThumbInstance.YUnits = GeneralUnitType.PixelsFromMiddle;
        ThumbContainer.AddChild(ThumbInstance);

        var category = new StateSaveCategory();
        category.Name = "OrientationCategory";
        this.AddCategory(category);

        void AddVariable(StateSave state, string name, object value)
        {
            state.Variables.Add(new VariableSave
            {
                Name = name,
                Value = value
            });
        }

        var horizontalState = new StateSave();
        category.States.Add(horizontalState);
        horizontalState.Name = "Horizontal";
        AddVariable(horizontalState, 
            nameof(this.Height), 24f);
        AddVariable(horizontalState, 
            nameof(this.HeightUnits), DimensionUnitType.Absolute);
        AddVariable(horizontalState, nameof(this.Width), 
            128f);
        AddVariable(horizontalState, nameof(this.WidthUnits), 
            DimensionUnitType.Absolute);

        AddVariable(horizontalState,
            "UpButtonIcon.Rotation", 180f);

        AddVariable(horizontalState,
            "UpButtonInstance.XUnits", GeneralUnitType.PixelsFromSmall);
        AddVariable(horizontalState,
            "UpButtonInstance.YUnits", GeneralUnitType.PixelsFromMiddle);
        AddVariable(horizontalState,
            "UpButtonInstance.XOrigin",
            HorizontalAlignment.Left);
        AddVariable(horizontalState,
            "UpButtonInstance.YOrigin", 
            global::RenderingLibrary.Graphics.VerticalAlignment.Center);
        AddVariable(horizontalState, 
            "UpButtonInstance.Width", 24f);
        AddVariable(horizontalState,
            "UpButtonInstance.WidthUnits", DimensionUnitType.Absolute);

        AddVariable(horizontalState,
            "ThumbContainer.Height", 0f);
        AddVariable(horizontalState,
            "ThumbContainer.HeightUnits", DimensionUnitType.RelativeToParent);
        AddVariable(horizontalState,
            "ThumbContainer.Width", -48f);
        AddVariable(horizontalState,
            "ThumbContainer.WidthUnits", DimensionUnitType.RelativeToParent);

        AddVariable(horizontalState,
            "ThumbInstance.Height", 0f);
        AddVariable(horizontalState,
            "ThumbInstance.HeightUnits", DimensionUnitType.RelativeToParent);
        AddVariable(horizontalState,
            "ThumbInstance.Y", 0f);
        AddVariable(horizontalState,
            "ThumbInstance.YUnits", GeneralUnitType.PixelsFromMiddle);
        AddVariable(horizontalState,
            "ThumbInstance.YOrigin", VerticalAlignment.Center);

        AddVariable(horizontalState,
            "DownButtonIcon.Rotation", 0f);
        AddVariable(horizontalState,
            "DownButtonInstance.XUnits", GeneralUnitType.PixelsFromLarge);
        AddVariable(horizontalState,
            "DownButtonInstance.XOrigin", HorizontalAlignment.Right);
        AddVariable(horizontalState,
            "DownButtonInstance.Width", 24f);
        AddVariable(horizontalState,
            "DownButtonInstance.WidthUnits", DimensionUnitType.Absolute);

        var verticalState = new StateSave();
        category.States.Add(verticalState);
        verticalState.Name = "Vertical";
        AddVariable(verticalState,
            nameof(this.Height), 128f);
        AddVariable(verticalState,
            nameof(this.HeightUnits), DimensionUnitType.Absolute);
        AddVariable(verticalState,
            nameof(this.Width), 24f);
        AddVariable(verticalState,
            nameof(this.WidthUnits), DimensionUnitType.Absolute);

        AddVariable(verticalState,
            "UpButtonIcon.Rotation", 90f);

        AddVariable(verticalState,
            "UpButtonInstance.XUnits", GeneralUnitType.PixelsFromMiddle);
        AddVariable(verticalState,
            "UpButtonInstance.YUnits", GeneralUnitType.PixelsFromSmall);
        AddVariable(verticalState,
            "UpButtonInstance.XOrigin", 
            global::RenderingLibrary.Graphics.VerticalAlignment.Center);
        AddVariable(verticalState,
            "UpButtonInstance.YOrigin", 
            global::RenderingLibrary.Graphics.VerticalAlignment.Top);
        AddVariable(verticalState,
            "UpButtonInstance.Height", 24f);
        AddVariable(verticalState,
            "UpButtonInstance.HeightUnits", DimensionUnitType.Absolute);

        AddVariable(verticalState,
            "ThumbContainer.Height", -48f);
        AddVariable(verticalState,
            "ThumbContainer.HeightUnits", DimensionUnitType.RelativeToParent);
        AddVariable(verticalState,
            "ThumbContainer.Width", 0f);
        AddVariable(verticalState,
            "ThumbContainer.WidthtUnits", DimensionUnitType.RelativeToParent);

        AddVariable(verticalState,
            "ThumbInstance.Width", 0f);
        AddVariable(verticalState,
            "ThumbInstance.WidthUnits", DimensionUnitType.RelativeToParent);
        AddVariable(verticalState,
            "ThumbInstance.X", 0f);
        AddVariable(verticalState,
            "ThumbInstance.XUnits", GeneralUnitType.PixelsFromMiddle);
        AddVariable(verticalState,
            "ThumbInstance.XOrigin", VerticalAlignment.Center);

        AddVariable(verticalState,
            "DownButtonIcon.Rotation", -90f);
        AddVariable(verticalState,
            "DownButtonInstance.YUnits", GeneralUnitType.PixelsFromLarge);
        AddVariable(verticalState,
            "DownButtonInstance.YOrigin", HorizontalAlignment.Right);
        AddVariable(verticalState,
            "DownButtonInstance.Height", 24f);
        AddVariable(verticalState,
            "DownButtonInstance.HeightUnits", DimensionUnitType.Absolute);

        if (tryCreateFormsObject)
        {
            this.FormsControlAsObject = new ScrollBar(this);
        }
    }

    public ScrollBar FormsControl => this.FormsControlAsObject as ScrollBar;
}
