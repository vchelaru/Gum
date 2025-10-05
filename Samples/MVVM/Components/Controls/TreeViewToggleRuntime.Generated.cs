//Code for Controls/TreeViewToggle (Container)
using GumRuntime;
using System.Linq;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
partial class TreeViewToggleRuntime : ContainerRuntime
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/TreeViewToggle", typeof(TreeViewToggleRuntime));
    }
    public enum ToggleCategory
    {
        EnabledOn,
        EnabledOff,
        DisabledOn,
        DisabledOff,
        HighlightedOn,
        HighlightedOff,
        PushedOn,
        PushedOff,
    }

    ToggleCategory? _toggleCategoryState;
    public ToggleCategory? ToggleCategoryState
    {
        get => _toggleCategoryState;
        set
        {
            _toggleCategoryState = value;
            if(value != null)
            {
                if(Categories.ContainsKey("ToggleCategory"))
                {
                    var category = Categories["ToggleCategory"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.ApplyState(state);
                }
                else
                {
                    var category = ((global::Gum.DataTypes.ElementSave)this.Tag).Categories.FirstOrDefault(item => item.Name == "ToggleCategory");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.ApplyState(state);
                }
            }
        }
    }
    public NineSliceRuntime NineSliceInstance { get; protected set; }
    public IconRuntime IconInstance { get; protected set; }

    public TreeViewToggleRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
    {
        if(fullInstantiation)
        {
            var element = ObjectFinder.Self.GetElementSave("Controls/TreeViewToggle");
            element?.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
        }



    }
    public override void AfterFullCreation()
    {
        NineSliceInstance = this.GetGraphicalUiElementByName("NineSliceInstance") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        IconInstance = this.GetGraphicalUiElementByName("IconInstance") as IconRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
