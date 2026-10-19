//Code for MainMenu
using GameUiSamples.Components;
using Gum;
using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary.Graphics;
using System.Linq;
namespace GameUiSamples.Screens;
partial class MainMenu : global::Gum.Forms.Controls.FrameworkElement
{
    #if UNITY_5_3_OR_NEWER
    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    #else
    [System.Runtime.CompilerServices.ModuleInitializer]
    #endif
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::Gum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("MainMenu") ?? throw new System.InvalidOperationException("Could not find an element named MainMenu - did you forget to load a Gum project?");
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
    public ListBoxItem HotbarHyTale { get; protected set; }
    public ListBoxItem InventoryStardew { get; protected set; }
    public ListBoxItem InventoryHyTale { get; protected set; }
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
        ListBoxInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<ListBox>(this.Visual,"ListBoxInstance");
        ButtonConfirmInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<ButtonConfirm>(this.Visual,"ButtonConfirmInstance");
        GameTitleScreenItem = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<ListBoxItem>(this.Visual,"GameTitleScreenItem");
        GameHudHollowKnight = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<ListBoxItem>(this.Visual,"GameHudHollowKnight");
        HotbarStardew = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<ListBoxItem>(this.Visual,"HotbarStardew");
        HotbarHyTale = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<ListBoxItem>(this.Visual,"HotbarHyTale");
        InventoryStardew = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<ListBoxItem>(this.Visual,"InventoryStardew");
        InventoryHyTale = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<ListBoxItem>(this.Visual,"InventoryHyTale");
        FrbClicker = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<ListBoxItem>(this.Visual,"FrbClicker");
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
