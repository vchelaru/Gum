//Code for Elements/DividerVertical (Container)
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

using MonoGameGum.GueDeriving;
namespace GameUiSamples.Components;
partial class DividerVertical : global::Gum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Elements/DividerVertical");
#if DEBUG
if(element == null) throw new System.InvalidOperationException("Could not find an element named Elements/DividerVertical - did you forget to load a Gum project?");
#endif
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
        AccentTop = this.Visual?.GetGraphicalUiElementByName("AccentTop") as global::MonoGameGum.GueDeriving.SpriteRuntime;
        Line = this.Visual?.GetGraphicalUiElementByName("Line") as global::MonoGameGum.GueDeriving.SpriteRuntime;
        AccentRight = this.Visual?.GetGraphicalUiElementByName("AccentRight") as global::MonoGameGum.GueDeriving.SpriteRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
