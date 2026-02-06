//Code for Elements/CautionLines (Container)
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using System.Linq;
namespace CodeGen_MonoGameForms_Localization_ByReference.Components.Elements;
partial class CautionLines : global::Gum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Elements/CautionLines");
#if DEBUG
if(element == null) throw new System.InvalidOperationException("Could not find an element named Elements/CautionLines - did you forget to load a Gum project?");
#endif
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new CautionLines(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(CautionLines)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Elements/CautionLines", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public SpriteRuntime LinesSprite { get; protected set; }

    public int LineAlpha
    {
        get => LinesSprite.Alpha;
        set => LinesSprite.Alpha = value;
    }


    public CautionLines(InteractiveGue visual) : base(visual)
    {
    }
    public CautionLines()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        LinesSprite = this.Visual?.GetGraphicalUiElementByName("LinesSprite") as global::MonoGameGum.GueDeriving.SpriteRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    public void ApplyLocalization()
    {
    }
    partial void CustomInitialize();
}
