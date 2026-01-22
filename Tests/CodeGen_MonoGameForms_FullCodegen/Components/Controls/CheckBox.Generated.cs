//Code for Controls/CheckBox (Container)
using CodeGen_MonoGameForms_FullCodegen.Components.Elements;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using System.Linq;
namespace CodeGen_MonoGameForms_FullCodegen.Components.Controls;
partial class CheckBox : global::Gum.Forms.Controls.CheckBox
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Controls/CheckBox");
#if DEBUG
if(element == null) throw new System.InvalidOperationException("Could not find an element named Controls/CheckBox - did you forget to load a Gum project?");
#endif
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new CheckBox(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(CheckBox)] = template;
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(global::Gum.Forms.Controls.CheckBox)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Controls/CheckBox", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public enum CheckBoxCategory
    {
        EnabledOn,
        EnabledOff,
        EnabledIndeterminate,
        DisabledOn,
        DisabledOff,
        DisabledIndeterminate,
        HighlightedOn,
        HighlightedOff,
        HighlightedIndeterminate,
        PushedOn,
        PushedOff,
        PushedIndeterminate,
        FocusedOn,
        FocusedOff,
        FocusedIndeterminate,
        HighlightedFocusedOn,
        HighlightedFocusedOff,
        HighlightedFocusedIndeterminate,
        DisabledFocusedOn,
        DisabledFocusedOff,
        DisabledFocusedIndeterminate,
    }

    private CheckBoxCategory? _checkBoxCategoryState;
    public CheckBoxCategory? CheckBoxCategoryState
    {
        get => _checkBoxCategoryState;
        set
        {
            _checkBoxCategoryState = value;
            var appliedDynamically = false;
            if(!appliedDynamically)
            {
                switch (value)
                {
                    case CheckBoxCategory.EnabledOn:
                        this.Check.IconCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon.IconCategory.Check;
                        this.Check.Visual.SetProperty("IconColor", "White");
                        this.Check.Visual.Visible = true;
                        this.CheckboxBackground.SetProperty("ColorCategoryState", "Primary");
                        this.FocusedIndicator.Visible = false;
                        this.TextInstance.SetProperty("ColorCategoryState", "White");
                        break;
                    case CheckBoxCategory.EnabledOff:
                        this.Check.IconCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon.IconCategory.Check;
                        this.Check.Visual.SetProperty("IconColor", "White");
                        this.Check.Visual.Visible = false;
                        this.CheckboxBackground.SetProperty("ColorCategoryState", "Primary");
                        this.FocusedIndicator.Visible = false;
                        this.TextInstance.SetProperty("ColorCategoryState", "White");
                        break;
                    case CheckBoxCategory.EnabledIndeterminate:
                        this.Check.IconCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon.IconCategory.Dash;
                        this.Check.Visual.SetProperty("IconColor", "White");
                        this.Check.Visual.Visible = true;
                        this.CheckboxBackground.SetProperty("ColorCategoryState", "Primary");
                        this.FocusedIndicator.Visible = false;
                        this.TextInstance.SetProperty("ColorCategoryState", "White");
                        break;
                    case CheckBoxCategory.DisabledOn:
                        this.Check.IconCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon.IconCategory.Check;
                        this.Check.Visual.SetProperty("IconColor", "Gray");
                        this.Check.Visual.Visible = true;
                        this.CheckboxBackground.SetProperty("ColorCategoryState", "DarkGray");
                        this.FocusedIndicator.Visible = false;
                        this.TextInstance.SetProperty("ColorCategoryState", "Gray");
                        break;
                    case CheckBoxCategory.DisabledOff:
                        this.Check.IconCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon.IconCategory.Check;
                        this.Check.Visual.SetProperty("IconColor", "White");
                        this.Check.Visual.Visible = false;
                        this.CheckboxBackground.SetProperty("ColorCategoryState", "DarkGray");
                        this.FocusedIndicator.Visible = false;
                        this.TextInstance.SetProperty("ColorCategoryState", "Gray");
                        break;
                    case CheckBoxCategory.DisabledIndeterminate:
                        this.Check.IconCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon.IconCategory.Dash;
                        this.Check.Visual.SetProperty("IconColor", "Gray");
                        this.Check.Visual.Visible = true;
                        this.CheckboxBackground.SetProperty("ColorCategoryState", "DarkGray");
                        this.FocusedIndicator.Visible = false;
                        this.TextInstance.SetProperty("ColorCategoryState", "Gray");
                        break;
                    case CheckBoxCategory.HighlightedOn:
                        this.Check.IconCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon.IconCategory.Check;
                        this.Check.Visual.SetProperty("IconColor", "White");
                        this.Check.Visual.Visible = true;
                        this.CheckboxBackground.SetProperty("ColorCategoryState", "PrimaryLight");
                        this.FocusedIndicator.Visible = false;
                        this.TextInstance.SetProperty("ColorCategoryState", "White");
                        break;
                    case CheckBoxCategory.HighlightedOff:
                        this.Check.IconCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon.IconCategory.Check;
                        this.Check.Visual.SetProperty("IconColor", "White");
                        this.Check.Visual.Visible = false;
                        this.CheckboxBackground.SetProperty("ColorCategoryState", "PrimaryLight");
                        this.FocusedIndicator.Visible = false;
                        this.TextInstance.SetProperty("ColorCategoryState", "White");
                        break;
                    case CheckBoxCategory.HighlightedIndeterminate:
                        this.Check.IconCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon.IconCategory.Dash;
                        this.Check.Visual.SetProperty("IconColor", "White");
                        this.Check.Visual.Visible = true;
                        this.CheckboxBackground.SetProperty("ColorCategoryState", "PrimaryLight");
                        this.FocusedIndicator.Visible = false;
                        this.TextInstance.SetProperty("ColorCategoryState", "White");
                        break;
                    case CheckBoxCategory.PushedOn:
                        this.Check.IconCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon.IconCategory.Check;
                        this.Check.Visual.SetProperty("IconColor", "White");
                        this.Check.Visual.Visible = true;
                        this.CheckboxBackground.SetProperty("ColorCategoryState", "PrimaryLight");
                        this.FocusedIndicator.Visible = false;
                        this.TextInstance.SetProperty("ColorCategoryState", "White");
                        break;
                    case CheckBoxCategory.PushedOff:
                        this.Check.IconCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon.IconCategory.Check;
                        this.Check.Visual.SetProperty("IconColor", "White");
                        this.Check.Visual.Visible = false;
                        this.CheckboxBackground.SetProperty("ColorCategoryState", "PrimaryLight");
                        this.FocusedIndicator.Visible = false;
                        this.TextInstance.SetProperty("ColorCategoryState", "White");
                        break;
                    case CheckBoxCategory.PushedIndeterminate:
                        this.Check.IconCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon.IconCategory.Dash;
                        this.Check.Visual.SetProperty("IconColor", "White");
                        this.Check.Visual.Visible = true;
                        this.CheckboxBackground.SetProperty("ColorCategoryState", "PrimaryLight");
                        this.FocusedIndicator.Visible = false;
                        this.TextInstance.SetProperty("ColorCategoryState", "White");
                        break;
                    case CheckBoxCategory.FocusedOn:
                        this.Check.IconCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon.IconCategory.Check;
                        this.Check.Visual.SetProperty("IconColor", "White");
                        this.Check.Visual.Visible = true;
                        this.CheckboxBackground.SetProperty("ColorCategoryState", "Primary");
                        this.FocusedIndicator.Visible = true;
                        this.TextInstance.SetProperty("ColorCategoryState", "White");
                        break;
                    case CheckBoxCategory.FocusedOff:
                        this.Check.IconCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon.IconCategory.Check;
                        this.Check.Visual.SetProperty("IconColor", "White");
                        this.Check.Visual.Visible = false;
                        this.CheckboxBackground.SetProperty("ColorCategoryState", "Primary");
                        this.FocusedIndicator.Visible = true;
                        this.TextInstance.SetProperty("ColorCategoryState", "White");
                        break;
                    case CheckBoxCategory.FocusedIndeterminate:
                        this.Check.IconCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon.IconCategory.Dash;
                        this.Check.Visual.SetProperty("IconColor", "White");
                        this.Check.Visual.Visible = true;
                        this.CheckboxBackground.SetProperty("ColorCategoryState", "Primary");
                        this.FocusedIndicator.Visible = true;
                        this.TextInstance.SetProperty("ColorCategoryState", "White");
                        break;
                    case CheckBoxCategory.HighlightedFocusedOn:
                        this.Check.IconCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon.IconCategory.Check;
                        this.Check.Visual.SetProperty("IconColor", "White");
                        this.Check.Visual.Visible = true;
                        this.CheckboxBackground.SetProperty("ColorCategoryState", "PrimaryLight");
                        this.FocusedIndicator.Visible = true;
                        this.TextInstance.SetProperty("ColorCategoryState", "White");
                        break;
                    case CheckBoxCategory.HighlightedFocusedOff:
                        this.Check.IconCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon.IconCategory.Check;
                        this.Check.Visual.SetProperty("IconColor", "White");
                        this.Check.Visual.Visible = false;
                        this.CheckboxBackground.SetProperty("ColorCategoryState", "PrimaryLight");
                        this.FocusedIndicator.Visible = true;
                        this.TextInstance.SetProperty("ColorCategoryState", "White");
                        break;
                    case CheckBoxCategory.HighlightedFocusedIndeterminate:
                        this.Check.IconCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon.IconCategory.Dash;
                        this.Check.Visual.SetProperty("IconColor", "White");
                        this.Check.Visual.Visible = true;
                        this.CheckboxBackground.SetProperty("ColorCategoryState", "PrimaryLight");
                        this.FocusedIndicator.Visible = true;
                        this.TextInstance.SetProperty("ColorCategoryState", "White");
                        break;
                    case CheckBoxCategory.DisabledFocusedOn:
                        this.Check.IconCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon.IconCategory.Check;
                        this.Check.Visual.SetProperty("IconColor", "Gray");
                        this.Check.Visual.Visible = true;
                        this.CheckboxBackground.SetProperty("ColorCategoryState", "DarkGray");
                        this.FocusedIndicator.Visible = true;
                        this.TextInstance.SetProperty("ColorCategoryState", "Gray");
                        break;
                    case CheckBoxCategory.DisabledFocusedOff:
                        this.Check.IconCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon.IconCategory.Check;
                        this.Check.Visual.SetProperty("IconColor", "White");
                        this.Check.Visual.Visible = false;
                        this.CheckboxBackground.SetProperty("ColorCategoryState", "DarkGray");
                        this.FocusedIndicator.Visible = true;
                        this.TextInstance.SetProperty("ColorCategoryState", "Gray");
                        break;
                    case CheckBoxCategory.DisabledFocusedIndeterminate:
                        this.Check.IconCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon.IconCategory.Dash;
                        this.Check.Visual.SetProperty("IconColor", "Gray");
                        this.Check.Visual.Visible = true;
                        this.CheckboxBackground.SetProperty("ColorCategoryState", "DarkGray");
                        this.FocusedIndicator.Visible = true;
                        this.TextInstance.SetProperty("ColorCategoryState", "Gray");
                        break;
                }
            }
        }
    }
    public NineSliceRuntime CheckboxBackground { get; protected set; }
    public TextRuntime TextInstance { get; protected set; }
    public Icon Check { get; protected set; }
    public NineSliceRuntime FocusedIndicator { get; protected set; }

    public string Text
    {
        get => TextInstance.Text;
        set => TextInstance.Text = value;
    }

    public CheckBox(InteractiveGue visual) : base(visual)
    {
        InitializeInstances();
        CustomInitialize();
    }
    public CheckBox() : base(new ContainerRuntime())
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
        CheckboxBackground = new global::MonoGameGum.GueDeriving.NineSliceRuntime();
        CheckboxBackground.ElementSave = ObjectFinder.Self.GetStandardElement("NineSlice");
        if (CheckboxBackground.ElementSave != null) CheckboxBackground.AddStatesAndCategoriesRecursivelyToGue(CheckboxBackground.ElementSave);
        if (CheckboxBackground.ElementSave != null) CheckboxBackground.SetInitialState();
        CheckboxBackground.Name = "CheckboxBackground";
        TextInstance = new global::MonoGameGum.GueDeriving.TextRuntime();
        TextInstance.ElementSave = ObjectFinder.Self.GetStandardElement("Text");
        if (TextInstance.ElementSave != null) TextInstance.AddStatesAndCategoriesRecursivelyToGue(TextInstance.ElementSave);
        if (TextInstance.ElementSave != null) TextInstance.SetInitialState();
        TextInstance.Name = "TextInstance";
        Check = new CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon();
        Check.Name = "Check";
        FocusedIndicator = new global::MonoGameGum.GueDeriving.NineSliceRuntime();
        FocusedIndicator.ElementSave = ObjectFinder.Self.GetStandardElement("NineSlice");
        if (FocusedIndicator.ElementSave != null) FocusedIndicator.AddStatesAndCategoriesRecursivelyToGue(FocusedIndicator.ElementSave);
        if (FocusedIndicator.ElementSave != null) FocusedIndicator.SetInitialState();
        FocusedIndicator.Name = "FocusedIndicator";
        base.RefreshInternalVisualReferences();
    }
    protected virtual void AssignParents()
    {
        this.AddChild(CheckboxBackground);
        this.AddChild(TextInstance);
        CheckboxBackground.AddChild(Check);
        this.AddChild(FocusedIndicator);
    }
    private void ApplyDefaultVariables()
    {
        this.CheckboxBackground.SetProperty("ColorCategoryState", "Primary");
        this.CheckboxBackground.SetProperty("StyleCategoryState", "Bordered");
        this.CheckboxBackground.Height = 24f;
        this.CheckboxBackground.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
        this.CheckboxBackground.Width = 24f;
        this.CheckboxBackground.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
        this.CheckboxBackground.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
        this.CheckboxBackground.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;

        this.TextInstance.SetProperty("ColorCategoryState", "White");
        this.TextInstance.SetProperty("StyleCategoryState", "Normal");
        this.TextInstance.Height = 32f;
        this.TextInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
        ((TextRuntime)this.TextInstance).HorizontalAlignment = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
        this.TextInstance.Text = @"Checkbox Label";
        ((TextRuntime)this.TextInstance).VerticalAlignment = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.TextInstance.Width = -28f;
        this.TextInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.TextInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
        this.TextInstance.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromLarge;
        this.TextInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.TextInstance.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.Check.IconCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon.IconCategory.Check;
        this.Check.Visual.SetProperty("IconColor", "White");
        this.Check.Visual.Height = 0f;
        this.Check.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.Check.Visual.Width = 0f;
        this.Check.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;

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
