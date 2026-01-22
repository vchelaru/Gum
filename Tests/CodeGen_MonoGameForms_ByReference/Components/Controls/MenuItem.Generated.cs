//Code for Controls/MenuItem (Container)
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using System.Linq;
namespace CodeGenProject.Components.Controls;
partial class MenuItem : global::Gum.Forms.Controls.MenuItem
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Controls/MenuItem");
#if DEBUG
if(element == null) throw new System.InvalidOperationException("Could not find an element named Controls/MenuItem - did you forget to load a Gum project?");
#endif
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new MenuItem(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(MenuItem)] = template;
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(global::Gum.Forms.Controls.MenuItem)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Controls/MenuItem", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
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
                if(Visual.Categories.ContainsKey("MenuItemCategory"))
                {
                    var category = Visual.Categories["MenuItemCategory"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
                else
                {
                    var category = ((global::Gum.DataTypes.ElementSave)this.Visual.Tag).Categories.FirstOrDefault(item => item.Name == "MenuItemCategory");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
            }
        }
    }
    public NineSliceRuntime Background { get; protected set; }
    public TextRuntime TextInstance { get; protected set; }
    public TextRuntime SubmenuIndicatorInstance { get; protected set; }
    public ContainerRuntime ContainerInstance { get; protected set; }

    public MenuItem(InteractiveGue visual) : base(visual)
    {
    }
    public MenuItem()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        Background = this.Visual?.GetGraphicalUiElementByName("Background") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        TextInstance = this.Visual?.GetGraphicalUiElementByName("TextInstance") as global::MonoGameGum.GueDeriving.TextRuntime;
        SubmenuIndicatorInstance = this.Visual?.GetGraphicalUiElementByName("SubmenuIndicatorInstance") as global::MonoGameGum.GueDeriving.TextRuntime;
        ContainerInstance = this.Visual?.GetGraphicalUiElementByName("ContainerInstance") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
