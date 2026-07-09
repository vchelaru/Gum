//Code for Controls/Panel (Container)
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
partial class Panel : global::Gum.Forms.Controls.Panel
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::Gum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Controls/Panel") ?? throw new System.InvalidOperationException("Could not find an element named Controls/Panel - did you forget to load a Gum project?");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new Panel(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(Panel)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Controls/Panel", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }

    public Panel(InteractiveGue visual) : base(visual)
    {
        InitializeInstances();
        CustomInitialize();
    }
    public Panel() : base(new ContainerRuntime())
    {


        InitializeInstances();

        ApplyDefaultVariables();
        AssignParents();
        CustomInitialize();
    }
    protected virtual void InitializeInstances()
    {
        base.RefreshInternalVisualReferences();
    }
    protected virtual void AssignParents()
    {
    }
    private void ApplyDefaultVariables()
    {
    }
    partial void CustomInitialize();
}
