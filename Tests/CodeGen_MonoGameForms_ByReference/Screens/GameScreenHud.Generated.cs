//Code for GameScreenHud
using CodeGenProject.Components.Controls;
using Gum;
using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary.Graphics;
using System.Linq;
namespace CodeGenProject.Screens;
partial class GameScreenHud : global::Gum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::Gum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("GameScreenHud") ?? throw new System.InvalidOperationException("Could not find an element named GameScreenHud - did you forget to load a Gum project?");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new GameScreenHud(visual);
            visual.Width = 0;
            visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
            visual.Height = 0;
            visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(GameScreenHud)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("GameScreenHud", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public Label HealthLabel { get; protected set; }

    public GameScreenHud(InteractiveGue visual) : base(visual)
    {
    }
    public GameScreenHud()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        HealthLabel = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<Label>(this.Visual,"HealthLabel");
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
