//Code for Controls/Label (Text)
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

namespace CodeGenProject.Components.Controls;
partial class Label : global::MonoGameGum.Forms.Controls.Label
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::MonoGameGum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.TextRuntime();
            var element = ObjectFinder.Self.GetElementSave("Controls/Label");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new Label(visual);
            return visual;
        });
        global::MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(Label)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Controls/Label", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }

    public Label(InteractiveGue visual) : base(visual)
    {
    }
    public Label()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
