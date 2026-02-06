//Code for Controls/Window (Container)
using CodeGen_MonoGameForms_Localization_ByReference.Components.Controls;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.StateAnimation.Runtime;
using Gum.Wireframe;
using GumRuntime;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using System.Linq;
namespace CodeGen_MonoGameForms_Localization_ByReference.Components.Controls;
partial class Window : global::Gum.Forms.Window
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Controls/Window");
#if DEBUG
if(element == null) throw new System.InvalidOperationException("Could not find an element named Controls/Window - did you forget to load a Gum project?");
#endif
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new Window(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(Window)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Controls/Window", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public NineSliceRuntime Background { get; protected set; }
    public Panel InnerPanelInstance { get; protected set; }
    public Panel TitleBarInstance { get; protected set; }
    public Panel BorderTopLeftInstance { get; protected set; }
    public Panel BorderTopRightInstance { get; protected set; }
    public Panel BorderBottomLeftInstance { get; protected set; }
    public Panel BorderBottomRightInstance { get; protected set; }
    public Panel BorderTopInstance { get; protected set; }
    public Panel BorderBottomInstance { get; protected set; }
    public Panel BorderLeftInstance { get; protected set; }
    public Panel BorderRightInstance { get; protected set; }

    public Window(InteractiveGue visual) : base(visual)
    {
    }
    public Window()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        Background = this.Visual?.GetGraphicalUiElementByName("Background") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        InnerPanelInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<Panel>(this.Visual,"InnerPanelInstance");
        TitleBarInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<Panel>(this.Visual,"TitleBarInstance");
        BorderTopLeftInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<Panel>(this.Visual,"BorderTopLeftInstance");
        BorderTopRightInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<Panel>(this.Visual,"BorderTopRightInstance");
        BorderBottomLeftInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<Panel>(this.Visual,"BorderBottomLeftInstance");
        BorderBottomRightInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<Panel>(this.Visual,"BorderBottomRightInstance");
        BorderTopInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<Panel>(this.Visual,"BorderTopInstance");
        BorderBottomInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<Panel>(this.Visual,"BorderBottomInstance");
        BorderLeftInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<Panel>(this.Visual,"BorderLeftInstance");
        BorderRightInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<Panel>(this.Visual,"BorderRightInstance");
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    public void ApplyLocalization()
    {
    }
    partial void CustomInitialize();
}
