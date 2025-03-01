//Code for Controls/StoreListBoxItem (Container)
using GumRuntime;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
public partial class StoreListBoxItemRuntime:ContainerRuntime
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/StoreListBoxItem", typeof(StoreListBoxItemRuntime));
    }
    public MonoGameGum.Forms.Controls.ListBoxItem FormsControl => FormsControlAsObject as MonoGameGum.Forms.Controls.ListBoxItem;
    public enum ListBoxItemCategory
    {
        Enabled,
        Highlighted,
        Selected,
        Focused,
    }

    public ListBoxItemCategory ListBoxItemCategoryState
    {
        set
        {
            if(Categories.ContainsKey("ListBoxItemCategory"))
            {
                var category = Categories["ListBoxItemCategory"];
                var state = category.States.Find(item => item.Name == value.ToString());
                this.ApplyState(state);
            }
            else
            {
                var category = ((Gum.DataTypes.ElementSave)this.Tag).Categories.FirstOrDefault(item => item.Name == "ListBoxItemCategory");
                var state = category.States.Find(item => item.Name == value.ToString());
                this.ApplyState(state);
            }
        }
    }
    public NineSliceRuntime Background { get; protected set; }
    public TextRuntime ItemNameTextInstance { get; protected set; }
    public TextRuntime CostTextInstance { get; protected set; }
    public NineSliceRuntime FocusedIndicator { get; protected set; }

    public string ListItemDisplayText
    {
        get => ItemNameTextInstance.Text;
        set => ItemNameTextInstance.Text = value;
    }

    public StoreListBoxItemRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
    {
        if(fullInstantiation)
        {
            var element = ObjectFinder.Self.GetElementSave("Controls/StoreListBoxItem");
            element?.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
        }



    }
    public override void AfterFullCreation()
    {
        if (FormsControl == null)
        {
            FormsControlAsObject = new MonoGameGum.Forms.Controls.ListBoxItem(this);
        }
        Background = this.GetGraphicalUiElementByName("Background") as NineSliceRuntime;
        ItemNameTextInstance = this.GetGraphicalUiElementByName("ItemNameTextInstance") as TextRuntime;
        CostTextInstance = this.GetGraphicalUiElementByName("CostTextInstance") as TextRuntime;
        FocusedIndicator = this.GetGraphicalUiElementByName("FocusedIndicator") as NineSliceRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
