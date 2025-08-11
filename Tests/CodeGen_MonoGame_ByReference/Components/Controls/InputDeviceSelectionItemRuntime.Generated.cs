//Code for Controls/InputDeviceSelectionItem (Container)
using GumRuntime;
using System.Linq;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using CodeGen_MonoGame_ByReference.Components.Elements;
using CodeGen_MonoGame_ByReference.Components.Controls;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

namespace CodeGen_MonoGame_ByReference.Components.Controls;
partial class InputDeviceSelectionItemRuntime : ContainerRuntime
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/InputDeviceSelectionItem", typeof(InputDeviceSelectionItemRuntime));
    }
    public enum JoinedCategory
    {
        NoInputDevice,
        HasInputDevice,
    }

    JoinedCategory? _joinedCategoryState;
    public JoinedCategory? JoinedCategoryState
    {
        get => _joinedCategoryState;
        set
        {
            _joinedCategoryState = value;
            if(value != null)
            {
                if(Categories.ContainsKey("JoinedCategory"))
                {
                    var category = Categories["JoinedCategory"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.ApplyState(state);
                }
                else
                {
                    var category = ((global::Gum.DataTypes.ElementSave)this.Tag).Categories.FirstOrDefault(item => item.Name == "JoinedCategory");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.ApplyState(state);
                }
            }
        }
    }
    public NineSliceRuntime Background { get; protected set; }
    public IconRuntime IconInstance { get; protected set; }
    public TextRuntime TextInstance { get; protected set; }
    public ButtonCloseRuntime RemoveDeviceButtonInstance { get; protected set; }

    public InputDeviceSelectionItemRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
    {
        if(fullInstantiation)
        {
            var element = ObjectFinder.Self.GetElementSave("Controls/InputDeviceSelectionItem");
            element?.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
        }



    }
    public override void AfterFullCreation()
    {
        Background = this.GetGraphicalUiElementByName("Background") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        IconInstance = this.GetGraphicalUiElementByName("IconInstance") as CodeGen_MonoGame_ByReference.Components.Elements.IconRuntime;
        TextInstance = this.GetGraphicalUiElementByName("TextInstance") as global::MonoGameGum.GueDeriving.TextRuntime;
        RemoveDeviceButtonInstance = this.GetGraphicalUiElementByName("RemoveDeviceButtonInstance") as CodeGen_MonoGame_ByReference.Components.Controls.ButtonCloseRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
