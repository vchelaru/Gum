//Code for GameTitleScreen
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
partial class GameTitleScreen : MonoGameGum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new MonoGameGum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("GameTitleScreen");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new GameTitleScreen(visual);
            visual.Width = 0;
            visual.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
            visual.Height = 0;
            visual.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
            return visual;
        });
        MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(GameTitleScreen)] = template;
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

    public GameTitleScreen(InteractiveGue visual) : base(visual) { }
    public GameTitleScreen()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        ContainerInstance = this.Visual?.GetGraphicalUiElementByName("ContainerInstance") as ContainerRuntime;
        Player1Button = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<TitleScreenButton>(this.Visual,"Player1Button");
        Player2Button = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<TitleScreenButton>(this.Visual,"Player2Button");
        OptionsButton = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<TitleScreenButton>(this.Visual,"OptionsButton");
        ExitButton = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<TitleScreenButton>(this.Visual,"ExitButton");
        TextInstance = this.Visual?.GetGraphicalUiElementByName("TextInstance") as TextRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
