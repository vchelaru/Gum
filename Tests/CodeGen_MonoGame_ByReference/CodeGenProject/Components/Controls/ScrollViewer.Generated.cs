//Code for Controls/ScrollViewer (Container)
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
partial class ScrollViewer : global::MonoGameGum.Forms.Controls.ScrollViewer
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::MonoGameGum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Controls/ScrollViewer");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new ScrollViewer(visual);
            return visual;
        });
        global::MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(ScrollViewer)] = template;
        global::MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(global::MonoGameGum.Forms.Controls.ScrollViewer)] = template;
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
    public enum ScrollViewerCategory
    {
        Enabled,
        Focused,
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
                    var category = ((global::Gum.DataTypes.ElementSave)this.Visual.Tag).Categories.FirstOrDefault(item => item.Name == "ScrollBarVisibility");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
            }
        }
    }

    ScrollViewerCategory? _scrollViewerCategoryState;
    public ScrollViewerCategory? ScrollViewerCategoryState
    {
        get => _scrollViewerCategoryState;
        set
        {
            _scrollViewerCategoryState = value;
            if(value != null)
            {
                if(Visual.Categories.ContainsKey("ScrollViewerCategory"))
                {
                    var category = Visual.Categories["ScrollViewerCategory"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
                else
                {
                    var category = ((global::Gum.DataTypes.ElementSave)this.Visual.Tag).Categories.FirstOrDefault(item => item.Name == "ScrollViewerCategory");
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
    public NineSliceRuntime FocusedIndicator { get; protected set; }

    public ScrollViewer(InteractiveGue visual) : base(visual)
    {
    }
    public ScrollViewer()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        Background = this.Visual?.GetGraphicalUiElementByName("Background") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        VerticalScrollBarInstance = global::MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ScrollBar>(this.Visual,"VerticalScrollBarInstance");
        ClipContainerInstance = this.Visual?.GetGraphicalUiElementByName("ClipContainerInstance") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        InnerPanelInstance = this.Visual?.GetGraphicalUiElementByName("InnerPanelInstance") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        FocusedIndicator = this.Visual?.GetGraphicalUiElementByName("FocusedIndicator") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
