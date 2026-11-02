//Code for Controls/StackPanel (Container)
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
[global::Gum.DataTypes.GumElement("Controls/StackPanel")]
partial class StackPanel : global::Gum.Forms.Controls.StackPanel
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
            var element = ObjectFinder.Self.GetRequiredElementSave("Controls/StackPanel");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new StackPanel(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(StackPanel)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Controls/StackPanel", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }

    public StackPanel(InteractiveGue visual) : base(visual)
    {
        InitializeInstancesFromVisual();
        ReactToGeneratedVisual();
        CustomInitialize();
    }
    public StackPanel() : base(new ContainerRuntime())
    {

        this.Visual.ChildrenLayout = global::Gum.Managers.ChildrenLayout.TopToBottomStack;

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
