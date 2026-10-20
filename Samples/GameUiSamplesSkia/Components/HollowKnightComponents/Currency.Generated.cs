//Code for HollowKnightComponents/Currency (Container)
using Gum;
using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary.Graphics;
using System.Linq;
namespace GameUiSamples.Components;
partial class Currency : global::Gum.Forms.Controls.FrameworkElement
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
            var element = ObjectFinder.Self.GetElementSave("HollowKnightComponents/Currency") ?? throw new System.InvalidOperationException("Could not find an element named HollowKnightComponents/Currency - did you forget to load a Gum project?");
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
        SpriteInstance = this.Visual?.GetGraphicalUiElementByName("SpriteInstance") as global::Gum.GueDeriving.SpriteRuntime;
        TotalMoneyTextInstance = this.Visual?.GetGraphicalUiElementByName("TotalMoneyTextInstance") as global::Gum.GueDeriving.TextRuntime;
        ToAddTextInstance = this.Visual?.GetGraphicalUiElementByName("ToAddTextInstance") as global::Gum.GueDeriving.TextRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
