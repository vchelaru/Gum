//Code for ComponentWithExposedStandardVariables (Container)
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
partial class ComponentWithExposedStandardVariables : global::Gum.Forms.Controls.FrameworkElement
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
            var element = ObjectFinder.Self.GetElementSave("ComponentWithExposedStandardVariables") ?? throw new System.InvalidOperationException("Could not find an element named ComponentWithExposedStandardVariables - did you forget to load a Gum project?");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new ComponentWithExposedStandardVariables(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(ComponentWithExposedStandardVariables)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("ComponentWithExposedStandardVariables", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public SpriteRuntime BlendSprite { get; protected set; }
    public TextRuntime FontText { get; protected set; }
    public ContainerRuntime ShaderContainer { get; protected set; }
    public ContainerRuntime EventsContainer { get; protected set; }
    public SpriteRuntime EventsSprite { get; protected set; }

    public global::Gum.RenderingLibrary.Blend? SpriteBlend
    {
        get => BlendSprite.Blend;
        set => BlendSprite.Blend = value;
    }

    // Could not generate variable EventsContainer.ContainedType (string)[exposed as ContainerContainedType] because the runtime type has no property for it

    public bool ContainerHasEvents
    {
        get => EventsContainer.HasEvents;
        set => EventsContainer.HasEvents = value;
    }

    // Could not generate variable EventsSprite.HasEvents (bool)[exposed as SpriteHasEvents] because the runtime type has no property for it

    public float TextFontSize
    {
        get => FontText.FontSize;
        set => FontText.FontSize = value;
    }

    public string ContainerShaderFile
    {
        set => ShaderContainer.SourceShaderFile = value;
    }

    public ComponentWithExposedStandardVariables(InteractiveGue visual) : base(visual)
    {
    }
    public ComponentWithExposedStandardVariables()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        BlendSprite = this.Visual?.GetGraphicalUiElementByName("BlendSprite") as global::Gum.GueDeriving.SpriteRuntime;
        FontText = this.Visual?.GetGraphicalUiElementByName("FontText") as global::Gum.GueDeriving.TextRuntime;
        ShaderContainer = this.Visual?.GetGraphicalUiElementByName("ShaderContainer") as global::Gum.GueDeriving.ContainerRuntime;
        EventsContainer = this.Visual?.GetGraphicalUiElementByName("EventsContainer") as global::Gum.GueDeriving.ContainerRuntime;
        EventsSprite = this.Visual?.GetGraphicalUiElementByName("EventsSprite") as global::Gum.GueDeriving.SpriteRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
