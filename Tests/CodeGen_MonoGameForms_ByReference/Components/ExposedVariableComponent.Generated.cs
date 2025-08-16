//Code for ExposedVariableComponent (Container)
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

namespace CodeGenProject.Components;
partial class ExposedVariableComponent : MonoGameGum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::MonoGameGum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("ExposedVariableComponent");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new ExposedVariableComponent(visual);
            return visual;
        });
        global::MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(ExposedVariableComponent)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("ExposedVariableComponent", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public ContainerRuntime ContainerInstance { get; protected set; }

    public HorizontalAlignment ContainerInstanceXOrigin
    {
        get => ContainerInstance.XOrigin;
        set => ContainerInstance.XOrigin = value;
    }

    public global::Gum.Converters.GeneralUnitType ContainerInstanceXUnits
    {
        get => ContainerInstance.XUnits;
        set => ContainerInstance.XUnits = value;
    }

    public VerticalAlignment ContainerInstanceYOrigin
    {
        get => ContainerInstance.YOrigin;
        set => ContainerInstance.YOrigin = value;
    }

    public global::Gum.Converters.GeneralUnitType ContainerInstanceYUnits
    {
        get => ContainerInstance.YUnits;
        set => ContainerInstance.YUnits = value;
    }

    public ExposedVariableComponent(InteractiveGue visual) : base(visual)
    {
    }
    public ExposedVariableComponent()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        ContainerInstance = this.Visual?.GetGraphicalUiElementByName("ContainerInstance") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
