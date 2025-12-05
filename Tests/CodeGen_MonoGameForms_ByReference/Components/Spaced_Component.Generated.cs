//Code for Spaced Component (Container)
using GumRuntime;
using System.Linq;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

namespace CodeGenProject.Components;
partial class Spaced_Component : global::Gum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Spaced Component");
#if DEBUG
if(element == null) throw new System.InvalidOperationException("Could not find an element named Spaced Component - did you forget to load a Gum project?");
#endif
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new Spaced_Component(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(Spaced_Component)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Spaced Component", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public enum Category
    {
        Spaced_State,
    }

    Category? _categoryState;
    public Category? CategoryState
    {
        get => _categoryState;
        set
        {
            _categoryState = value;
            if(value != null)
            {
                if(Visual.Categories.ContainsKey("Category"))
                {
                    var category = Visual.Categories["Category"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
                else
                {
                    var category = ((global::Gum.DataTypes.ElementSave)this.Visual.Tag).Categories.FirstOrDefault(item => item.Name == "Category");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
            }
        }
    }

    public int Spaced_Variable
    {
        get;
        set;
    }
    public Spaced_Component(InteractiveGue visual) : base(visual)
    {
    }
    public Spaced_Component()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
