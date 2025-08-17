//Code for Controls/WindowStandard (Container)
using GumRuntime;
using System.Linq;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using CodeGenProject.Components.Controls;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

namespace CodeGenProject.Components.Controls;
partial class WindowStandard : global::MonoGameGum.Forms.Window
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::MonoGameGum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Controls/WindowStandard");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new WindowStandard(visual);
            return visual;
        });
        global::MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(WindowStandard)] = template;
        global::MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(global::MonoGameGum.Forms.Window)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Controls/WindowStandard", () => 
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

    public WindowStandard(InteractiveGue visual) : base(visual)
    {
    }
    public WindowStandard()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        Background = this.Visual?.GetGraphicalUiElementByName("Background") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        InnerPanelInstance = global::MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<Panel>(this.Visual,"InnerPanelInstance");
        TitleBarInstance = global::MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<Panel>(this.Visual,"TitleBarInstance");
        BorderTopLeftInstance = global::MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<Panel>(this.Visual,"BorderTopLeftInstance");
        BorderTopRightInstance = global::MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<Panel>(this.Visual,"BorderTopRightInstance");
        BorderBottomLeftInstance = global::MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<Panel>(this.Visual,"BorderBottomLeftInstance");
        BorderBottomRightInstance = global::MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<Panel>(this.Visual,"BorderBottomRightInstance");
        BorderTopInstance = global::MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<Panel>(this.Visual,"BorderTopInstance");
        BorderBottomInstance = global::MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<Panel>(this.Visual,"BorderBottomInstance");
        BorderLeftInstance = global::MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<Panel>(this.Visual,"BorderLeftInstance");
        BorderRightInstance = global::MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<Panel>(this.Visual,"BorderRightInstance");
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
