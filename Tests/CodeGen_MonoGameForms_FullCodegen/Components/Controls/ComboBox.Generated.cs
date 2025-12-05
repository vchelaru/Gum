//Code for Controls/ComboBox (Container)
using GumRuntime;
using System.Linq;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using CodeGen_MonoGameForms_FullCodegen.Components.Controls;
using CodeGen_MonoGameForms_FullCodegen.Components.Elements;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

namespace CodeGen_MonoGameForms_FullCodegen.Components.Controls;
partial class ComboBox : global::Gum.Forms.Controls.ComboBox
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Controls/ComboBox");
#if DEBUG
if(element == null) throw new System.InvalidOperationException("Could not find an element named Controls/ComboBox - did you forget to load a Gum project?");
#endif
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new ComboBox(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(ComboBox)] = template;
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(global::Gum.Forms.Controls.ComboBox)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Controls/ComboBox", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public enum ComboBoxCategory
    {
        Enabled,
        Disabled,
        Highlighted,
        Pushed,
        HighlightedFocused,
        Focused,
        DisabledFocused,
    }

    private ComboBoxCategory? _comboBoxCategoryState;
    public ComboBoxCategory? ComboBoxCategoryState
    {
        get => _comboBoxCategoryState;
        set
        {
            _comboBoxCategoryState = value;
            var appliedDynamically = false;
            if(!appliedDynamically)
            {
                switch (value)
                {
                    case ComboBoxCategory.Enabled:
                        this.FocusedIndicator.Visible = false;
                        this.IconInstance.Visual.SetProperty("IconColor", "Primary");
                        this.TextInstance.SetProperty("ColorCategoryState", "White");
                        break;
                    case ComboBoxCategory.Disabled:
                        this.FocusedIndicator.Visible = false;
                        this.IconInstance.Visual.SetProperty("IconColor", "Gray");
                        this.TextInstance.SetProperty("ColorCategoryState", "Gray");
                        break;
                    case ComboBoxCategory.Highlighted:
                        this.FocusedIndicator.Visible = false;
                        this.IconInstance.Visual.SetProperty("IconColor", "PrimaryLight");
                        this.TextInstance.SetProperty("ColorCategoryState", "PrimaryLight");
                        break;
                    case ComboBoxCategory.Pushed:
                        this.FocusedIndicator.Visible = false;
                        this.IconInstance.Visual.SetProperty("IconColor", "PrimaryDark");
                        this.TextInstance.SetProperty("ColorCategoryState", "PrimaryDark");
                        break;
                    case ComboBoxCategory.HighlightedFocused:
                        this.FocusedIndicator.Visible = true;
                        this.IconInstance.Visual.SetProperty("IconColor", "Primary");
                        this.TextInstance.SetProperty("ColorCategoryState", "White");
                        break;
                    case ComboBoxCategory.Focused:
                        this.FocusedIndicator.Visible = true;
                        this.IconInstance.Visual.SetProperty("IconColor", "Primary");
                        this.TextInstance.SetProperty("ColorCategoryState", "White");
                        break;
                    case ComboBoxCategory.DisabledFocused:
                        this.FocusedIndicator.Visible = true;
                        this.IconInstance.Visual.SetProperty("IconColor", "Primary");
                        this.TextInstance.SetProperty("ColorCategoryState", "White");
                        break;
                }
            }
        }
    }
    public NineSliceRuntime Background { get; protected set; }
    public TextRuntime TextInstance { get; protected set; }
    public ListBox ListBoxInstance { get; protected set; }
    public Icon IconInstance { get; protected set; }
    public NineSliceRuntime FocusedIndicator { get; protected set; }

    public ComboBox(InteractiveGue visual) : base(visual)
    {
        InitializeInstances();
        CustomInitialize();
    }
    public ComboBox() : base(new ContainerRuntime())
    {

        this.Visual.Height = 24f;
         
        this.Visual.Width = 256f;

        InitializeInstances();

        ApplyDefaultVariables();
        AssignParents();
        CustomInitialize();
    }
    protected virtual void InitializeInstances()
    {
        Background = new global::MonoGameGum.GueDeriving.NineSliceRuntime();
        Background.ElementSave = ObjectFinder.Self.GetStandardElement("NineSlice");
        if (Background.ElementSave != null) Background.AddStatesAndCategoriesRecursivelyToGue(Background.ElementSave);
        if (Background.ElementSave != null) Background.SetInitialState();
        Background.Name = "Background";
        TextInstance = new global::MonoGameGum.GueDeriving.TextRuntime();
        TextInstance.ElementSave = ObjectFinder.Self.GetStandardElement("Text");
        if (TextInstance.ElementSave != null) TextInstance.AddStatesAndCategoriesRecursivelyToGue(TextInstance.ElementSave);
        if (TextInstance.ElementSave != null) TextInstance.SetInitialState();
        TextInstance.Name = "TextInstance";
        ListBoxInstance = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.ListBox();
        ListBoxInstance.Name = "ListBoxInstance";
        IconInstance = new CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon();
        IconInstance.Name = "IconInstance";
        FocusedIndicator = new global::MonoGameGum.GueDeriving.NineSliceRuntime();
        FocusedIndicator.ElementSave = ObjectFinder.Self.GetStandardElement("NineSlice");
        if (FocusedIndicator.ElementSave != null) FocusedIndicator.AddStatesAndCategoriesRecursivelyToGue(FocusedIndicator.ElementSave);
        if (FocusedIndicator.ElementSave != null) FocusedIndicator.SetInitialState();
        FocusedIndicator.Name = "FocusedIndicator";
        base.RefreshInternalVisualReferences();
    }
    protected virtual void AssignParents()
    {
        this.AddChild(Background);
        this.AddChild(TextInstance);
        this.AddChild(ListBoxInstance);
        this.AddChild(IconInstance);
        this.AddChild(FocusedIndicator);
    }
    private void ApplyDefaultVariables()
    {
        this.Background.SetProperty("ColorCategoryState", "DarkGray");
        this.Background.SetProperty("StyleCategoryState", "Solid");
        this.Background.Height = 0f;
        this.Background.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.Background.Width = 0f;
        this.Background.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.Background.X = 0f;
        this.Background.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.Background.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        this.Background.Y = 0f;
        this.Background.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.Background.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.TextInstance.SetProperty("ColorCategoryState", "White");
        this.TextInstance.SetProperty("StyleCategoryState", "Strong");
        this.TextInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.TextInstance.Text = @"Selected Item";
        ((TextRuntime)this.TextInstance).VerticalAlignment = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.TextInstance.Width = -8f;
        this.TextInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.TextInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.TextInstance.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        this.TextInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.TextInstance.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.ListBoxInstance.Visual.Height = 128f;
        this.ListBoxInstance.Visual.Visible = false;
        this.ListBoxInstance.Visual.Width = 0f;
        this.ListBoxInstance.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.ListBoxInstance.Visual.Y = 28f;

        this.IconInstance.IconCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon.IconCategory.Arrow2;
        this.IconInstance.Visual.SetProperty("IconColor", "Primary");
        this.IconInstance.Visual.Height = 24f;
        this.IconInstance.Visual.Rotation = -90f;
        this.IconInstance.Visual.Width = 24f;
        this.IconInstance.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.IconInstance.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromLarge;
        this.IconInstance.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
        this.IconInstance.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

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
