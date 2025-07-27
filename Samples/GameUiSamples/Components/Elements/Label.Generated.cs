//Code for Elements/Label (Container)
using GumRuntime;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
namespace GameUiSamples.Components;
partial class Label : MonoGameGum.Forms.Controls.Label
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new MonoGameGum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Elements/Label");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new Label(visual);
            return visual;
        });
        MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(Label)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Elements/Label", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public TextRuntime TextInstance { get; protected set; }


    public HorizontalAlignment HorizontalAlignment
    {
        get => TextInstance.HorizontalAlignment;
        set => TextInstance.HorizontalAlignment = value;
    }


    public string LabelText
    {
        get => TextInstance.Text;
        set => TextInstance.Text = value;
    }

    public VerticalAlignment VerticalAlignment
    {
        get => TextInstance.VerticalAlignment;
        set => TextInstance.VerticalAlignment = value;
    }

    public Label(InteractiveGue visual) : base(visual) { }
    public Label()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        TextInstance = this.Visual?.GetGraphicalUiElementByName("TextInstance") as TextRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
