//Code for Controls/TreeViewToggle (Container)
using GumRuntime;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using GameUiSamples.Components;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
namespace GameUiSamples.Components;
partial class TreeViewToggle : MonoGameGum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new MonoGameGum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Controls/TreeViewToggle");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new TreeViewToggle(visual);
            return visual;
        });
        MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(TreeViewToggle)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Controls/TreeViewToggle", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public enum ToggleCategory
    {
        EnabledOn,
        EnabledOff,
        DisabledOn,
        DisabledOff,
        HighlightedOn,
        HighlightedOff,
        PushedOn,
        PushedOff,
    }

    ToggleCategory? _toggleCategoryState;
    public ToggleCategory? ToggleCategoryState
    {
        get => _toggleCategoryState;
        set
        {
            _toggleCategoryState = value;
            if(value != null)
            {
                if(Visual.Categories.ContainsKey("ToggleCategory"))
                {
                    var category = Visual.Categories["ToggleCategory"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
                else
                {
                    var category = ((Gum.DataTypes.ElementSave)this.Visual.Tag).Categories.FirstOrDefault(item => item.Name == "ToggleCategory");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
            }
        }
    }
    public NineSliceRuntime NineSliceInstance { get; protected set; }
    public Icon IconInstance { get; protected set; }

    public TreeViewToggle(InteractiveGue visual) : base(visual) { }
    public TreeViewToggle()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        NineSliceInstance = this.Visual?.GetGraphicalUiElementByName("NineSliceInstance") as NineSliceRuntime;
        IconInstance = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<Icon>(this.Visual,"IconInstance");
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
