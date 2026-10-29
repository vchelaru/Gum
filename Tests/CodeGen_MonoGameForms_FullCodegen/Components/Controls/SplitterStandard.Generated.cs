//Code for Controls/SplitterStandard (Container)
using Gum;
using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary.Graphics;
using System.Linq;
namespace CodeGen_MonoGameForms_FullCodegen.Components.Controls;
[global::Gum.DataTypes.GumElement("Controls/SplitterStandard", FormsType = typeof(global::Gum.Forms.Controls.Splitter))]
partial class SplitterStandard : global::Gum.Forms.Controls.Splitter
{
    #if UNITY_5_3_OR_NEWER && !GUM_NO_UNITY_ENGINE
    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    #else
    [System.Runtime.CompilerServices.ModuleInitializer]
    #endif
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::Gum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetRequiredElementSave("Controls/SplitterStandard");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new SplitterStandard(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(SplitterStandard)] = template;
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(global::Gum.Forms.Controls.Splitter)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Controls/SplitterStandard", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public NineSliceRuntime NineSliceInstance { get; protected set; }

    public SplitterStandard(InteractiveGue visual) : base(visual)
    {
        InitializeInstancesFromVisual();
        ReactToGeneratedVisual();
        CustomInitialize();
    }
    public SplitterStandard() : base(new ContainerRuntime())
    {

        this.Visual.Height = 5f;
        this.Visual.Width = -0f;
        this.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.Visual.X = 0f;
        this.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        InitializeInstances();

        ApplyDefaultVariables();
        AssignParents();
        ReactToGeneratedVisual();
        CustomInitialize();
    }
    protected virtual void InitializeInstances()
    {
        NineSliceInstance = new global::Gum.GueDeriving.NineSliceRuntime();
        NineSliceInstance.ElementSave = ObjectFinder.Self.GetStandardElement("NineSlice");
        if (NineSliceInstance.ElementSave != null) NineSliceInstance.AddStatesAndCategoriesRecursivelyToGue(NineSliceInstance.ElementSave);
        if (NineSliceInstance.ElementSave != null) NineSliceInstance.SetInitialState();
        NineSliceInstance.Name = "NineSliceInstance";
    }
    protected virtual void InitializeInstancesFromVisual()
    {
        NineSliceInstance = this.Visual?.GetGraphicalUiElementByName("NineSliceInstance") as global::Gum.GueDeriving.NineSliceRuntime;
    }
    protected virtual void AssignParents()
    {
        this.Visual.AddChild(this.NineSliceInstance);
    }
    protected virtual void ApplyDefaultVariables()
    {
        this.NineSliceInstance.SetProperty("ColorCategoryState", "DarkGray");
        this.NineSliceInstance.SetProperty("StyleCategoryState", "Bordered");
        this.NineSliceInstance.Height = -0f;
        this.NineSliceInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.NineSliceInstance.Width = -0f;
        this.NineSliceInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.NineSliceInstance.X = 0f;
        this.NineSliceInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.NineSliceInstance.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        this.NineSliceInstance.Y = 0f;
        this.NineSliceInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.NineSliceInstance.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

    }
    private bool _isGeneratedVisualBuilt;
    protected override void ReactToVisualChanged()
    {
        if (_isGeneratedVisualBuilt) base.ReactToVisualChanged();
    }
    private void ReactToGeneratedVisual()
    {
        _isGeneratedVisualBuilt = true;
        ReactToVisualChanged();
    }
    partial void CustomInitialize();
}
