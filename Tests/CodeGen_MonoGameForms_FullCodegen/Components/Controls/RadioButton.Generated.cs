//Code for Controls/RadioButton (Container)
using GumRuntime;
using System.Linq;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using CodeGen_MonoGameForms_FullCodegen.Components.Elements;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

namespace CodeGen_MonoGameForms_FullCodegen.Components.Controls;
partial class RadioButton : global::Gum.Forms.Controls.RadioButton
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Controls/RadioButton");
#if DEBUG
if(element == null) throw new System.InvalidOperationException("Could not find an element named Controls/RadioButton - did you forget to load a Gum project?");
#endif
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new RadioButton(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(RadioButton)] = template;
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(global::Gum.Forms.Controls.RadioButton)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Controls/RadioButton", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public enum RadioButtonCategory
    {
        EnabledOn,
        EnabledOff,
        DisabledOn,
        DisabledOff,
        HighlightedOn,
        HighlightedOff,
        PushedOn,
        PushedOff,
        FocusedOn,
        FocusedOff,
        HighlightedFocusedOn,
        HighlightedFocusedOff,
        DisabledFocusedOn,
        DisabledFocusedOff,
    }

    private RadioButtonCategory? _radioButtonCategoryState;
    public RadioButtonCategory? RadioButtonCategoryState
    {
        get => _radioButtonCategoryState;
        set
        {
            _radioButtonCategoryState = value;
            var appliedDynamically = false;
            if(!appliedDynamically)
            {
                switch (value)
                {
                    case RadioButtonCategory.EnabledOn:
                        this.FocusedIndicator.Visible = false;
                        this.Radio.Visual.SetProperty("IconColor", "White");
                        this.Radio.Visual.Visible = true;
                        this.RadioBackground.SetProperty("ColorCategoryState", "Primary");
                        this.TextInstance.SetProperty("ColorCategoryState", "White");
                        break;
                    case RadioButtonCategory.EnabledOff:
                        this.FocusedIndicator.Visible = false;
                        this.Radio.Visual.SetProperty("IconColor", "White");
                        this.Radio.Visual.Visible = false;
                        this.RadioBackground.SetProperty("ColorCategoryState", "Primary");
                        this.TextInstance.SetProperty("ColorCategoryState", "White");
                        break;
                    case RadioButtonCategory.DisabledOn:
                        this.FocusedIndicator.Visible = false;
                        this.Radio.Visual.SetProperty("IconColor", "Gray");
                        this.Radio.Visual.Visible = true;
                        this.RadioBackground.SetProperty("ColorCategoryState", "DarkGray");
                        this.TextInstance.SetProperty("ColorCategoryState", "Gray");
                        break;
                    case RadioButtonCategory.DisabledOff:
                        this.FocusedIndicator.Visible = false;
                        this.Radio.Visual.SetProperty("IconColor", "Gray");
                        this.Radio.Visual.Visible = false;
                        this.RadioBackground.SetProperty("ColorCategoryState", "DarkGray");
                        this.TextInstance.SetProperty("ColorCategoryState", "Gray");
                        break;
                    case RadioButtonCategory.HighlightedOn:
                        this.FocusedIndicator.Visible = false;
                        this.Radio.Visual.SetProperty("IconColor", "White");
                        this.Radio.Visual.Visible = true;
                        this.RadioBackground.SetProperty("ColorCategoryState", "PrimaryLight");
                        this.TextInstance.SetProperty("ColorCategoryState", "White");
                        break;
                    case RadioButtonCategory.HighlightedOff:
                        this.FocusedIndicator.Visible = false;
                        this.Radio.Visual.SetProperty("IconColor", "White");
                        this.Radio.Visual.Visible = false;
                        this.RadioBackground.SetProperty("ColorCategoryState", "PrimaryLight");
                        this.TextInstance.SetProperty("ColorCategoryState", "White");
                        break;
                    case RadioButtonCategory.PushedOn:
                        this.FocusedIndicator.Visible = false;
                        this.Radio.Visual.SetProperty("IconColor", "White");
                        this.Radio.Visual.Visible = true;
                        this.RadioBackground.SetProperty("ColorCategoryState", "PrimaryDark");
                        this.TextInstance.SetProperty("ColorCategoryState", "White");
                        break;
                    case RadioButtonCategory.PushedOff:
                        this.FocusedIndicator.Visible = false;
                        this.Radio.Visual.SetProperty("IconColor", "White");
                        this.Radio.Visual.Visible = false;
                        this.RadioBackground.SetProperty("ColorCategoryState", "PrimaryDark");
                        this.TextInstance.SetProperty("ColorCategoryState", "White");
                        break;
                    case RadioButtonCategory.FocusedOn:
                        this.FocusedIndicator.Visible = true;
                        this.Radio.Visual.SetProperty("IconColor", "White");
                        this.Radio.Visual.Visible = true;
                        this.RadioBackground.SetProperty("ColorCategoryState", "Primary");
                        this.TextInstance.SetProperty("ColorCategoryState", "White");
                        break;
                    case RadioButtonCategory.FocusedOff:
                        this.FocusedIndicator.Visible = true;
                        this.Radio.Visual.SetProperty("IconColor", "White");
                        this.Radio.Visual.Visible = false;
                        this.RadioBackground.SetProperty("ColorCategoryState", "Primary");
                        this.TextInstance.SetProperty("ColorCategoryState", "White");
                        break;
                    case RadioButtonCategory.HighlightedFocusedOn:
                        this.FocusedIndicator.Visible = true;
                        this.Radio.Visual.SetProperty("IconColor", "White");
                        this.Radio.Visual.Visible = true;
                        this.RadioBackground.SetProperty("ColorCategoryState", "PrimaryLight");
                        this.TextInstance.SetProperty("ColorCategoryState", "White");
                        break;
                    case RadioButtonCategory.HighlightedFocusedOff:
                        this.FocusedIndicator.Visible = true;
                        this.Radio.Visual.SetProperty("IconColor", "White");
                        this.Radio.Visual.Visible = false;
                        this.RadioBackground.SetProperty("ColorCategoryState", "PrimaryLight");
                        this.TextInstance.SetProperty("ColorCategoryState", "White");
                        break;
                    case RadioButtonCategory.DisabledFocusedOn:
                        this.FocusedIndicator.Visible = true;
                        this.Radio.Visual.SetProperty("IconColor", "Gray");
                        this.Radio.Visual.Visible = true;
                        this.RadioBackground.SetProperty("ColorCategoryState", "DarkGray");
                        this.TextInstance.SetProperty("ColorCategoryState", "Gray");
                        break;
                    case RadioButtonCategory.DisabledFocusedOff:
                        this.FocusedIndicator.Visible = true;
                        this.Radio.Visual.SetProperty("IconColor", "Gray");
                        this.Radio.Visual.Visible = false;
                        this.RadioBackground.SetProperty("ColorCategoryState", "DarkGray");
                        this.TextInstance.SetProperty("ColorCategoryState", "Gray");
                        break;
                }
            }
        }
    }
    public NineSliceRuntime RadioBackground { get; protected set; }
    public Icon Radio { get; protected set; }
    public TextRuntime TextInstance { get; protected set; }
    public NineSliceRuntime FocusedIndicator { get; protected set; }

    public string Text
    {
        get => TextInstance.Text;
        set => TextInstance.Text = value;
    }

    public RadioButton(InteractiveGue visual) : base(visual)
    {
        InitializeInstances();
        CustomInitialize();
    }
    public RadioButton() : base(new ContainerRuntime())
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
        RadioBackground = new global::MonoGameGum.GueDeriving.NineSliceRuntime();
        RadioBackground.ElementSave = ObjectFinder.Self.GetStandardElement("NineSlice");
        if (RadioBackground.ElementSave != null) RadioBackground.AddStatesAndCategoriesRecursivelyToGue(RadioBackground.ElementSave);
        if (RadioBackground.ElementSave != null) RadioBackground.SetInitialState();
        RadioBackground.Name = "RadioBackground";
        Radio = new CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon();
        Radio.Name = "Radio";
        TextInstance = new global::MonoGameGum.GueDeriving.TextRuntime();
        TextInstance.ElementSave = ObjectFinder.Self.GetStandardElement("Text");
        if (TextInstance.ElementSave != null) TextInstance.AddStatesAndCategoriesRecursivelyToGue(TextInstance.ElementSave);
        if (TextInstance.ElementSave != null) TextInstance.SetInitialState();
        TextInstance.Name = "TextInstance";
        FocusedIndicator = new global::MonoGameGum.GueDeriving.NineSliceRuntime();
        FocusedIndicator.ElementSave = ObjectFinder.Self.GetStandardElement("NineSlice");
        if (FocusedIndicator.ElementSave != null) FocusedIndicator.AddStatesAndCategoriesRecursivelyToGue(FocusedIndicator.ElementSave);
        if (FocusedIndicator.ElementSave != null) FocusedIndicator.SetInitialState();
        FocusedIndicator.Name = "FocusedIndicator";
        base.RefreshInternalVisualReferences();
    }
    protected virtual void AssignParents()
    {
        this.AddChild(RadioBackground);
        RadioBackground.AddChild(Radio);
        this.AddChild(TextInstance);
        this.AddChild(FocusedIndicator);
    }
    private void ApplyDefaultVariables()
    {
        this.RadioBackground.SetProperty("ColorCategoryState", "Primary");
        this.RadioBackground.SetProperty("StyleCategoryState", "CircleBordered");
        this.RadioBackground.Height = 24f;
        this.RadioBackground.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
        this.RadioBackground.Width = 24f;
        this.RadioBackground.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
        this.RadioBackground.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
        this.RadioBackground.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;
        this.RadioBackground.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.RadioBackground.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.Radio.IconCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon.IconCategory.Circle2;
        this.Radio.Visual.SetProperty("IconColor", "White");
        this.Radio.Visual.Height = 0f;
        this.Radio.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.Radio.Visual.Width = 0f;
        this.Radio.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;

        this.TextInstance.SetProperty("ColorCategoryState", "White");
        this.TextInstance.SetProperty("StyleCategoryState", "Normal");
        this.TextInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.TextInstance.Text = @"Radio Label";
        ((TextRuntime)this.TextInstance).VerticalAlignment = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.TextInstance.Width = -28f;
        this.TextInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.TextInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
        this.TextInstance.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromLarge;

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
