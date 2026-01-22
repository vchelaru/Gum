//Code for Elements/PercentBarIcon (Container)
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
namespace CodeGen_MonoGameForms_FullCodegen.Components.Elements;
partial class PercentBarIcon : global::Gum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Elements/PercentBarIcon");
#if DEBUG
if(element == null) throw new System.InvalidOperationException("Could not find an element named Elements/PercentBarIcon - did you forget to load a Gum project?");
#endif
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new PercentBarIcon(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(PercentBarIcon)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Elements/PercentBarIcon", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public enum BarDecorCategory
    {
        None,
        CautionLines,
        VerticalLines,
    }

    private BarDecorCategory? _barDecorCategoryState;
    public BarDecorCategory? BarDecorCategoryState
    {
        get => _barDecorCategoryState;
        set
        {
            _barDecorCategoryState = value;
            var appliedDynamically = false;
            if(!appliedDynamically)
            {
                switch (value)
                {
                    case BarDecorCategory.None:
                        this.CautionLinesInstance.Visual.Visible = false;
                        this.VerticalLinesInstance.Visual.Visible = false;
                        break;
                    case BarDecorCategory.CautionLines:
                        this.CautionLinesInstance.Visual.Visible = true;
                        this.VerticalLinesInstance.Visual.Visible = false;
                        break;
                    case BarDecorCategory.VerticalLines:
                        this.CautionLinesInstance.Visual.Visible = false;
                        this.VerticalLinesInstance.Visual.Visible = true;
                        break;
                }
            }
        }
    }
    public NineSliceRuntime Background { get; protected set; }
    public Icon IconInstance { get; protected set; }
    public NineSliceRuntime BarContainer { get; protected set; }
    public NineSliceRuntime Bar { get; protected set; }
    public CautionLines CautionLinesInstance { get; protected set; }
    public VerticalLines VerticalLinesInstance { get; protected set; }


    public float BarPercent
    {
        get => Bar.Width;
        set => Bar.Width = value;
    }

    public Icon.IconCategory? BarIcon
    {
        get => IconInstance.IconCategoryState;
        set => IconInstance.IconCategoryState = value;
    }


    public PercentBarIcon(InteractiveGue visual) : base(visual)
    {
        InitializeInstances();
        CustomInitialize();
    }
    public PercentBarIcon() : base(new ContainerRuntime())
    {

        this.Visual.Height = 16f;
         
        this.Visual.Width = 128f;

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
        IconInstance = new CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon();
        IconInstance.Name = "IconInstance";
        BarContainer = new global::MonoGameGum.GueDeriving.NineSliceRuntime();
        BarContainer.ElementSave = ObjectFinder.Self.GetStandardElement("NineSlice");
        if (BarContainer.ElementSave != null) BarContainer.AddStatesAndCategoriesRecursivelyToGue(BarContainer.ElementSave);
        if (BarContainer.ElementSave != null) BarContainer.SetInitialState();
        BarContainer.Name = "BarContainer";
        Bar = new global::MonoGameGum.GueDeriving.NineSliceRuntime();
        Bar.ElementSave = ObjectFinder.Self.GetStandardElement("NineSlice");
        if (Bar.ElementSave != null) Bar.AddStatesAndCategoriesRecursivelyToGue(Bar.ElementSave);
        if (Bar.ElementSave != null) Bar.SetInitialState();
        Bar.Name = "Bar";
        CautionLinesInstance = new CodeGen_MonoGameForms_FullCodegen.Components.Elements.CautionLines();
        CautionLinesInstance.Name = "CautionLinesInstance";
        VerticalLinesInstance = new CodeGen_MonoGameForms_FullCodegen.Components.Elements.VerticalLines();
        VerticalLinesInstance.Name = "VerticalLinesInstance";
        base.RefreshInternalVisualReferences();
    }
    protected virtual void AssignParents()
    {
        this.AddChild(Background);
        this.AddChild(IconInstance);
        this.AddChild(BarContainer);
        BarContainer.AddChild(Bar);
        Bar.AddChild(CautionLinesInstance);
        Bar.AddChild(VerticalLinesInstance);
    }
    private void ApplyDefaultVariables()
    {
        this.Background.SetProperty("ColorCategoryState", "DarkGray");
        this.Background.SetProperty("StyleCategoryState", "Bordered");

        this.IconInstance.IconCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon.IconCategory.Heart;
        this.IconInstance.Visual.SetProperty("IconColor", "Danger");
        this.IconInstance.Visual.Height = -4f;
        this.IconInstance.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.IconInstance.Visual.Width = 100f;
        this.IconInstance.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfOtherDimension;
        this.IconInstance.Visual.X = 2f;
        this.IconInstance.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.IconInstance.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.BarContainer.SetProperty("ColorCategoryState", "Black");
        this.BarContainer.SetProperty("StyleCategoryState", "Solid");
        this.BarContainer.Height = -4f;
        this.BarContainer.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.BarContainer.Width = -18f;
        this.BarContainer.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.BarContainer.X = -2f;
        this.BarContainer.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
        this.BarContainer.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromLarge;
        this.BarContainer.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.BarContainer.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.Bar.SetProperty("ColorCategoryState", "Danger");
        this.Bar.SetProperty("StyleCategoryState", "Solid");
        this.Bar.Width = 25f;
        this.Bar.WidthUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.Bar.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
        this.Bar.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;

        this.CautionLinesInstance.Visual.SetProperty("LineColor", "Black");
        this.CautionLinesInstance.Visual.Height = 0f;
        this.CautionLinesInstance.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.CautionLinesInstance.LineAlpha = 50;
        this.CautionLinesInstance.Visual.Visible = false;
        this.CautionLinesInstance.Visual.Width = 0f;
        this.CautionLinesInstance.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;

        this.VerticalLinesInstance.Visual.SetProperty("LineColor", "Black");
        this.VerticalLinesInstance.Visual.Height = 0f;
        this.VerticalLinesInstance.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.VerticalLinesInstance.LineAlpha = 50;
        this.VerticalLinesInstance.Visual.Visible = false;
        this.VerticalLinesInstance.Visual.Width = 0f;
        this.VerticalLinesInstance.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;

    }
    partial void CustomInitialize();
}
