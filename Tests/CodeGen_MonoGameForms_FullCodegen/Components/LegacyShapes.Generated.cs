//Code for LegacyShapes (Container)
using Gum;
using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary.Graphics;
using System.Linq;
namespace CodeGen_MonoGameForms_FullCodegen.Components;
partial class LegacyShapes : global::Gum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::Gum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("LegacyShapes") ?? throw new System.InvalidOperationException("Could not find an element named LegacyShapes - did you forget to load a Gum project?");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new LegacyShapes(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(LegacyShapes)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("LegacyShapes", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public CircleRuntime FilledCircle { get; protected set; }
    public CircleRuntime OutlineCircle { get; protected set; }
    public RectangleRuntime RoundedCustom { get; protected set; }
    public RectangleRuntime RoundedDefault { get; protected set; }

    public LegacyShapes(InteractiveGue visual) : base(visual)
    {
        InitializeInstances();
        CustomInitialize();
    }
    public LegacyShapes() : base(new ContainerRuntime())
    {


        InitializeInstances();

        ApplyDefaultVariables();
        AssignParents();
        CustomInitialize();
    }
    protected virtual void InitializeInstances()
    {
        FilledCircle = new global::Gum.GueDeriving.CircleRuntime();
        FilledCircle.ElementSave = ObjectFinder.Self.GetStandardElement("ColoredCircle");
        if (FilledCircle.ElementSave != null) FilledCircle.AddStatesAndCategoriesRecursivelyToGue(FilledCircle.ElementSave);
        if (FilledCircle.ElementSave != null) FilledCircle.SetInitialState();
        FilledCircle.IsFilled = true;
        FilledCircle.StrokeWidth = 0f;
        FilledCircle.Name = "FilledCircle";
        OutlineCircle = new global::Gum.GueDeriving.CircleRuntime();
        OutlineCircle.ElementSave = ObjectFinder.Self.GetStandardElement("ColoredCircle");
        if (OutlineCircle.ElementSave != null) OutlineCircle.AddStatesAndCategoriesRecursivelyToGue(OutlineCircle.ElementSave);
        if (OutlineCircle.ElementSave != null) OutlineCircle.SetInitialState();
        OutlineCircle.IsFilled = false;
        OutlineCircle.StrokeWidth = 2f;
        OutlineCircle.Name = "OutlineCircle";
        RoundedCustom = new global::Gum.GueDeriving.RectangleRuntime();
        RoundedCustom.ElementSave = ObjectFinder.Self.GetStandardElement("RoundedRectangle");
        if (RoundedCustom.ElementSave != null) RoundedCustom.AddStatesAndCategoriesRecursivelyToGue(RoundedCustom.ElementSave);
        if (RoundedCustom.ElementSave != null) RoundedCustom.SetInitialState();
        RoundedCustom.IsFilled = true;
        RoundedCustom.StrokeWidth = 0f;
        RoundedCustom.CornerRadius = 5f;
        RoundedCustom.Name = "RoundedCustom";
        RoundedDefault = new global::Gum.GueDeriving.RectangleRuntime();
        RoundedDefault.ElementSave = ObjectFinder.Self.GetStandardElement("RoundedRectangle");
        if (RoundedDefault.ElementSave != null) RoundedDefault.AddStatesAndCategoriesRecursivelyToGue(RoundedDefault.ElementSave);
        if (RoundedDefault.ElementSave != null) RoundedDefault.SetInitialState();
        RoundedDefault.IsFilled = true;
        RoundedDefault.StrokeWidth = 0f;
        RoundedDefault.CornerRadius = 5f;
        RoundedDefault.Name = "RoundedDefault";
        base.RefreshInternalVisualReferences();
    }
    protected virtual void AssignParents()
    {
        this.AddChild(FilledCircle);
        this.AddChild(OutlineCircle);
        this.AddChild(RoundedCustom);
        this.AddChild(RoundedDefault);
    }
    private void ApplyDefaultVariables()
    {
        this.FilledCircle.FillBlue = 30;
        this.FilledCircle.FillGreen = 20;
        this.FilledCircle.FillRed = 10;

        this.OutlineCircle.IsFilled = false;
        this.OutlineCircle.StrokeRed = 200;
        this.OutlineCircle.StrokeWidth = 4f;

        this.RoundedCustom.CornerRadius = 8f;


    }
    partial void CustomInitialize();
}
