//Code for Controls/ScrollViewer (Container)
using GumRuntime;
using System.Linq;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using CodeGen_MonoGame_ByReference.Components.Controls;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

namespace CodeGen_MonoGame_ByReference.Components.Controls;
partial class ScrollViewerRuntime : ContainerRuntime
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/ScrollViewer", typeof(ScrollViewerRuntime));
        global::MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsComponents[typeof(global::MonoGameGum.Forms.Controls.ScrollViewer)] = typeof(ScrollViewerRuntime);
    }
    public global::MonoGameGum.Forms.Controls.ScrollViewer FormsControl => FormsControlAsObject as global::MonoGameGum.Forms.Controls.ScrollViewer;
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
                if(Categories.ContainsKey("ScrollBarVisibility"))
                {
                    var category = Categories["ScrollBarVisibility"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.ApplyState(state);
                }
                else
                {
                    var category = ((global::Gum.DataTypes.ElementSave)this.Tag).Categories.FirstOrDefault(item => item.Name == "ScrollBarVisibility");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.ApplyState(state);
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
                if(Categories.ContainsKey("ScrollViewerCategory"))
                {
                    var category = Categories["ScrollViewerCategory"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.ApplyState(state);
                }
                else
                {
                    var category = ((global::Gum.DataTypes.ElementSave)this.Tag).Categories.FirstOrDefault(item => item.Name == "ScrollViewerCategory");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.ApplyState(state);
                }
            }
        }
    }
    public NineSliceRuntime Background { get; protected set; }
    public ScrollBarRuntime VerticalScrollBarInstance { get; protected set; }
    public ContainerRuntime ClipContainerInstance { get; protected set; }
    public ContainerRuntime InnerPanelInstance { get; protected set; }
    public NineSliceRuntime FocusedIndicator { get; protected set; }

    public ScrollViewerRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
    {
        if(fullInstantiation)
        {
            var element = ObjectFinder.Self.GetElementSave("Controls/ScrollViewer");
            element?.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
        }



    }
    public override void AfterFullCreation()
    {
        if (FormsControl == null)
        {
            FormsControlAsObject = new global::MonoGameGum.Forms.Controls.ScrollViewer(this);
        }
        Background = this.GetGraphicalUiElementByName("Background") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        VerticalScrollBarInstance = this.GetGraphicalUiElementByName("VerticalScrollBarInstance") as ScrollBarRuntime;
        ClipContainerInstance = this.GetGraphicalUiElementByName("ClipContainerInstance") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        InnerPanelInstance = this.GetGraphicalUiElementByName("InnerPanelInstance") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        FocusedIndicator = this.GetGraphicalUiElementByName("FocusedIndicator") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
