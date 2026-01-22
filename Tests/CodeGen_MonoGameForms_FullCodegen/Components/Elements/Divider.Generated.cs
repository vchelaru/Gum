//Code for Elements/Divider (Container)
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
partial class Divider : global::Gum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Elements/Divider");
#if DEBUG
if(element == null) throw new System.InvalidOperationException("Could not find an element named Elements/Divider - did you forget to load a Gum project?");
#endif
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new Divider(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(Divider)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Elements/Divider", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public SpriteRuntime AccentLeft { get; protected set; }
    public SpriteRuntime Line { get; protected set; }
    public SpriteRuntime AccentRight { get; protected set; }

    public Divider(InteractiveGue visual) : base(visual)
    {
        InitializeInstances();
        CustomInitialize();
    }
    public Divider() : base(new ContainerRuntime())
    {

        this.Visual.Height = 0f;
        this.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        this.Visual.Width = 128f;

        InitializeInstances();

        ApplyDefaultVariables();
        AssignParents();
        CustomInitialize();
    }
    protected virtual void InitializeInstances()
    {
        AccentLeft = new global::MonoGameGum.GueDeriving.SpriteRuntime();
        AccentLeft.ElementSave = ObjectFinder.Self.GetStandardElement("Sprite");
        if (AccentLeft.ElementSave != null) AccentLeft.AddStatesAndCategoriesRecursivelyToGue(AccentLeft.ElementSave);
        if (AccentLeft.ElementSave != null) AccentLeft.SetInitialState();
        AccentLeft.Name = "AccentLeft";
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
        this.AddChild(AccentLeft);
        this.AddChild(Line);
        this.AddChild(AccentRight);
    }
    private void ApplyDefaultVariables()
    {
        this.AccentLeft.SetProperty("ColorCategoryState", "Gray");
        this.AccentLeft.Height = 100f;
        this.AccentLeft.HeightUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
        this.AccentLeft.SourceFileName = @"UISpriteSheet.png";
        this.AccentLeft.TextureAddress = global::Gum.Managers.TextureAddress.Custom;
        this.AccentLeft.TextureHeight = 3;
        this.AccentLeft.TextureLeft = 281;
        this.AccentLeft.TextureTop = 0;
        this.AccentLeft.TextureWidth = 3;
        this.AccentLeft.Width = 100f;

        this.Line.SetProperty("ColorCategoryState", "Gray");
        this.Line.Height = 100f;
        this.Line.HeightUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
        this.Line.SourceFileName = @"UISpriteSheet.png";
        this.Line.TextureAddress = global::Gum.Managers.TextureAddress.Custom;
        this.Line.TextureHeight = 1;
        this.Line.TextureLeft = 281;
        this.Line.TextureTop = 1;
        this.Line.TextureWidth = 3;
        this.Line.Width = -10f;
        this.Line.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
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
        this.AccentRight.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
        this.AccentRight.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromLarge;

    }
    partial void CustomInitialize();
}
