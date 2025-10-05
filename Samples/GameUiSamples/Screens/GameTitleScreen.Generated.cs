//Code for GameTitleScreen
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
partial class GameTitleScreen : global::Gum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("GameTitleScreen");
#if DEBUG
if(element == null) throw new System.InvalidOperationException("Could not find an element named GameTitleScreen - did you forget to load a Gum project?");
#endif
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
        ContainerInstance = this.Visual?.GetGraphicalUiElementByName("ContainerInstance") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        Player1Button = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<TitleScreenButton>(this.Visual,"Player1Button");
        Player2Button = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<TitleScreenButton>(this.Visual,"Player2Button");
        OptionsButton = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<TitleScreenButton>(this.Visual,"OptionsButton");
        ExitButton = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<TitleScreenButton>(this.Visual,"ExitButton");
        TextInstance = this.Visual?.GetGraphicalUiElementByName("TextInstance") as global::MonoGameGum.GueDeriving.TextRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
