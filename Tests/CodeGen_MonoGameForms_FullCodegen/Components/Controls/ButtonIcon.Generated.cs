//Code for Controls/ButtonIcon (Container)
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
partial class ButtonIcon : global::MonoGameGum.Forms.Controls.Button
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::MonoGameGum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Controls/ButtonIcon");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new ButtonIcon(visual);
            return visual;
        });
        global::MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(ButtonIcon)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Controls/ButtonIcon", () => 
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

    ButtonCategory? mButtonCategoryState;
    public ButtonCategory? ButtonCategoryState
    {
        get => mButtonCategoryState;
        set
        {
            mButtonCategoryState = value;
            var appliedDynamically = false;
            if(!appliedDynamically)
            {
                switch (value)
                {
                    case ButtonCategory.Enabled:
                        this.Background.SetProperty("ColorCategoryState", "Primary");
                        this.FocusedIndicator.Visible = false;
                        this.Icon.Visual.SetProperty("IconColor", "White");
                        break;
                    case ButtonCategory.Disabled:
                        this.Background.SetProperty("ColorCategoryState", "DarkGray");
                        this.FocusedIndicator.Visible = false;
                        this.Icon.Visual.SetProperty("IconColor", "Gray");
                        break;
                    case ButtonCategory.Highlighted:
                        this.Background.SetProperty("ColorCategoryState", "PrimaryLight");
                        this.FocusedIndicator.Visible = false;
                        this.Icon.Visual.SetProperty("IconColor", "White");
                        break;
                    case ButtonCategory.Pushed:
                        this.Background.SetProperty("ColorCategoryState", "PrimaryDark");
                        this.FocusedIndicator.Visible = false;
                        this.Icon.Visual.SetProperty("IconColor", "White");
                        break;
                    case ButtonCategory.HighlightedFocused:
                        this.Background.SetProperty("ColorCategoryState", "PrimaryLight");
                        this.FocusedIndicator.Visible = true;
                        this.Icon.Visual.SetProperty("IconColor", "White");
                        break;
                    case ButtonCategory.Focused:
                        this.Background.SetProperty("ColorCategoryState", "Primary");
                        this.FocusedIndicator.Visible = true;
                        this.Icon.Visual.SetProperty("IconColor", "White");
                        break;
                    case ButtonCategory.DisabledFocused:
                        this.Background.SetProperty("ColorCategoryState", "DarkGray");
                        this.FocusedIndicator.Visible = true;
                        this.Icon.Visual.SetProperty("IconColor", "Gray");
                        break;
                }
            }
        }
    }
    public NineSliceRuntime Background { get; protected set; }
    public Icon Icon { get; protected set; }
    public NineSliceRuntime FocusedIndicator { get; protected set; }

    public Icon.IconCategory? IconCategory
    {
        get => Icon.IconCategoryState;
        set => Icon.IconCategoryState = value;
    }

    public ButtonIcon(InteractiveGue visual) : base(visual)
    {
        InitializeInstances();
        CustomInitialize();
    }
    public ButtonIcon() : base(new ContainerRuntime())
    {

         
        this.Visual.Height = 32f;
        this.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
         
        this.Visual.Width = 32f;
        this.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;

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
        Icon = new Icon();
        Icon.Name = "Icon";
        FocusedIndicator = new global::MonoGameGum.GueDeriving.NineSliceRuntime();
        FocusedIndicator.ElementSave = ObjectFinder.Self.GetStandardElement("NineSlice");
        if (FocusedIndicator.ElementSave != null) FocusedIndicator.AddStatesAndCategoriesRecursivelyToGue(FocusedIndicator.ElementSave);
        if (FocusedIndicator.ElementSave != null) FocusedIndicator.SetInitialState();
        FocusedIndicator.Name = "FocusedIndicator";
    }
    protected virtual void AssignParents()
    {
        this.AddChild(Background);
        this.AddChild(Icon);
        this.AddChild(FocusedIndicator);
    }
    private void ApplyDefaultVariables()
    {
        this.Background.SetProperty("ColorCategoryState", "Primary");
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

        this.Icon.Visual.Height = 0f;
        this.Icon.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.Icon.Visual.Width = 0f;
        this.Icon.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.Icon.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.Icon.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        this.Icon.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.Icon.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

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
