//Code for GeneralScreen
using CodeGen_MonoGameForms_FullCodegen.Components.Controls;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using System.Linq;
namespace CodeGen_MonoGameForms_FullCodegen.Screens;
partial class GeneralScreen : global::Gum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("GeneralScreen");
#if DEBUG
if(element == null) throw new System.InvalidOperationException("Could not find an element named GeneralScreen - did you forget to load a Gum project?");
#endif
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new GeneralScreen(visual);
            visual.Width = 0;
            visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
            visual.Height = 0;
            visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(GeneralScreen)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("GeneralScreen", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public CircleRuntime CircleInstance { get; protected set; }
    public Label LabelInstance { get; protected set; }

    public HorizontalAlignment LabelInstanceHorizontalAlignment
    {
        get => ((TextRuntime) LabelInstance.Visual).HorizontalAlignment;
        set => ((TextRuntime)LabelInstance.Visual).HorizontalAlignment = value;
    }

    public int? LabelInstanceMaxLettersToShow
    {
        get => ((TextRuntime) LabelInstance.Visual).MaxLettersToShow;
        set => ((TextRuntime)LabelInstance.Visual).MaxLettersToShow = value;
    }

    public int? LabelInstanceMaxNumberOfLines
    {
        get => ((TextRuntime) LabelInstance.Visual).MaxNumberOfLines;
        set => ((TextRuntime)LabelInstance.Visual).MaxNumberOfLines = value;
    }

    public TextOverflowHorizontalMode LabelInstanceTextOverflowHorizontalMode
    {
        get => ((TextRuntime) LabelInstance.Visual).TextOverflowHorizontalMode;
        set => ((TextRuntime)LabelInstance.Visual).TextOverflowHorizontalMode = value;
    }

    public TextOverflowVerticalMode LabelInstanceTextOverflowVerticalMode
    {
        get => LabelInstance.Visual.TextOverflowVerticalMode;
        set => LabelInstance.Visual.TextOverflowVerticalMode = value;
    }

    public VerticalAlignment LabelInstanceVerticalAlignment
    {
        get => ((TextRuntime) LabelInstance.Visual).VerticalAlignment;
        set => ((TextRuntime)LabelInstance.Visual).VerticalAlignment = value;
    }

    public GeneralScreen(InteractiveGue visual) : base(visual)
    {
        InitializeInstances();
        CustomInitialize();
    }
    public GeneralScreen() : base(new ContainerRuntime())
    {


        InitializeInstances();

        ApplyDefaultVariables();
        AssignParents();
        CustomInitialize();
    }
    protected virtual void InitializeInstances()
    {
        CircleInstance = new global::MonoGameGum.GueDeriving.CircleRuntime();
        CircleInstance.ElementSave = ObjectFinder.Self.GetStandardElement("Circle");
        if (CircleInstance.ElementSave != null) CircleInstance.AddStatesAndCategoriesRecursivelyToGue(CircleInstance.ElementSave);
        if (CircleInstance.ElementSave != null) CircleInstance.SetInitialState();
        CircleInstance.Name = "CircleInstance";
        LabelInstance = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.Label();
        LabelInstance.Name = "LabelInstance";
        base.RefreshInternalVisualReferences();
    }
    protected virtual void AssignParents()
    {
        LabelInstance.AddChild(CircleInstance);
        this.AddChild(LabelInstance);
    }
    private void ApplyDefaultVariables()
    {
        this.Visual.Width = 0f;
        this.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.Visual.Height = 0f;
        this.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.CircleInstance.X = 197f;
        this.CircleInstance.Y = 110f;

        ((TextRuntime)this.LabelInstance.Visual).HorizontalAlignment = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        ((TextRuntime)this.LabelInstance.Visual).MaxLettersToShow = 55;
        ((TextRuntime)this.LabelInstance.Visual).MaxNumberOfLines = 3;
        this.LabelInstance.Text = @"Hello1234";
        ((TextRuntime)this.LabelInstance.Visual).TextOverflowHorizontalMode = global::RenderingLibrary.Graphics.TextOverflowHorizontalMode.EllipsisLetter;
        this.LabelInstance.Visual.TextOverflowVerticalMode = global::RenderingLibrary.Graphics.TextOverflowVerticalMode.TruncateLine;
        ((TextRuntime)this.LabelInstance.Visual).VerticalAlignment = global::RenderingLibrary.Graphics.VerticalAlignment.Center;

    }
    partial void CustomInitialize();
}
