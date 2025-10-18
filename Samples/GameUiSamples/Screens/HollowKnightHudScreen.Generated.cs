//Code for HollowKnightHudScreen
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
partial class HollowKnightHudScreen : global::Gum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("HollowKnightHudScreen");
#if DEBUG
if(element == null) throw new System.InvalidOperationException("Could not find an element named HollowKnightHudScreen - did you forget to load a Gum project?");
#endif
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new HollowKnightHudScreen(visual);
            visual.Width = 0;
            visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
            visual.Height = 0;
            visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(HollowKnightHudScreen)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("HollowKnightHudScreen", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public ColoredRectangleRuntime ColoredRectangleInstance { get; protected set; }
    public ContainerRuntime MainHudContainer { get; protected set; }
    public ManaOrb ManaOrbInstance { get; protected set; }
    public StackPanel HealthContainer { get; protected set; }
    public HealthItem HealthItemInstance { get; protected set; }
    public HealthItem HealthItemInstance1 { get; protected set; }
    public HealthItem HealthItemInstance2 { get; protected set; }
    public HealthItem HealthItemInstance3 { get; protected set; }
    public HealthItem HealthItemInstance4 { get; protected set; }
    public HealthItem HealthItemInstance5 { get; protected set; }
    public Currency CurrencyInstance { get; protected set; }
    public ContainerRuntime ActionButtonContainer { get; protected set; }
    public ButtonStandard AddManaButton { get; protected set; }
    public ButtonStandard SubtractManaButton { get; protected set; }
    public ButtonStandard TakeDamageButton { get; protected set; }
    public ButtonStandard RefillHealthButton { get; protected set; }
    public ButtonStandard AddMoneyButton { get; protected set; }
    public ButtonStandard ExitButton { get; protected set; }

    public HollowKnightHudScreen(InteractiveGue visual) : base(visual)
    {
    }
    public HollowKnightHudScreen()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        ColoredRectangleInstance = this.Visual?.GetGraphicalUiElementByName("ColoredRectangleInstance") as global::MonoGameGum.GueDeriving.ColoredRectangleRuntime;
        MainHudContainer = this.Visual?.GetGraphicalUiElementByName("MainHudContainer") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        ManaOrbInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ManaOrb>(this.Visual,"ManaOrbInstance");
        HealthContainer = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<StackPanel>(this.Visual,"HealthContainer");
        HealthItemInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HealthItem>(this.Visual,"HealthItemInstance");
        HealthItemInstance1 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HealthItem>(this.Visual,"HealthItemInstance1");
        HealthItemInstance2 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HealthItem>(this.Visual,"HealthItemInstance2");
        HealthItemInstance3 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HealthItem>(this.Visual,"HealthItemInstance3");
        HealthItemInstance4 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HealthItem>(this.Visual,"HealthItemInstance4");
        HealthItemInstance5 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HealthItem>(this.Visual,"HealthItemInstance5");
        CurrencyInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<Currency>(this.Visual,"CurrencyInstance");
        ActionButtonContainer = this.Visual?.GetGraphicalUiElementByName("ActionButtonContainer") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        AddManaButton = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ButtonStandard>(this.Visual,"AddManaButton");
        SubtractManaButton = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ButtonStandard>(this.Visual,"SubtractManaButton");
        TakeDamageButton = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ButtonStandard>(this.Visual,"TakeDamageButton");
        RefillHealthButton = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ButtonStandard>(this.Visual,"RefillHealthButton");
        AddMoneyButton = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ButtonStandard>(this.Visual,"AddMoneyButton");
        ExitButton = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ButtonStandard>(this.Visual,"ExitButton");
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
