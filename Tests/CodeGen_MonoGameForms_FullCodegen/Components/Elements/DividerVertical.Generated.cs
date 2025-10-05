//Code for Elements/DividerVertical (Container)
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

namespace CodeGen_MonoGameForms_FullCodegen.Components.Elements;
partial class DividerVertical : global::Gum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Elements/DividerVertical");
#if DEBUG
if(element == null) throw new System.InvalidOperationException("Could not find an element named Elements/DividerVertical - did you forget to load a Gum project?");
#endif
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new DividerVertical(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(DividerVertical)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Elements/DividerVertical", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public SpriteRuntime AccentTop { get; protected set; }
    public SpriteRuntime Line { get; protected set; }
    public SpriteRuntime AccentRight { get; protected set; }

    public DividerVertical(InteractiveGue visual) : base(visual)
    {
        InitializeInstances();
        CustomInitialize();
    }
    public DividerVertical() : base(new ContainerRuntime())
    {

        this.Visual.Height = 128f;
         
        this.Visual.Width = 0f;
        this.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;

        InitializeInstances();

        ApplyDefaultVariables();
        AssignParents();
        CustomInitialize();
    }
    protected virtual void InitializeInstances()
    {
        AccentTop = new global::MonoGameGum.GueDeriving.SpriteRuntime();
        AccentTop.ElementSave = ObjectFinder.Self.GetStandardElement("Sprite");
        if (AccentTop.ElementSave != null) AccentTop.AddStatesAndCategoriesRecursivelyToGue(AccentTop.ElementSave);
        if (AccentTop.ElementSave != null) AccentTop.SetInitialState();
        AccentTop.Name = "AccentTop";
        Line = new global::MonoGameGum.GueDeriving.SpriteRuntime();
        Line.ElementSave = ObjectFinder.Self.GetStandardElement("Sprite");
        if (Line.ElementSave != null) Line.AddStatesAndCategoriesRecursivelyToGue(Line.ElementSave);
        if (Line.ElementSave != null) Line.SetInitialState();
        Line.Name = "Line";
        AccentRight = new global::MonoGameGum.GueDeriving.SpriteRuntime();
        AccentRight.ElementSave = ObjectFinder.Self.GetStandardElement("Sprite");
        if (AccentRight.ElementSave != null) AccentRight.AddStatesAndCategoriesRecursivelyToGue(AccentRight.ElementSave);
        if (AccentRight.ElementSave != null) AccentRight.SetInitialState();
        AccentRight.Name = "AccentRight";
        base.RefreshInternalVisualReferences();
    }
    protected virtual void AssignParents()
    {
        this.AddChild(AccentTop);
        this.AddChild(Line);
        this.AddChild(AccentRight);
    }
    private void ApplyDefaultVariables()
    {
        this.AccentTop.SetProperty("ColorCategoryState", "Gray");
        this.AccentTop.Height = 100f;
        this.AccentTop.HeightUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
        this.AccentTop.SourceFileName = @"UISpriteSheet.png";
        this.AccentTop.TextureAddress = global::Gum.Managers.TextureAddress.Custom;
        this.AccentTop.TextureHeight = 3;
        this.AccentTop.TextureLeft = 281;
        this.AccentTop.TextureTop = 0;
        this.AccentTop.TextureWidth = 3;
        this.AccentTop.Width = 100f;
        this.AccentTop.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.AccentTop.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.Line.SetProperty("ColorCategoryState", "Gray");
        this.Line.Height = -8f;
        this.Line.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.Line.SourceFileName = @"UISpriteSheet.png";
        this.Line.TextureAddress = global::Gum.Managers.TextureAddress.Custom;
        this.Line.TextureHeight = 1;
        this.Line.TextureLeft = 281;
        this.Line.TextureTop = 1;
        this.Line.TextureWidth = 3;
        this.Line.Width = 1f;
        this.Line.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
        this.Line.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.Line.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        this.Line.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.Line.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.AccentRight.SetProperty("ColorCategoryState", "Gray");
        this.AccentRight.Height = 100f;
        this.AccentRight.HeightUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
        this.AccentRight.SourceFileName = @"UISpriteSheet.png";
        this.AccentRight.TextureAddress = global::Gum.Managers.TextureAddress.Custom;
        this.AccentRight.TextureHeight = 3;
        this.AccentRight.TextureLeft = 281;
        this.AccentRight.TextureTop = 0;
        this.AccentRight.TextureWidth = 3;
        this.AccentRight.Width = 100f;
        this.AccentRight.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.AccentRight.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        this.AccentRight.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Bottom;
        this.AccentRight.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromLarge;

    }
    partial void CustomInitialize();
}
