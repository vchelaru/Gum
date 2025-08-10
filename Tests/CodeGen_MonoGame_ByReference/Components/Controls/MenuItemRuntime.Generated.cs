//Code for Controls/MenuItem (Container)
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

namespace CodeGen_MonoGame_ByReference.Components.Controls;
partial class MenuItemRuntime : ContainerRuntime
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/MenuItem", typeof(MenuItemRuntime));
        global::MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsComponents[typeof(global::MonoGameGum.Forms.Controls.MenuItem)] = typeof(MenuItemRuntime);
    }
    public global::MonoGameGum.Forms.Controls.MenuItem FormsControl => FormsControlAsObject as global::MonoGameGum.Forms.Controls.MenuItem;
    public enum MenuItemCategory
    {
        Enabled,
        Highlighted,
        Selected,
        Focused,
        Disabled,
    }

    MenuItemCategory? _menuItemCategoryState;
    public MenuItemCategory? MenuItemCategoryState
    {
        get => _menuItemCategoryState;
        set
        {
            _menuItemCategoryState = value;
            if(value != null)
            {
                if(Categories.ContainsKey("MenuItemCategory"))
                {
                    var category = Categories["MenuItemCategory"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.ApplyState(state);
                }
                else
                {
                    var category = ((global::Gum.DataTypes.ElementSave)this.Tag).Categories.FirstOrDefault(item => item.Name == "MenuItemCategory");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.ApplyState(state);
                }
            }
        }
    }
    public NineSliceRuntime Background { get; protected set; }
    public TextRuntime TextInstance { get; protected set; }
    public TextRuntime SubmenuIndicatorInstance { get; protected set; }
    public ContainerRuntime ContainerInstance { get; protected set; }

    public MenuItemRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
    {
        if(fullInstantiation)
        {
            var element = ObjectFinder.Self.GetElementSave("Controls/MenuItem");
            element?.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
        }



    }
    public override void AfterFullCreation()
    {
        if (FormsControl == null)
        {
            FormsControlAsObject = new global::MonoGameGum.Forms.Controls.MenuItem(this);
        }
        Background = this.GetGraphicalUiElementByName("Background") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        TextInstance = this.GetGraphicalUiElementByName("TextInstance") as global::MonoGameGum.GueDeriving.TextRuntime;
        SubmenuIndicatorInstance = this.GetGraphicalUiElementByName("SubmenuIndicatorInstance") as global::MonoGameGum.GueDeriving.TextRuntime;
        ContainerInstance = this.GetGraphicalUiElementByName("ContainerInstance") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
