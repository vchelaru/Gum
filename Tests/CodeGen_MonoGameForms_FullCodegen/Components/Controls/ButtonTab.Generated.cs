//Code for Controls/ButtonTab (Container)
using GumRuntime;
using System.Linq;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

namespace CodeGen_MonoGameForms_FullCodegen.Components.Controls;
partial class ButtonTab : global::MonoGameGum.Forms.Controls.Button
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::MonoGameGum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Controls/ButtonTab");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new ButtonTab(visual);
            return visual;
        });
        global::MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(ButtonTab)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Controls/ButtonTab", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public enum ButtonCategory
    {
        Enabled,
        Disabled,
        Highlighted,
        Pushed,
        HighlightedFocused,
        Focused,
        DisabledFocused,
    }

    private ButtonCategory? _buttonCategoryState;
    public ButtonCategory? ButtonCategoryState
    {
        get => _buttonCategoryState;
        set
        {
            _buttonCategoryState = value;
            var appliedDynamically = false;
            if(!appliedDynamically)
            {
                switch (value)
                {
                    case ButtonCategory.Enabled:
                        this.Background.SetProperty("ColorCategoryState", "Primary");
                        this.FocusedIndicator.Visible = false;
                        this.TabText.SetProperty("ColorCategoryState", "White");
                        break;
                    case ButtonCategory.Disabled:
                        this.Background.SetProperty("ColorCategoryState", "DarkGray");
                        this.FocusedIndicator.Visible = false;
                        this.TabText.SetProperty("ColorCategoryState", "Gray");
                        break;
                    case ButtonCategory.Highlighted:
                        this.Background.SetProperty("ColorCategoryState", "PrimaryLight");
                        this.FocusedIndicator.Visible = false;
                        this.TabText.SetProperty("ColorCategoryState", "White");
                        break;
                    case ButtonCategory.Pushed:
                        this.Background.SetProperty("ColorCategoryState", "PrimaryDark");
                        this.FocusedIndicator.Visible = false;
                        this.TabText.SetProperty("ColorCategoryState", "White");
                        break;
                    case ButtonCategory.HighlightedFocused:
                        this.Background.SetProperty("ColorCategoryState", "PrimaryLight");
                        this.FocusedIndicator.Visible = true;
                        this.TabText.SetProperty("ColorCategoryState", "White");
                        break;
                    case ButtonCategory.Focused:
                        this.Background.SetProperty("ColorCategoryState", "Primary");
                        this.FocusedIndicator.Visible = true;
                        this.TabText.SetProperty("ColorCategoryState", "White");
                        break;
                    case ButtonCategory.DisabledFocused:
                        this.Background.SetProperty("ColorCategoryState", "DarkGray");
                        this.FocusedIndicator.Visible = true;
                        this.TabText.SetProperty("ColorCategoryState", "Gray");
                        break;
                }
            }
        }
    }
    public NineSliceRuntime Background { get; protected set; }
    public TextRuntime TabText { get; protected set; }
    public NineSliceRuntime FocusedIndicator { get; protected set; }

    public string TabDisplayText
    {
        get => TabText.Text;
        set => TabText.Text = value;
    }

    public ButtonTab(InteractiveGue visual) : base(visual)
    {
        InitializeInstances();
        CustomInitialize();
    }
    public ButtonTab() : base(new ContainerRuntime())
    {

        this.Visual.Height = 32f;
         
        this.Visual.Width = 0f;
        this.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        this.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        this.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Bottom;

        InitializeInstances();

        ApplyDefaultVariables();
        AssignParents();
        CustomInitialize();
    }
    protected virtual void InitializeInstances()
    {
        base.ReactToVisualChanged();
        Background = new global::MonoGameGum.GueDeriving.NineSliceRuntime();
        Background.ElementSave = ObjectFinder.Self.GetStandardElement("NineSlice");
        if (Background.ElementSave != null) Background.AddStatesAndCategoriesRecursivelyToGue(Background.ElementSave);
        if (Background.ElementSave != null) Background.SetInitialState();
        Background.Name = "Background";
        TabText = new global::MonoGameGum.GueDeriving.TextRuntime();
        TabText.ElementSave = ObjectFinder.Self.GetStandardElement("Text");
        if (TabText.ElementSave != null) TabText.AddStatesAndCategoriesRecursivelyToGue(TabText.ElementSave);
        if (TabText.ElementSave != null) TabText.SetInitialState();
        TabText.Name = "TabText";
        FocusedIndicator = new global::MonoGameGum.GueDeriving.NineSliceRuntime();
        FocusedIndicator.ElementSave = ObjectFinder.Self.GetStandardElement("NineSlice");
        if (FocusedIndicator.ElementSave != null) FocusedIndicator.AddStatesAndCategoriesRecursivelyToGue(FocusedIndicator.ElementSave);
        if (FocusedIndicator.ElementSave != null) FocusedIndicator.SetInitialState();
        FocusedIndicator.Name = "FocusedIndicator";
    }
    protected virtual void AssignParents()
    {
        this.AddChild(Background);
        Background.AddChild(TabText);
        Background.AddChild(FocusedIndicator);
    }
    private void ApplyDefaultVariables()
    {
        this.Background.SetProperty("ColorCategoryState", "Primary");
        this.Background.SetProperty("StyleCategoryState", "TabBordered");
        this.Background.Width = 32f;
        this.Background.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;

        this.TabText.SetProperty("ColorCategoryState", "White");
        this.TabText.SetProperty("StyleCategoryState", "Strong");
        this.TabText.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.TabText.HorizontalAlignment = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.TabText.Text = @"Tab 1";
        this.TabText.VerticalAlignment = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.TabText.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.TabText.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        this.TabText.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.TabText.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.FocusedIndicator.SetProperty("ColorCategoryState", "Warning");
        this.FocusedIndicator.SetProperty("StyleCategoryState", "Solid");
        this.FocusedIndicator.Height = 2f;
        this.FocusedIndicator.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
        this.FocusedIndicator.Visible = false;
        this.FocusedIndicator.Width = -8f;
        this.FocusedIndicator.Y = 2f;
        this.FocusedIndicator.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
        this.FocusedIndicator.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromLarge;

    }
    partial void CustomInitialize();
}
