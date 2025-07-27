//Code for HollowKnightComponents/HealthItem (Container)
using GumRuntime;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
namespace GameUiSamples.Components;
partial class HealthItem : MonoGameGum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new MonoGameGum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("HollowKnightComponents/HealthItem");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new HealthItem(visual);
            return visual;
        });
        MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(HealthItem)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("HollowKnightComponents/HealthItem", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public enum FullEmptyCategory
    {
        Full,
        Empty,
    }

    FullEmptyCategory? _fullEmptyCategoryState;
    public FullEmptyCategory? FullEmptyCategoryState
    {
        get => _fullEmptyCategoryState;
        set
        {
            _fullEmptyCategoryState = value;
            if(value != null)
            {
                if(Visual.Categories.ContainsKey("FullEmptyCategory"))
                {
                    var category = Visual.Categories["FullEmptyCategory"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
                else
                {
                    var category = ((Gum.DataTypes.ElementSave)this.Visual.Tag).Categories.FirstOrDefault(item => item.Name == "FullEmptyCategory");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
            }
        }
    }
    public SpriteRuntime SpriteInstance { get; protected set; }

    public HealthItem(InteractiveGue visual) : base(visual) { }
    public HealthItem()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        SpriteInstance = this.Visual?.GetGraphicalUiElementByName("SpriteInstance") as SpriteRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
