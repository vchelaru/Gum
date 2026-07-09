//Code for Elements/DividerVertical (Container)
using Gum;
using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.StateAnimation.Runtime;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary.Graphics;
using System.Linq;
namespace CodeGenProject.Components.Elements;
partial class DividerVertical : global::Gum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::Gum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Elements/DividerVertical") ?? throw new System.InvalidOperationException("Could not find an element named Elements/DividerVertical - did you forget to load a Gum project?");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new DividerVertical(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(DividerVertical)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Elements/DividerVertical", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public SpriteRuntime AccentTop { get; protected set; }
    public SpriteRuntime Line { get; protected set; }
    public SpriteRuntime AccentRight { get; protected set; }

    public DividerVertical(InteractiveGue visual) : base(visual)
    {
    }
    public DividerVertical()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        AccentTop = this.Visual?.GetGraphicalUiElementByName("AccentTop") as global::Gum.GueDeriving.SpriteRuntime;
        Line = this.Visual?.GetGraphicalUiElementByName("Line") as global::Gum.GueDeriving.SpriteRuntime;
        AccentRight = this.Visual?.GetGraphicalUiElementByName("AccentRight") as global::Gum.GueDeriving.SpriteRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
