//Code for Controls/ListBox (Container)
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
partial class ListBoxRuntime : ContainerRuntime
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/ListBox", typeof(ListBoxRuntime));
        global::MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsComponents[typeof(global::MonoGameGum.Forms.Controls.ListBox)] = typeof(ListBoxRuntime);
    }
    public global::MonoGameGum.Forms.Controls.ListBox FormsControl => FormsControlAsObject as global::MonoGameGum.Forms.Controls.ListBox;
    public enum ListBoxCategory
    {
        Enabled,
        Disabled,
        Focused,
        DisabledFocused,
    }

    ListBoxCategory? _listBoxCategoryState;
    public ListBoxCategory? ListBoxCategoryState
    {
        get => _listBoxCategoryState;
        set
        {
            _listBoxCategoryState = value;
            if(value != null)
            {
                if(Categories.ContainsKey("ListBoxCategory"))
                {
                    var category = Categories["ListBoxCategory"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.ApplyState(state);
                }
                else
                {
                    var category = ((global::Gum.DataTypes.ElementSave)this.Tag).Categories.FirstOrDefault(item => item.Name == "ListBoxCategory");
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
    public ContainerRuntime ClipAndScrollContainer { get; protected set; }
    public ContainerRuntime ClipContainerParent { get; protected set; }

    public ListBoxRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
    {
        if(fullInstantiation)
        {
            var element = ObjectFinder.Self.GetElementSave("Controls/ListBox");
            element?.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
        }



    }
    public override void AfterFullCreation()
    {
        if (FormsControl == null)
        {
            FormsControlAsObject = new global::MonoGameGum.Forms.Controls.ListBox(this);
        }
        Background = this.GetGraphicalUiElementByName("Background") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        VerticalScrollBarInstance = this.GetGraphicalUiElementByName("VerticalScrollBarInstance") as CodeGen_MonoGame_ByReference.Components.Controls.ScrollBarRuntime;
        ClipContainerInstance = this.GetGraphicalUiElementByName("ClipContainerInstance") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        InnerPanelInstance = this.GetGraphicalUiElementByName("InnerPanelInstance") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        FocusedIndicator = this.GetGraphicalUiElementByName("FocusedIndicator") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        ClipAndScrollContainer = this.GetGraphicalUiElementByName("ClipAndScrollContainer") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        ClipContainerParent = this.GetGraphicalUiElementByName("ClipContainerParent") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
