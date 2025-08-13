//Code for Controls/Slider (Container)
using GumRuntime;
using System.Linq;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using CodeGen_MonoGameForms_FullCodegen.Components.Controls;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

namespace CodeGen_MonoGameForms_FullCodegen.Components.Controls;
partial class Slider : global::MonoGameGum.Forms.Controls.Slider
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::MonoGameGum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Controls/Slider");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new Slider(visual);
            return visual;
        });
        global::MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(Slider)] = template;
        global::MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(global::MonoGameGum.Forms.Controls.Slider)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Controls/Slider", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public enum SliderCategory
    {
        Enabled,
        Disabled,
        DisabledFocused,
        Focused,
        Highlighted,
        HighlightedFocused,
        Pushed,
    }

    private SliderCategory? _sliderCategoryState;
    public SliderCategory? SliderCategoryState
    {
        get => _sliderCategoryState;
        set
        {
            _sliderCategoryState = value;
            var appliedDynamically = false;
            if(!appliedDynamically)
            {
                switch (value)
                {
                    case SliderCategory.Enabled:
                        this.FocusedIndicator.Visible = false;
                        this.ThumbInstance.ButtonCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Controls.ButtonStandard.ButtonCategory.Enabled;
                        break;
                    case SliderCategory.Disabled:
                        this.FocusedIndicator.Visible = false;
                        this.ThumbInstance.ButtonCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Controls.ButtonStandard.ButtonCategory.Disabled;
                        break;
                    case SliderCategory.DisabledFocused:
                        this.FocusedIndicator.Visible = true;
                        this.ThumbInstance.ButtonCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Controls.ButtonStandard.ButtonCategory.Disabled;
                        break;
                    case SliderCategory.Focused:
                        this.FocusedIndicator.Visible = true;
                        this.ThumbInstance.ButtonCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Controls.ButtonStandard.ButtonCategory.Enabled;
                        break;
                    case SliderCategory.Highlighted:
                        this.FocusedIndicator.Visible = false;
                        this.ThumbInstance.ButtonCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Controls.ButtonStandard.ButtonCategory.Highlighted;
                        break;
                    case SliderCategory.HighlightedFocused:
                        this.FocusedIndicator.Visible = true;
                        this.ThumbInstance.ButtonCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Controls.ButtonStandard.ButtonCategory.Highlighted;
                        break;
                    case SliderCategory.Pushed:
                        this.FocusedIndicator.Visible = false;
                        this.ThumbInstance.ButtonCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Controls.ButtonStandard.ButtonCategory.Enabled;
                        break;
                }
            }
        }
    }
    public ContainerRuntime TrackInstance { get; protected set; }
    public NineSliceRuntime TrackBackground { get; protected set; }
    public ButtonStandard ThumbInstance { get; protected set; }
    public NineSliceRuntime FocusedIndicator { get; protected set; }

    public float SliderPercent
    {
        get => ThumbInstance.Visual.X;
        set => ThumbInstance.Visual.X = value;
    }

    public Slider(InteractiveGue visual) : base(visual)
    {
        InitializeInstances();
        CustomInitialize();
    }
    public Slider() : base(new ContainerRuntime())
    {

        this.Visual.Height = 24f;
         
        this.Visual.Width = 128f;

        InitializeInstances();

        ApplyDefaultVariables();
        AssignParents();
        CustomInitialize();
    }
    protected virtual void InitializeInstances()
    {
        base.ReactToVisualChanged();
        TrackInstance = new global::MonoGameGum.GueDeriving.ContainerRuntime();
        TrackInstance.ElementSave = ObjectFinder.Self.GetStandardElement("Container");
        if (TrackInstance.ElementSave != null) TrackInstance.AddStatesAndCategoriesRecursivelyToGue(TrackInstance.ElementSave);
        if (TrackInstance.ElementSave != null) TrackInstance.SetInitialState();
        TrackInstance.Name = "TrackInstance";
        TrackBackground = new global::MonoGameGum.GueDeriving.NineSliceRuntime();
        TrackBackground.ElementSave = ObjectFinder.Self.GetStandardElement("NineSlice");
        if (TrackBackground.ElementSave != null) TrackBackground.AddStatesAndCategoriesRecursivelyToGue(TrackBackground.ElementSave);
        if (TrackBackground.ElementSave != null) TrackBackground.SetInitialState();
        TrackBackground.Name = "TrackBackground";
        ThumbInstance = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.ButtonStandard();
        ThumbInstance.Name = "ThumbInstance";
        FocusedIndicator = new global::MonoGameGum.GueDeriving.NineSliceRuntime();
        FocusedIndicator.ElementSave = ObjectFinder.Self.GetStandardElement("NineSlice");
        if (FocusedIndicator.ElementSave != null) FocusedIndicator.AddStatesAndCategoriesRecursivelyToGue(FocusedIndicator.ElementSave);
        if (FocusedIndicator.ElementSave != null) FocusedIndicator.SetInitialState();
        FocusedIndicator.Name = "FocusedIndicator";
    }
    protected virtual void AssignParents()
    {
        this.AddChild(TrackInstance);
        TrackInstance.AddChild(TrackBackground);
        TrackInstance.AddChild(ThumbInstance);
        this.AddChild(FocusedIndicator);
    }
    private void ApplyDefaultVariables()
    {
        this.TrackInstance.Height = 0f;
        this.TrackInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.TrackInstance.Width = -32f;
        this.TrackInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.TrackInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.TrackInstance.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        this.TrackInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.TrackInstance.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.TrackBackground.SetProperty("ColorCategoryState", "DarkGray");
        this.TrackBackground.SetProperty("StyleCategoryState", "Bordered");
        this.TrackBackground.Height = 8f;
        this.TrackBackground.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
        this.TrackBackground.Width = 0f;

        this.ThumbInstance.ButtonDisplayText = @"";
        this.ThumbInstance.Visual.Height = 24f;
        this.ThumbInstance.Visual.Width = 32f;
        this.ThumbInstance.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.ThumbInstance.Visual.XUnits = global::Gum.Converters.GeneralUnitType.Percentage;
        this.ThumbInstance.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.ThumbInstance.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.FocusedIndicator.SetProperty("ColorCategoryState", "Warning");
        this.FocusedIndicator.SetProperty("StyleCategoryState", "Solid");
        this.FocusedIndicator.Height = 2f;
        this.FocusedIndicator.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
        this.FocusedIndicator.Visible = false;
        this.FocusedIndicator.Y = 2f;
        this.FocusedIndicator.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
        this.FocusedIndicator.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromLarge;

    }
    partial void CustomInitialize();
}
