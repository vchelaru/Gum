//Code for Controls/Label (Text)
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
[global::Gum.DataTypes.GumElement("Controls/Label")]
partial class Label : global::Gum.Forms.Controls.Label
{
    #if UNITY_5_3_OR_NEWER
    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    #else
    [System.Runtime.CompilerServices.ModuleInitializer]
    #endif
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::Gum.GueDeriving.TextRuntime();
            var element = ObjectFinder.Self.GetRequiredElementSave("Controls/Label");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new Label(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(Label)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Controls/Label", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }

    public Label(InteractiveGue visual) : base(visual)
    {
        InitializeInstancesFromVisual();
        ReactToGeneratedVisual();
        CustomInitialize();
    }
    public Label() : base(new global::Gum.GueDeriving.TextRuntime())
    {

        ((global::Gum.GueDeriving.TextRuntime)this.Visual).SetProperty("ColorCategoryState", "White");
        ((global::Gum.GueDeriving.TextRuntime)this.Visual).Height = 0f;
        ((global::Gum.GueDeriving.TextRuntime)this.Visual).HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        ((global::Gum.GueDeriving.TextRuntime)this.Visual).SetProperty("StyleCategoryState", "Strong");
        ((global::Gum.GueDeriving.TextRuntime)this.Visual).Width = 0f;
        ((global::Gum.GueDeriving.TextRuntime)this.Visual).WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;

        InitializeInstances();

        ApplyDefaultVariables();
        AssignParents();
        ReactToGeneratedVisual();
        CustomInitialize();
    }
    protected virtual void InitializeInstances()
    {
    }
    protected virtual void InitializeInstancesFromVisual()
    {
    }
    protected virtual void AssignParents()
    {
    }
    protected virtual void ApplyDefaultVariables()
    {
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
