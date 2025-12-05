//Code for HollowKnightComponents/ManaOrb (Container)
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

using MonoGameGum.GueDeriving;
namespace GameUiSamples.Components;
partial class ManaOrb : global::Gum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("HollowKnightComponents/ManaOrb");
#if DEBUG
if(element == null) throw new System.InvalidOperationException("Could not find an element named HollowKnightComponents/ManaOrb - did you forget to load a Gum project?");
#endif
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new ManaOrb(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(ManaOrb)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("HollowKnightComponents/ManaOrb", () => 
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
                    var category = ((global::Gum.DataTypes.ElementSave)this.Visual.Tag).Categories.FirstOrDefault(item => item.Name == "FullEmptyCategory");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
            }
        }
    }
    public SpriteRuntime OrbBackground { get; protected set; }
    public ContainerRuntime RenderTargetContainer { get; protected set; }
    public SpriteRuntime WaveTop { get; protected set; }
    public SpriteRuntime WaveMaskSprite { get; protected set; }
    public ColoredRectangleRuntime ColoredRectangleInstance { get; protected set; }

    public ManaOrb(InteractiveGue visual) : base(visual)
    {
    }
    public ManaOrb()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        OrbBackground = this.Visual?.GetGraphicalUiElementByName("OrbBackground") as global::MonoGameGum.GueDeriving.SpriteRuntime;
        RenderTargetContainer = this.Visual?.GetGraphicalUiElementByName("RenderTargetContainer") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        WaveTop = this.Visual?.GetGraphicalUiElementByName("WaveTop") as global::MonoGameGum.GueDeriving.SpriteRuntime;
        WaveMaskSprite = this.Visual?.GetGraphicalUiElementByName("WaveMaskSprite") as global::MonoGameGum.GueDeriving.SpriteRuntime;
        ColoredRectangleInstance = this.Visual?.GetGraphicalUiElementByName("ColoredRectangleInstance") as global::MonoGameGum.GueDeriving.ColoredRectangleRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
