//Code for Controls/ScrollViewer (Container)
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
partial class ScrollViewer : MonoGameGum.Forms.Controls.ScrollViewer
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new MonoGameGum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Controls/ScrollViewer");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new ScrollViewer(visual);
            return visual;
        });
        MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(ScrollViewer)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Controls/ScrollViewer", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public enum ScrollBarVisibility
    {
        NoScrollBar,
        VerticalScrollVisible,
    }

    ScrollBarVisibility? _scrollBarVisibilityState;
    public ScrollBarVisibility? ScrollBarVisibilityState
    {
        get => _scrollBarVisibilityState;
        set
        {
            _scrollBarVisibilityState = value;
            if(value != null)
            {
                if(Visual.Categories.ContainsKey("ScrollBarVisibility"))
                {
                    var category = Visual.Categories["ScrollBarVisibility"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
                else
                {
                    var category = ((Gum.DataTypes.ElementSave)this.Visual.Tag).Categories.FirstOrDefault(item => item.Name == "ScrollBarVisibility");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
            }
        }
    }
    public NineSliceRuntime Background { get; protected set; }
    public ScrollBar VerticalScrollBarInstance { get; protected set; }
    public ContainerRuntime ClipContainerInstance { get; protected set; }
    public ContainerRuntime InnerPanelInstance { get; protected set; }
    public ListBoxItem ListBoxItemInstance { get; protected set; }

    public ScrollViewer(InteractiveGue visual) : base(visual) { }
    public ScrollViewer()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        Background = this.Visual?.GetGraphicalUiElementByName("Background") as NineSliceRuntime;
        VerticalScrollBarInstance = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ScrollBar>(this.Visual,"VerticalScrollBarInstance");
        ClipContainerInstance = this.Visual?.GetGraphicalUiElementByName("ClipContainerInstance") as ContainerRuntime;
        InnerPanelInstance = this.Visual?.GetGraphicalUiElementByName("InnerPanelInstance") as ContainerRuntime;
        ListBoxItemInstance = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ListBoxItem>(this.Visual,"ListBoxItemInstance");
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
