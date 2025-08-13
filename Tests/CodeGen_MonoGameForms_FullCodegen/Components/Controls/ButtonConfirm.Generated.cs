//Code for Controls/ButtonConfirm (Container)
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
partial class ButtonConfirm : global::MonoGameGum.Forms.Controls.Button
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::MonoGameGum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Controls/ButtonConfirm");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new ButtonConfirm(visual);
            return visual;
        });
        global::MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(ButtonConfirm)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Controls/ButtonConfirm", () => 
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
                        this.Background.SetProperty("ColorCategoryState", "Success");
                        this.FocusedIndicator.Visible = false;
                        this.TextInstance.SetProperty("ColorCategoryState", "White");
                        break;
                    case ButtonCategory.Disabled:
                        this.Background.SetProperty("ColorCategoryState", "DarkGray");
                        this.FocusedIndicator.Visible = false;
                        this.TextInstance.SetProperty("ColorCategoryState", "Gray");
                        break;
                    case ButtonCategory.Highlighted:
                        this.Background.SetProperty("ColorCategoryState", "PrimaryLight");
                        this.FocusedIndicator.Visible = false;
                        this.TextInstance.SetProperty("ColorCategoryState", "White");
                        break;
                    case ButtonCategory.Pushed:
                        this.Background.SetProperty("ColorCategoryState", "PrimaryDark");
                        this.FocusedIndicator.Visible = false;
                        this.TextInstance.SetProperty("ColorCategoryState", "White");
                        break;
                    case ButtonCategory.HighlightedFocused:
                        this.Background.SetProperty("ColorCategoryState", "PrimaryLight");
                        this.FocusedIndicator.Visible = true;
                        this.TextInstance.SetProperty("ColorCategoryState", "White");
                        break;
                    case ButtonCategory.Focused:
                        this.Background.SetProperty("ColorCategoryState", "Success");
                        this.FocusedIndicator.Visible = true;
                        this.TextInstance.SetProperty("ColorCategoryState", "White");
                        break;
                    case ButtonCategory.DisabledFocused:
                        this.Background.SetProperty("ColorCategoryState", "DarkGray");
                        this.FocusedIndicator.Visible = true;
                        this.TextInstance.SetProperty("ColorCategoryState", "Gray");
                        break;
                }
            }
        }
    }
    public NineSliceRuntime Background { get; protected set; }
    public TextRuntime TextInstance { get; protected set; }
    public NineSliceRuntime FocusedIndicator { get; protected set; }

    public string ButtonDisplayText
    {
        get => TextInstance.Text;
        set => TextInstance.Text = value;
    }

    public ButtonConfirm(InteractiveGue visual) : base(visual)
    {
        InitializeInstances();
        CustomInitialize();
    }
    public ButtonConfirm() : base(new ContainerRuntime())
    {

        this.Visual.Height = 32f;
         
        this.Visual.Width = 128f;

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
    }
    protected virtual void AssignParents()
    {
        this.AddChild(Background);
        this.AddChild(TextInstance);
        this.AddChild(FocusedIndicator);
    }
    private void ApplyDefaultVariables()
    {
        this.Background.SetProperty("ColorCategoryState", "Success");
        this.Background.SetProperty("StyleCategoryState", "Bordered");
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
        this.TextInstance.HorizontalAlignment = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.TextInstance.Text = @"Okay";
        this.TextInstance.VerticalAlignment = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.TextInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;

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
