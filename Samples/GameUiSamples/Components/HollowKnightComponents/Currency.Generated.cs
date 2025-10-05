//Code for HollowKnightComponents/Currency (Container)
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
partial class Currency : global::Gum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("HollowKnightComponents/Currency");
#if DEBUG
if(element == null) throw new System.InvalidOperationException("Could not find an element named HollowKnightComponents/Currency - did you forget to load a Gum project?");
#endif
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new Currency(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(Currency)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("HollowKnightComponents/Currency", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public SpriteRuntime SpriteInstance { get; protected set; }
    public TextRuntime TotalMoneyTextInstance { get; protected set; }
    public TextRuntime ToAddTextInstance { get; protected set; }

    public Currency(InteractiveGue visual) : base(visual)
    {
    }
    public Currency()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        SpriteInstance = this.Visual?.GetGraphicalUiElementByName("SpriteInstance") as global::MonoGameGum.GueDeriving.SpriteRuntime;
        TotalMoneyTextInstance = this.Visual?.GetGraphicalUiElementByName("TotalMoneyTextInstance") as global::MonoGameGum.GueDeriving.TextRuntime;
        ToAddTextInstance = this.Visual?.GetGraphicalUiElementByName("ToAddTextInstance") as global::MonoGameGum.GueDeriving.TextRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
