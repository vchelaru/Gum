//Code for ComponentWithRenderTargetSource (Container)
using Gum;
using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary.Graphics;
using System.Linq;
namespace CodeGenProject.Components;
partial class ComponentWithRenderTargetSource : global::Gum.Forms.Controls.FrameworkElement
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
            var element = ObjectFinder.Self.GetElementSave("ComponentWithRenderTargetSource") ?? throw new System.InvalidOperationException("Could not find an element named ComponentWithRenderTargetSource - did you forget to load a Gum project?");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new ComponentWithRenderTargetSource(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(ComponentWithRenderTargetSource)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("ComponentWithRenderTargetSource", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public ContainerRuntime RenderTargetContainer { get; protected set; }
    public SpriteRuntime SourceSprite { get; protected set; }

    public global::RenderingLibrary.Graphics.IRenderableIpso RenderTargetSource
    {
        get => SourceSprite.RenderTargetTextureSource;
        set => SourceSprite.RenderTargetTextureSource = value;
    }

    public ComponentWithRenderTargetSource(InteractiveGue visual) : base(visual)
    {
    }
    public ComponentWithRenderTargetSource()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        RenderTargetContainer = this.Visual?.GetGraphicalUiElementByName("RenderTargetContainer") as global::Gum.GueDeriving.ContainerRuntime;
        SourceSprite = this.Visual?.GetGraphicalUiElementByName("SourceSprite") as global::Gum.GueDeriving.SpriteRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
