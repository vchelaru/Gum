//Code for StardewInventoryScreen
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
partial class StardewInventoryScreen : MonoGameGum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new MonoGameGum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("StardewInventoryScreen");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new StardewInventoryScreen(visual);
            visual.Width = 0;
            visual.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
            visual.Height = 0;
            visual.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
            return visual;
        });
        MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(StardewInventoryScreen)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("StardewInventoryScreen", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public InventoryGrid InventoryGridInstance { get; protected set; }

    public StardewInventoryScreen(InteractiveGue visual) : base(visual) { }
    public StardewInventoryScreen()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        InventoryGridInstance = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<InventoryGrid>(this.Visual,"InventoryGridInstance");
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
