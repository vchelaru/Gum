//Code for AnimatedComponent (Container)
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.StateAnimation.Runtime;
using Gum.Wireframe;
using GumRuntime;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using System.Linq;
namespace CodeGenProject.Components;
partial class AnimatedComponent : global::Gum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("AnimatedComponent");
#if DEBUG
if(element == null) throw new System.InvalidOperationException("Could not find an element named AnimatedComponent - did you forget to load a Gum project?");
#endif
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new AnimatedComponent(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(AnimatedComponent)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("AnimatedComponent", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public enum AnimationCategory
    {
        State1,
        State2,
    }

    AnimationCategory? _animationCategoryState;
    public AnimationCategory? AnimationCategoryState
    {
        get => _animationCategoryState;
        set
        {
            _animationCategoryState = value;
            if(value != null)
            {
                if(Visual.Categories.ContainsKey("AnimationCategory"))
                {
                    var category = Visual.Categories["AnimationCategory"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
                else
                {
                    var category = ((global::Gum.DataTypes.ElementSave)this.Visual.Tag).Categories.FirstOrDefault(item => item.Name == "AnimationCategory");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
            }
        }
    }


    #region Animation Fields
    public AnimationRuntime Animation1 {get; protected set;}
    #endregion
    public AnimatedComponent(InteractiveGue visual) : base(visual)
    {
    }
    public AnimatedComponent()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        Animation1 = this.Visual.GetAnimation("Animation1");
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
