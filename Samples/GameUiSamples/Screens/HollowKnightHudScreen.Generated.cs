//Code for HollowKnightHudScreen
using GumRuntime;
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
partial class HollowKnightHudScreen : MonoGameGum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new MonoGameGum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("HollowKnightHudScreen");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new HollowKnightHudScreen(visual);
            visual.Width = 0;
            visual.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
            visual.Height = 0;
            visual.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
            return visual;
        });
        MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(HollowKnightHudScreen)] = template;
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

    public HollowKnightHudScreen(InteractiveGue visual) : base(visual) { }
    public HollowKnightHudScreen()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        ColoredRectangleInstance = this.Visual?.GetGraphicalUiElementByName("ColoredRectangleInstance") as ColoredRectangleRuntime;
        MainHudContainer = this.Visual?.GetGraphicalUiElementByName("MainHudContainer") as ContainerRuntime;
        ManaOrbInstance = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ManaOrb>(this.Visual,"ManaOrbInstance");
        HealthContainer = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<StackPanel>(this.Visual,"HealthContainer");
        HealthItemInstance = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HealthItem>(this.Visual,"HealthItemInstance");
        HealthItemInstance1 = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HealthItem>(this.Visual,"HealthItemInstance1");
        HealthItemInstance2 = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HealthItem>(this.Visual,"HealthItemInstance2");
        HealthItemInstance3 = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HealthItem>(this.Visual,"HealthItemInstance3");
        HealthItemInstance4 = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HealthItem>(this.Visual,"HealthItemInstance4");
        HealthItemInstance5 = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<HealthItem>(this.Visual,"HealthItemInstance5");
        CurrencyInstance = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<Currency>(this.Visual,"CurrencyInstance");
        ActionButtonContainer = this.Visual?.GetGraphicalUiElementByName("ActionButtonContainer") as ContainerRuntime;
        AddManaButton = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ButtonStandard>(this.Visual,"AddManaButton");
        SubtractManaButton = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ButtonStandard>(this.Visual,"SubtractManaButton");
        TakeDamageButton = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ButtonStandard>(this.Visual,"TakeDamageButton");
        RefillHealthButton = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ButtonStandard>(this.Visual,"RefillHealthButton");
        AddMoneyButton = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ButtonStandard>(this.Visual,"AddMoneyButton");
        ExitButton = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ButtonStandard>(this.Visual,"ExitButton");
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
