//Code for Controls/InputDeviceSelector (Container)
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
partial class InputDeviceSelector : MonoGameGum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::MonoGameGum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Controls/InputDeviceSelector");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new InputDeviceSelector(visual);
            return visual;
        });
        global::MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(InputDeviceSelector)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Controls/InputDeviceSelector", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public NineSliceRuntime Background { get; protected set; }
    public TextRuntime TextInstance { get; protected set; }
    public TextRuntime TextInstance1 { get; protected set; }
    public ContainerRuntime ContainerInstance1 { get; protected set; }
    public ContainerRuntime InputDeviceContainerInstance { get; protected set; }
    public ContainerRuntime ContainerInstance2 { get; protected set; }
    public InputDeviceSelectionItem InputDeviceSelectionItemInstance { get; protected set; }
    public InputDeviceSelectionItem InputDeviceSelectionItemInstance1 { get; protected set; }
    public InputDeviceSelectionItem InputDeviceSelectionItemInstance2 { get; protected set; }
    public InputDeviceSelectionItem InputDeviceSelectionItemInstance3 { get; protected set; }

    public InputDeviceSelector(InteractiveGue visual) : base(visual)
    {
    }
    public InputDeviceSelector()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        Background = this.Visual?.GetGraphicalUiElementByName("Background") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        TextInstance = this.Visual?.GetGraphicalUiElementByName("TextInstance") as global::MonoGameGum.GueDeriving.TextRuntime;
        TextInstance1 = this.Visual?.GetGraphicalUiElementByName("TextInstance1") as global::MonoGameGum.GueDeriving.TextRuntime;
        ContainerInstance1 = this.Visual?.GetGraphicalUiElementByName("ContainerInstance1") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        InputDeviceContainerInstance = this.Visual?.GetGraphicalUiElementByName("InputDeviceContainerInstance") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        ContainerInstance2 = this.Visual?.GetGraphicalUiElementByName("ContainerInstance2") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        InputDeviceSelectionItemInstance = global::MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<InputDeviceSelectionItem>(this.Visual,"InputDeviceSelectionItemInstance");
        InputDeviceSelectionItemInstance1 = global::MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<InputDeviceSelectionItem>(this.Visual,"InputDeviceSelectionItemInstance1");
        InputDeviceSelectionItemInstance2 = global::MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<InputDeviceSelectionItem>(this.Visual,"InputDeviceSelectionItemInstance2");
        InputDeviceSelectionItemInstance3 = global::MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<InputDeviceSelectionItem>(this.Visual,"InputDeviceSelectionItemInstance3");
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
