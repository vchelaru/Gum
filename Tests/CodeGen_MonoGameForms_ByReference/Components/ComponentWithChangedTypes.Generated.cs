//Code for ComponentWithChangedTypes (Container)
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using System.Linq;
namespace CodeGenProject.Components;
partial class ComponentWithChangedTypes : global::Gum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("ComponentWithChangedTypes");
#if DEBUG
if(element == null) throw new System.InvalidOperationException("Could not find an element named ComponentWithChangedTypes - did you forget to load a Gum project?");
#endif
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new ComponentWithChangedTypes(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(ComponentWithChangedTypes)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("ComponentWithChangedTypes", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public SpriteRuntime Icon { get; protected set; }

    // Could not generate variable Icon.IconCategoryState (IconCategory) = GamepadXbox[exposed as IconType] because it references a variable that doesn't exist

    public ComponentWithChangedTypes(InteractiveGue visual) : base(visual)
    {
    }
    public ComponentWithChangedTypes()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        Icon = this.Visual?.GetGraphicalUiElementByName("Icon") as global::MonoGameGum.GueDeriving.SpriteRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
