using Gum.Converters;
using Gum.DataTypes.Variables;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Gum.Forms.Controls;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;


namespace Gum.Forms.DefaultVisuals.V3;

public abstract class TextBoxBaseVisual : InteractiveGue
{
    public NineSliceRuntime Background { get; private set; }
    public ContainerRuntime ClipContainer { get; private set; }
    public NineSliceRuntime SelectionInstance { get; private set; }
    public TextRuntime TextInstance { get; private set; }
    public TextRuntime PlaceholderTextInstance { get; private set; }
    public SpriteRuntime CaretInstance { get; private set; }
    public NineSliceRuntime FocusedIndicator { get; private set; }

    protected abstract string CategoryName { get; }

    public class TextBoxCategoryStates
    {
        public StateSave Enabled { get; set; } = new StateSave() { Name = FrameworkElement.EnabledStateName };
        public StateSave Disabled { get; set; } = new StateSave() { Name = FrameworkElement.DisabledStateName };
        public StateSave Highlighted { get; set; } = new StateSave() { Name = FrameworkElement.HighlightedStateName };
        public StateSave Focused { get; set; } = new StateSave() { Name = FrameworkElement.FocusedStateName };

        // These next were combined into the single "states" variable per discussion
        // But if we want we can always make this thing multi-layered with a "LineMode" sub-class.
        public StateSave SingleLineMode { get; set; } = new StateSave() { Name = "Single" }; 
        public StateSave MultiLineMode { get; set; } = new StateSave() { Name = "Multi" };
        public StateSave MultiLineModeNoWrap { get; set; } = new StateSave() { Name = "MultiNoWrap" };
    }

    public TextBoxCategoryStates States;
    public StateSaveCategory TextboxCategory { get; private set; }
    public StateSaveCategory LineModeCategory { get; private set; }

