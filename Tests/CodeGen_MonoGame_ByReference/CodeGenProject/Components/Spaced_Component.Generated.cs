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
partial class Spaced_Component : MonoGameGum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::MonoGameGum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Spaced Component");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new Spaced_Component(visual);
            return visual;
        });
        global::MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(Spaced_Component)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Spaced Component", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public enum Spaced_State_Category
    {
        Spaced_State,
    }

    Spaced_State_Category? _spaced_State_CategoryState;
    public Spaced_State_Category? Spaced_State_CategoryState
    {
        get => _spaced_State_CategoryState;
        set
        {
            _spaced_State_CategoryState = value;
            if(value != null)
            {
                if(Visual.Categories.ContainsKey("Spaced State Category"))
                {
                    var category = Visual.Categories["Spaced State Category"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
                else
                {
                    var category = ((global::Gum.DataTypes.ElementSave)this.Visual.Tag).Categories.FirstOrDefault(item => item.Name == "Spaced State Category");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
            }
        }
    }

    public float Spaced_State_Variable
    {
        get;
        set;
    }
    public float Spaced_Variable
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
