//Code for Controls/ScrollBar (Container)
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
partial class ScrollBarRuntime : ContainerRuntime
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/ScrollBar", typeof(ScrollBarRuntime));
        global::MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsComponents[typeof(global::MonoGameGum.Forms.Controls.ScrollBar)] = typeof(ScrollBarRuntime);
    }
    public global::MonoGameGum.Forms.Controls.ScrollBar FormsControl => FormsControlAsObject as global::MonoGameGum.Forms.Controls.ScrollBar;
    public enum ScrollBarCategory
    {
    }

    ScrollBarCategory? _scrollBarCategoryState;
    public ScrollBarCategory? ScrollBarCategoryState
    {
        get => _scrollBarCategoryState;
        set
        {
            _scrollBarCategoryState = value;
            if(value != null)
            {
                if(Categories.ContainsKey("ScrollBarCategory"))
                {
                    var category = Categories["ScrollBarCategory"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.ApplyState(state);
                }
                else
                {
                    var category = ((global::Gum.DataTypes.ElementSave)this.Tag).Categories.FirstOrDefault(item => item.Name == "ScrollBarCategory");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.ApplyState(state);
                }
            }
        }
    }
    public ButtonIconRuntime UpButtonInstance { get; protected set; }
    public ButtonIconRuntime DownButtonInstance { get; protected set; }
    public ContainerRuntime TrackInstance { get; protected set; }
    public NineSliceRuntime TrackBackground { get; protected set; }
    public ButtonStandardRuntime ThumbInstance { get; protected set; }

    public ScrollBarRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
    {
        if(fullInstantiation)
        {
            var element = ObjectFinder.Self.GetElementSave("Controls/ScrollBar");
            element?.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
        }



    }
    public override void AfterFullCreation()
    {
        if (FormsControl == null)
        {
            FormsControlAsObject = new global::MonoGameGum.Forms.Controls.ScrollBar(this);
        }
        UpButtonInstance = this.GetGraphicalUiElementByName("UpButtonInstance") as global::MonoGameGum.GueDeriving.ButtonIconRuntime;
        DownButtonInstance = this.GetGraphicalUiElementByName("DownButtonInstance") as global::MonoGameGum.GueDeriving.ButtonIconRuntime;
        TrackInstance = this.GetGraphicalUiElementByName("TrackInstance") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        TrackBackground = this.GetGraphicalUiElementByName("TrackBackground") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        ThumbInstance = this.GetGraphicalUiElementByName("ThumbInstance") as global::MonoGameGum.GueDeriving.ButtonStandardRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
