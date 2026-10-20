//Code for GameTitleScreen
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
partial class GameTitleScreen : global::Gum.Forms.Controls.FrameworkElement
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
            var element = ObjectFinder.Self.GetElementSave("GameTitleScreen") ?? throw new System.InvalidOperationException("Could not find an element named GameTitleScreen - did you forget to load a Gum project?");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new GameTitleScreen(visual);
            visual.Width = 0;
            visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
            visual.Height = 0;
            visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(GameTitleScreen)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("GameTitleScreen", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public ContainerRuntime ContainerInstance { get; protected set; }
    public TitleScreenButton Player1Button { get; protected set; }
    public TitleScreenButton Player2Button { get; protected set; }
    public TitleScreenButton OptionsButton { get; protected set; }
    public TitleScreenButton ExitButton { get; protected set; }
    public TextRuntime TextInstance { get; protected set; }

    public GameTitleScreen(InteractiveGue visual) : base(visual)
    {
    }
    public GameTitleScreen()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        ContainerInstance = this.Visual?.GetGraphicalUiElementByName("ContainerInstance") as global::Gum.GueDeriving.ContainerRuntime;
        Player1Button = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<TitleScreenButton>(this.Visual,"Player1Button");
        Player2Button = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<TitleScreenButton>(this.Visual,"Player2Button");
        OptionsButton = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<TitleScreenButton>(this.Visual,"OptionsButton");
        ExitButton = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<TitleScreenButton>(this.Visual,"ExitButton");
        TextInstance = this.Visual?.GetGraphicalUiElementByName("TextInstance") as global::Gum.GueDeriving.TextRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
