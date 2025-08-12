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

namespace CodeGen_MonoGameForms_FullCodegen.Components;
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
    public enum Category
    {
        Spaced_State,
    }

    Category? mCategoryState;
    public Category? CategoryState
    {
        get => mCategoryState;
        set
        {
            mCategoryState = value;
            var appliedDynamically = false;
            if(!appliedDynamically)
            {
                switch (value)
                {
                    case Category.Spaced_State:
                        break;
                }
            }
        }
    }

    public Spaced_Component(InteractiveGue visual) : base(visual)
    {
        InitializeInstances();
        CustomInitialize();
    }
    public Spaced_Component() : base(new ContainerRuntime())
    {


        InitializeInstances();

        ApplyDefaultVariables();
        AssignParents();
        CustomInitialize();
    }
    protected virtual void InitializeInstances()
    {
        base.ReactToVisualChanged();
    }
    protected virtual void AssignParents()
    {
    }
    private void ApplyDefaultVariables()
    {
    }
    partial void CustomInitialize();
}
