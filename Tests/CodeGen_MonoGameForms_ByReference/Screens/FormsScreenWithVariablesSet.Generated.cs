//Code for FormsScreenWithVariablesSet
using CodeGenProject.Components.Controls;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using System.Linq;
namespace CodeGenProject.Screens;
partial class FormsScreenWithVariablesSet : global::Gum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("FormsScreenWithVariablesSet");
#if DEBUG
if(element == null) throw new System.InvalidOperationException("Could not find an element named FormsScreenWithVariablesSet - did you forget to load a Gum project?");
#endif
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new FormsScreenWithVariablesSet(visual);
            visual.Width = 0;
            visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
            visual.Height = 0;
            visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(FormsScreenWithVariablesSet)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("FormsScreenWithVariablesSet", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public TextBox TextBoxInstance { get; protected set; }
    public TextBox TextBoxInstance1 { get; protected set; }
    public TextBox MaxLettersToShow10 { get; protected set; }
    public TextBox MaxLinesSetTo2 { get; protected set; }
    public StackPanel StackPanelInstance { get; protected set; }

    public FormsScreenWithVariablesSet(InteractiveGue visual) : base(visual)
    {
    }
    public FormsScreenWithVariablesSet()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        TextBoxInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<TextBox>(this.Visual,"TextBoxInstance");
        TextBoxInstance1 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<TextBox>(this.Visual,"TextBoxInstance1");
        MaxLettersToShow10 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<TextBox>(this.Visual,"MaxLettersToShow10");
        MaxLinesSetTo2 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<TextBox>(this.Visual,"MaxLinesSetTo2");
        StackPanelInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<StackPanel>(this.Visual,"StackPanelInstance");
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
