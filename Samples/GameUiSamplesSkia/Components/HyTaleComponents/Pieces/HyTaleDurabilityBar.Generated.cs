//Code for HyTaleComponents/Pieces/HyTaleDurabilityBar (Container)
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
namespace GameUiSamples.Components;
partial class HyTaleDurabilityBar : global::Gum.Forms.Controls.FrameworkElement
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
            var element = ObjectFinder.Self.GetElementSave("HyTaleComponents/Pieces/HyTaleDurabilityBar") ?? throw new System.InvalidOperationException("Could not find an element named HyTaleComponents/Pieces/HyTaleDurabilityBar - did you forget to load a Gum project?");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new HyTaleDurabilityBar(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(HyTaleDurabilityBar)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("HyTaleComponents/Pieces/HyTaleDurabilityBar", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public ColoredRectangleRuntime BackgroundBlack { get; protected set; }
    public ColoredRectangleRuntime BarPercent { get; protected set; }

    public float DurabilityRatioValue
    {
        get => BarPercent.Width;
        set => BarPercent.Width = value;
    }

    public HyTaleDurabilityBar(InteractiveGue visual) : base(visual)
    {
    }
    public HyTaleDurabilityBar()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        BackgroundBlack = this.Visual?.GetGraphicalUiElementByName("BackgroundBlack") as global::Gum.GueDeriving.ColoredRectangleRuntime;
        BarPercent = this.Visual?.GetGraphicalUiElementByName("BarPercent") as global::Gum.GueDeriving.ColoredRectangleRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
