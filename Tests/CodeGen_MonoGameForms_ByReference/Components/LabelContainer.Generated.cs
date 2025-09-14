//Code for LabelContainer (Container)
using GumRuntime;
using System.Linq;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using CodeGenProject.Components.Controls;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

namespace CodeGenProject.Components;
partial class LabelContainer : MonoGameGum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::MonoGameGum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("LabelContainer");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new LabelContainer(visual);
            return visual;
        });
        global::MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(LabelContainer)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("LabelContainer", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public enum Category1
    {
        SetState1,
    }

    Category1? _category1State;
    public Category1? Category1State
    {
        get => _category1State;
        set
        {
            _category1State = value;
            if(value != null)
            {
                if(Visual.Categories.ContainsKey("Category1"))
                {
                    var category = Visual.Categories["Category1"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
                else
                {
                    var category = ((global::Gum.DataTypes.ElementSave)this.Visual.Tag).Categories.FirstOrDefault(item => item.Name == "Category1");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
            }
        }
    }
    public Label LabelInstance { get; protected set; }
    public ColoredRectangleRuntime NonLabelShouldAppearAfterLabel { get; protected set; }

    public string Text
    {
        get => LabelInstance.Text;
        set => LabelInstance.Text = value;
    }

    public LabelContainer(InteractiveGue visual) : base(visual)
    {
    }
    public LabelContainer()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        LabelInstance = global::MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<Label>(this.Visual,"LabelInstance");
        NonLabelShouldAppearAfterLabel = this.Visual?.GetGraphicalUiElementByName("NonLabelShouldAppearAfterLabel") as global::MonoGameGum.GueDeriving.ColoredRectangleRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
