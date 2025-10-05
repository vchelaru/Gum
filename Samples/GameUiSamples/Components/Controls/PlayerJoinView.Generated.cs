//Code for Controls/PlayerJoinView (Container)
using GumRuntime;
using System.Linq;
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
partial class PlayerJoinView : global::Gum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Controls/PlayerJoinView");
#if DEBUG
if(element == null) throw new System.InvalidOperationException("Could not find an element named Controls/PlayerJoinView - did you forget to load a Gum project?");
#endif
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new PlayerJoinView(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(PlayerJoinView)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Controls/PlayerJoinView", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public ContainerRuntime InnerPanelInstance { get; protected set; }
    public PlayerJoinViewItem PlayerJoinViewItem1 { get; protected set; }
    public PlayerJoinViewItem PlayerJoinViewItem2 { get; protected set; }
    public PlayerJoinViewItem PlayerJoinViewItem3 { get; protected set; }
    public PlayerJoinViewItem PlayerJoinViewItem4 { get; protected set; }

    public PlayerJoinView(InteractiveGue visual) : base(visual)
    {
    }
    public PlayerJoinView()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        InnerPanelInstance = this.Visual?.GetGraphicalUiElementByName("InnerPanelInstance") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        PlayerJoinViewItem1 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<PlayerJoinViewItem>(this.Visual,"PlayerJoinViewItem1");
        PlayerJoinViewItem2 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<PlayerJoinViewItem>(this.Visual,"PlayerJoinViewItem2");
        PlayerJoinViewItem3 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<PlayerJoinViewItem>(this.Visual,"PlayerJoinViewItem3");
        PlayerJoinViewItem4 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<PlayerJoinViewItem>(this.Visual,"PlayerJoinViewItem4");
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
