//Code for MainMenu
using GumRuntime;
using System.Linq;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using GameUiSamples.Components;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
namespace GameUiSamples.Screens;
partial class MainMenu : global::Gum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("MainMenu");
#if DEBUG
if(element == null) throw new System.InvalidOperationException("Could not find an element named MainMenu - did you forget to load a Gum project?");
#endif
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new MainMenu(visual);
            visual.Width = 0;
            visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
            visual.Height = 0;
            visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(MainMenu)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("MainMenu", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public ListBox ListBoxInstance { get; protected set; }
    public ButtonConfirm ButtonConfirmInstance { get; protected set; }
    public ListBoxItem GameTitleScreenItem { get; protected set; }
    public ListBoxItem GameHudHollowKnight { get; protected set; }
    public ListBoxItem HotbarStardew { get; protected set; }
    public ListBoxItem InventoryStardew { get; protected set; }
    public ListBoxItem FrbClicker { get; protected set; }

    public MainMenu(InteractiveGue visual) : base(visual)
    {
    }
    public MainMenu()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        ListBoxInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ListBox>(this.Visual,"ListBoxInstance");
        ButtonConfirmInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ButtonConfirm>(this.Visual,"ButtonConfirmInstance");
        GameTitleScreenItem = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ListBoxItem>(this.Visual,"GameTitleScreenItem");
        GameHudHollowKnight = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ListBoxItem>(this.Visual,"GameHudHollowKnight");
        HotbarStardew = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ListBoxItem>(this.Visual,"HotbarStardew");
        InventoryStardew = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ListBoxItem>(this.Visual,"InventoryStardew");
        FrbClicker = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ListBoxItem>(this.Visual,"FrbClicker");
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
