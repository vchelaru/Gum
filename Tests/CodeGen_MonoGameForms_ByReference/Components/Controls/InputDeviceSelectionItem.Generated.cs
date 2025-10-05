//Code for Controls/InputDeviceSelectionItem (Container)
using GumRuntime;
using System.Linq;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using CodeGenProject.Components.Elements;
using CodeGenProject.Components.Controls;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

namespace CodeGenProject.Components.Controls;
partial class InputDeviceSelectionItem : global::Gum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Controls/InputDeviceSelectionItem");
#if DEBUG
if(element == null) throw new System.InvalidOperationException("Could not find an element named Controls/InputDeviceSelectionItem - did you forget to load a Gum project?");
#endif
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new InputDeviceSelectionItem(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(InputDeviceSelectionItem)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Controls/InputDeviceSelectionItem", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public enum JoinedCategory
    {
        NoInputDevice,
        HasInputDevice,
    }

    JoinedCategory? _joinedCategoryState;
    public JoinedCategory? JoinedCategoryState
    {
        get => _joinedCategoryState;
        set
        {
            _joinedCategoryState = value;
            if(value != null)
            {
                if(Visual.Categories.ContainsKey("JoinedCategory"))
                {
                    var category = Visual.Categories["JoinedCategory"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
                else
                {
                    var category = ((global::Gum.DataTypes.ElementSave)this.Visual.Tag).Categories.FirstOrDefault(item => item.Name == "JoinedCategory");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
            }
        }
    }
    public NineSliceRuntime Background { get; protected set; }
    public Icon IconInstance { get; protected set; }
    public TextRuntime TextInstance { get; protected set; }
    public ButtonClose RemoveDeviceButtonInstance { get; protected set; }

    public InputDeviceSelectionItem(InteractiveGue visual) : base(visual)
    {
    }
    public InputDeviceSelectionItem()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        Background = this.Visual?.GetGraphicalUiElementByName("Background") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        IconInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<Icon>(this.Visual,"IconInstance");
        TextInstance = this.Visual?.GetGraphicalUiElementByName("TextInstance") as global::MonoGameGum.GueDeriving.TextRuntime;
        RemoveDeviceButtonInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<ButtonClose>(this.Visual,"RemoveDeviceButtonInstance");
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
