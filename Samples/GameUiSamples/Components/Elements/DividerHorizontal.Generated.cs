//Code for Elements/DividerHorizontal (Container)
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
partial class DividerHorizontal : global::Gum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Elements/DividerHorizontal");
#if DEBUG
if(element == null) throw new System.InvalidOperationException("Could not find an element named Elements/DividerHorizontal - did you forget to load a Gum project?");
#endif
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new DividerHorizontal(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(DividerHorizontal)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Elements/DividerHorizontal", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public SpriteRuntime AccentLeft { get; protected set; }
    public SpriteRuntime Line { get; protected set; }
    public SpriteRuntime AccentRight { get; protected set; }

    public DividerHorizontal(InteractiveGue visual) : base(visual)
    {
    }
    public DividerHorizontal()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        AccentLeft = this.Visual?.GetGraphicalUiElementByName("AccentLeft") as global::MonoGameGum.GueDeriving.SpriteRuntime;
        Line = this.Visual?.GetGraphicalUiElementByName("Line") as global::MonoGameGum.GueDeriving.SpriteRuntime;
        AccentRight = this.Visual?.GetGraphicalUiElementByName("AccentRight") as global::MonoGameGum.GueDeriving.SpriteRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
