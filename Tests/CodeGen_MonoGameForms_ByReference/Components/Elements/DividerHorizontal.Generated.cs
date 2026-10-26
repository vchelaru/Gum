//Code for Elements/DividerHorizontal (Container)
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
[global::Gum.DataTypes.GumElement("Elements/DividerHorizontal")]
partial class DividerHorizontal : global::Gum.Forms.Controls.FrameworkElement
{
    #if UNITY_5_3_OR_NEWER
    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    #else
    [System.Runtime.CompilerServices.ModuleInitializer]
    #endif
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::Gum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetRequiredElementSave("Elements/DividerHorizontal");
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
        AccentLeft = this.Visual?.GetGraphicalUiElementByName("AccentLeft") as global::Gum.GueDeriving.SpriteRuntime;
        Line = this.Visual?.GetGraphicalUiElementByName("Line") as global::Gum.GueDeriving.SpriteRuntime;
        AccentRight = this.Visual?.GetGraphicalUiElementByName("AccentRight") as global::Gum.GueDeriving.SpriteRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
