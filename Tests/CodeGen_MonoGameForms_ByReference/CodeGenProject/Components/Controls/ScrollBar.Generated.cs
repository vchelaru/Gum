//Code for Controls/ScrollBar (Container)
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

namespace CodeGenProject.Components.Controls;
partial class ScrollBar : global::MonoGameGum.Forms.Controls.ScrollBar
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::MonoGameGum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Controls/ScrollBar");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new ScrollBar(visual);
            return visual;
        });
        global::MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(ScrollBar)] = template;
        global::MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(global::MonoGameGum.Forms.Controls.ScrollBar)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Controls/ScrollBar", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public enum ScrollBarCategory
    {
    }

    ScrollBarCategory? _scrollBarCategoryState;
    public ScrollBarCategory? ScrollBarCategoryState
    {
        get => _scrollBarCategoryState;
        set
        {
            _scrollBarCategoryState = value;
            if(value != null)
            {
                if(Visual.Categories.ContainsKey("ScrollBarCategory"))
                {
                    var category = Visual.Categories["ScrollBarCategory"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
                else
                {
                    var category = ((global::Gum.DataTypes.ElementSave)this.Visual.Tag).Categories.FirstOrDefault(item => item.Name == "ScrollBarCategory");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
            }
        }
    }
    public ButtonIcon UpButtonInstance { get; protected set; }
    public ButtonIcon DownButtonInstance { get; protected set; }
    public ContainerRuntime TrackInstance { get; protected set; }
    public NineSliceRuntime TrackBackground { get; protected set; }
    public ButtonStandard ThumbInstance { get; protected set; }

    public ScrollBar(InteractiveGue visual) : base(visual)
    {
    }
    public ScrollBar()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        UpButtonInstance = global::MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ButtonIcon>(this.Visual,"UpButtonInstance");
        DownButtonInstance = global::MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ButtonIcon>(this.Visual,"DownButtonInstance");
        TrackInstance = this.Visual?.GetGraphicalUiElementByName("TrackInstance") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        TrackBackground = this.Visual?.GetGraphicalUiElementByName("TrackBackground") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        ThumbInstance = global::MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ButtonStandard>(this.Visual,"ThumbInstance");
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