    public TextBoxBaseVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {
        States = new TextBoxCategoryStates();
        Width = 100;
        Height = 24;

        var uiSpriteSheetTexture = Styling.ActiveStyle.SpriteSheet;

        Background = new NineSliceRuntime();
        Background.Name = "Background";
        Background.X = 0;
        Background.Y = 0;
        Background.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        Background.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        Background.XOrigin = HorizontalAlignment.Center;
        Background.YOrigin = VerticalAlignment.Center;
        Background.Width = 0;
        Background.Height = 0;
        Background.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        Background.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        Background.Color = Styling.ActiveStyle.Colors.DarkGray;
        Background.Texture = uiSpriteSheetTexture;
        Background.ApplyState(Styling.ActiveStyle.NineSlice.Bordered);
        this.AddChild(Background);

        ClipContainer = new ContainerRuntime();
        ClipContainer.Name = "ClipContiner";
        ClipContainer.Dock(Gum.Wireframe.Dock.Fill);
        ClipContainer.ClipsChildren = true;
        this.AddChild(ClipContainer);

        SelectionInstance = new NineSliceRuntime();
        SelectionInstance.Name = "SelectionInstance";
        SelectionInstance.Color = Styling.ActiveStyle.Colors.Accent;
        SelectionInstance.Height = -4f;
        SelectionInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        SelectionInstance.Width = 7f;
        SelectionInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
        SelectionInstance.X = 15f;
        SelectionInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
        SelectionInstance.XUnits = GeneralUnitType.PixelsFromSmall;
        SelectionInstance.Y = 0f;
        SelectionInstance.Texture = uiSpriteSheetTexture;
        SelectionInstance.ApplyState(Styling.ActiveStyle.NineSlice.Solid);
        ClipContainer.AddChild(SelectionInstance);

        TextInstance = new TextRuntime();
        TextInstance.Name = "TextInstance";
        TextInstance.X = 4f;
        TextInstance.XUnits = GeneralUnitType.PixelsFromSmall;
        TextInstance.Y = 0f;
        TextInstance.YUnits = GeneralUnitType.PixelsFromMiddle;
        TextInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
        TextInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        TextInstance.Width = 0f;
        TextInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        TextInstance.Height = -4f;
        TextInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        TextInstance.HorizontalAlignment = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
        TextInstance.VerticalAlignment = VerticalAlignment.Center;
        TextInstance.Color = Styling.ActiveStyle.Colors.White;
        TextInstance.ApplyState(Styling.ActiveStyle.Text.Normal);
        TextInstance.Text = "";
        ClipContainer.AddChild(TextInstance);

        PlaceholderTextInstance = new TextRuntime();
        PlaceholderTextInstance.Name = "PlaceholderTextInstance";
        PlaceholderTextInstance.X = 4f;
        PlaceholderTextInstance.XUnits = GeneralUnitType.PixelsFromSmall;
        PlaceholderTextInstance.Y = 0f;
        PlaceholderTextInstance.YUnits = GeneralUnitType.PixelsFromMiddle;
        PlaceholderTextInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
        PlaceholderTextInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        PlaceholderTextInstance.Width = -8f;
        PlaceholderTextInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        PlaceholderTextInstance.Height = -4f;
        PlaceholderTextInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        PlaceholderTextInstance.Color = Styling.ActiveStyle.Colors.Gray;
        PlaceholderTextInstance.Text = "Text Placeholder";
        PlaceholderTextInstance.VerticalAlignment = VerticalAlignment.Center;
        ClipContainer.AddChild(PlaceholderTextInstance);

        CaretInstance = new SpriteRuntime();
        CaretInstance.Name = "CaretInstance";
        CaretInstance.Color = Styling.ActiveStyle.Colors.Primary;
        CaretInstance.Height = 18f;
        CaretInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
        CaretInstance.Texture = uiSpriteSheetTexture;
        CaretInstance.TextureAddress = global::Gum.Managers.TextureAddress.Custom;
        CaretInstance.ApplyState(Styling.ActiveStyle.NineSlice.Solid);
        CaretInstance.Width = 1f;
        CaretInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
        CaretInstance.X = 4f;
        CaretInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
        CaretInstance.XUnits = GeneralUnitType.PixelsFromSmall;
        CaretInstance.Y = 0f;
        CaretInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        CaretInstance.YUnits = GeneralUnitType.PixelsFromMiddle;
        ClipContainer.AddChild(CaretInstance);

        FocusedIndicator = new NineSliceRuntime();
        FocusedIndicator.Name = "FocusedIndicator";
        FocusedIndicator.Color = Styling.ActiveStyle.Colors.Warning;
        FocusedIndicator.X = 0;
        FocusedIndicator.Y = 2;
        FocusedIndicator.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        FocusedIndicator.YUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;
        FocusedIndicator.XOrigin = HorizontalAlignment.Center;
        FocusedIndicator.YOrigin = VerticalAlignment.Top;
        FocusedIndicator.Width = 0;
        FocusedIndicator.Height = 2;
        FocusedIndicator.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        FocusedIndicator.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        FocusedIndicator.Texture = uiSpriteSheetTexture;
        FocusedIndicator.ApplyState(Styling.ActiveStyle.NineSlice.Solid);
        FocusedIndicator.Visible = false;
        this.AddChild(FocusedIndicator);

        TextboxCategory = new Gum.DataTypes.Variables.StateSaveCategory();
        TextboxCategory.Name = CategoryName;
        this.AddCategory(TextboxCategory);

        void AddVariable(StateSave state, string name, object value)
        {
            state.Variables.Add(new VariableSave
            {
                Name = name,
                Value = value
            });
        }

        void AddTextBoxCategoryState(StateSave state, Color backgroundColor, Color textInstanceColor, bool isFocusedVisible, 
            Color placeholderTextColor, Color selectionColor)
        {
            TextboxCategory.States.Add(state);
            AddVariable(state, "Background.Color", backgroundColor);
            AddVariable(state, "TextInstance.Color", textInstanceColor);
            AddVariable(state, "FocusedIndicator.Visible", isFocusedVisible);
            AddVariable(state, "PlaceholderTextInstance.Color", placeholderTextColor);
            AddVariable(state, "SelectionInstance.Color", selectionColor);
        }

        AddTextBoxCategoryState(States.Enabled, Styling.ActiveStyle.Colors.DarkGray, Styling.ActiveStyle.Colors.White, false, 
            Styling.ActiveStyle.Colors.Gray, Styling.ActiveStyle.Colors.Gray);

        AddTextBoxCategoryState(States.Disabled, Styling.ActiveStyle.Colors.DarkGray, Styling.ActiveStyle.Colors.Gray, false, 
            Styling.ActiveStyle.Colors.Gray, Styling.ActiveStyle.Colors.Gray);

        AddTextBoxCategoryState(States.Highlighted, Styling.ActiveStyle.Colors.Gray, Styling.ActiveStyle.Colors.White, false, 
            Styling.ActiveStyle.Colors.DarkGray, Styling.ActiveStyle.Colors.Gray);

        AddTextBoxCategoryState(States.Focused, Styling.ActiveStyle.Colors.DarkGray, Styling.ActiveStyle.Colors.White, true, 
            Styling.ActiveStyle.Colors.Gray, Styling.ActiveStyle.Colors.Accent);

        LineModeCategory = new Gum.DataTypes.Variables.StateSaveCategory();
        LineModeCategory.Name = "LineModeCategory";
        this.AddCategory(LineModeCategory);

        LineModeCategory.States.Add(States.SingleLineMode);
        AddVariable(States.SingleLineMode, "SelectionInstance.Height", -4f);
        AddVariable(States.SingleLineMode, "SelectionInstance.HeightUnits", global::Gum.DataTypes.DimensionUnitType.RelativeToParent);
        AddVariable(States.SingleLineMode, "TextInstance.Width", 0f);
        AddVariable(States.SingleLineMode, "TextInstance.WidthUnits", global::Gum.DataTypes.DimensionUnitType.RelativeToChildren);
        AddVariable(States.SingleLineMode, "PlaceholderTextInstance.VerticalAlignment", VerticalAlignment.Center);
        AddVariable(States.SingleLineMode, "TextInstance.VerticalAlignment", VerticalAlignment.Center);

        LineModeCategory.States.Add(States.MultiLineMode);
        AddVariable(States.MultiLineMode, "SelectionInstance.Height", 20f);
        AddVariable(States.MultiLineMode, "SelectionInstance.HeightUnits", global::Gum.DataTypes.DimensionUnitType.Absolute);
        AddVariable(States.MultiLineMode, "TextInstance.Width", -8f);
        AddVariable(States.MultiLineMode, "TextInstance.WidthUnits", global::Gum.DataTypes.DimensionUnitType.RelativeToParent);
        AddVariable(States.MultiLineMode, "PlaceholderTextInstance.VerticalAlignment", VerticalAlignment.Top);
        AddVariable(States.MultiLineMode, "TextInstance.VerticalAlignment", VerticalAlignment.Top);

        LineModeCategory.States.Add(States.MultiLineModeNoWrap);
        AddVariable(States.MultiLineModeNoWrap, "SelectionInstance.Height", 20f);
        AddVariable(States.MultiLineModeNoWrap, "SelectionInstance.HeightUnits", global::Gum.DataTypes.DimensionUnitType.Absolute);
        AddVariable(States.MultiLineModeNoWrap, "TextInstance.Width", 0f);
        AddVariable(States.MultiLineModeNoWrap, "TextInstance.WidthUnits", global::Gum.DataTypes.DimensionUnitType.RelativeToChildren);
        AddVariable(States.MultiLineModeNoWrap, "PlaceholderTextInstance.VerticalAlignment", VerticalAlignment.Top);
        AddVariable(States.MultiLineModeNoWrap, "TextInstance.VerticalAlignment", VerticalAlignment.Top);
    }
}
