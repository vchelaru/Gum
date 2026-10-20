//Code for StardewHotbarScreen
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
partial class StardewHotbarScreen : global::Gum.Forms.Controls.FrameworkElement
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
            var element = ObjectFinder.Self.GetElementSave("StardewHotbarScreen") ?? throw new System.InvalidOperationException("Could not find an element named StardewHotbarScreen - did you forget to load a Gum project?");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new StardewHotbarScreen(visual);
            visual.Width = 0;
            visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
            visual.Height = 0;
            visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(StardewHotbarScreen)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("StardewHotbarScreen", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public Hotbar HotbarInstance { get; protected set; }
    public ButtonStandard ExitButton { get; protected set; }
    public TextRuntime StatusInfo { get; protected set; }

    public StardewHotbarScreen(InteractiveGue visual) : base(visual)
    {
    }
    public StardewHotbarScreen()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        HotbarInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<Hotbar>(this.Visual,"HotbarInstance");
        ExitButton = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<ButtonStandard>(this.Visual,"ExitButton");
        StatusInfo = this.Visual?.GetGraphicalUiElementByName("StatusInfo") as global::Gum.GueDeriving.TextRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
