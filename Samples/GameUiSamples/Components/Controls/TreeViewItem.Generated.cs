//Code for Controls/TreeViewItem (Container)
using GumRuntime;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using GameUiSamples.Components;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
namespace GameUiSamples.Components;
partial class TreeViewItem : MonoGameGum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new MonoGameGum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Controls/TreeViewItem");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new TreeViewItem(visual);
            return visual;
        });
        MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(TreeViewItem)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Controls/TreeViewItem", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public TreeViewToggle ToggleButtonInstance { get; protected set; }
    public ListBoxItem ListBoxItemInstance { get; protected set; }
    public ContainerRuntime InnerPanelInstance { get; protected set; }

    public TreeViewItem(InteractiveGue visual) : base(visual) { }
    public TreeViewItem()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        ToggleButtonInstance = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<TreeViewToggle>(this.Visual,"ToggleButtonInstance");
        ListBoxItemInstance = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ListBoxItem>(this.Visual,"ListBoxItemInstance");
        InnerPanelInstance = this.Visual?.GetGraphicalUiElementByName("InnerPanelInstance") as ContainerRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
