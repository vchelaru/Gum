//Code for Controls/ItemsControl (Container)
using CodeGen_MonoGameForms_Localization_ByReference.Components.Controls;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.StateAnimation.Runtime;
using Gum.Wireframe;
using GumRuntime;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using System.Linq;
namespace CodeGen_MonoGameForms_Localization_ByReference.Components.Controls;
partial class ItemsControl : global::Gum.Forms.Controls.ItemsControl
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Controls/ItemsControl");
#if DEBUG
if(element == null) throw new System.InvalidOperationException("Could not find an element named Controls/ItemsControl - did you forget to load a Gum project?");
#endif
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new ItemsControl(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(ItemsControl)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Controls/ItemsControl", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public enum ItemsControlCategory
    {
        Enabled,
        Disabled,
        Focused,
        DisabledFocused,
    }

    ItemsControlCategory? _itemsControlCategoryState;
    public ItemsControlCategory? ItemsControlCategoryState
    {
        get => _itemsControlCategoryState;
        set
        {
            _itemsControlCategoryState = value;
            if(value != null)
            {
                if(Visual.Categories.ContainsKey("ItemsControlCategory"))
                {
                    var category = Visual.Categories["ItemsControlCategory"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
                else
                {
                    var category = ((global::Gum.DataTypes.ElementSave)this.Visual.Tag).Categories.FirstOrDefault(item => item.Name == "ItemsControlCategory");
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
    public ContainerRuntime ClipAndScrollContainer { get; protected set; }
    public ContainerRuntime ClipContainerParent { get; protected set; }

    public ItemsControl(InteractiveGue visual) : base(visual)
    {
    }
    public ItemsControl()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        Background = this.Visual?.GetGraphicalUiElementByName("Background") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        VerticalScrollBarInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ScrollBar>(this.Visual,"VerticalScrollBarInstance");
        ClipContainerInstance = this.Visual?.GetGraphicalUiElementByName("ClipContainerInstance") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        InnerPanelInstance = this.Visual?.GetGraphicalUiElementByName("InnerPanelInstance") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        FocusedIndicator = this.Visual?.GetGraphicalUiElementByName("FocusedIndicator") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        ClipAndScrollContainer = this.Visual?.GetGraphicalUiElementByName("ClipAndScrollContainer") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        ClipContainerParent = this.Visual?.GetGraphicalUiElementByName("ClipContainerParent") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    public void ApplyLocalization()
    {
    }
    partial void CustomInitialize();
}
