//Code for Controls/MenuItem (Container)
using GumRuntime;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
public partial class MenuItemRuntime
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/MenuItem", typeof(MenuItemRuntime));
    }
    public MonoGameGum.Forms.Controls.MenuItem FormsControl => FormsControlAsObject as MonoGameGum.Forms.Controls.MenuItem;
    public enum MenuItemCategory
    {
        Enabled,
        Highlighted,
        Selected,
        Focused,
    }

    public MenuItemCategory MenuItemCategoryState
    {
        set
        {
            if(Categories.ContainsKey("MenuItemCategory"))
            {
                var category = Categories["MenuItemCategory"];
                var state = category.States.Find(item => item.Name == value.ToString());
                this.ApplyState(state);
            }
            else
            {
                var category = ((Gum.DataTypes.ElementSave)this.Tag).Categories.FirstOrDefault(item => item.Name == "MenuItemCategory");
                var state = category.States.Find(item => item.Name == value.ToString());
                this.ApplyState(state);
            }
        }
    }
    public NineSliceRuntime Background { get; protected set; }
    public TextRuntime TextInstance { get; protected set; }

    public string Header
    {
        get => TextInstance.Text;
        set => TextInstance.Text = value;
    }

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
            FormsControlAsObject = new MonoGameGum.Forms.Controls.MenuItem(this);
        }
        Background = this.GetGraphicalUiElementByName("Background") as NineSliceRuntime;
        TextInstance = this.GetGraphicalUiElementByName("TextInstance") as TextRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
