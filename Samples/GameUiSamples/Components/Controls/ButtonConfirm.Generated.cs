//Code for Controls/ButtonConfirm (Container)
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
partial class ButtonConfirm : MonoGameGum.Forms.Controls.Button
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new MonoGameGum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Controls/ButtonConfirm");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new ButtonConfirm(visual);
            return visual;
        });
        MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(ButtonConfirm)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Controls/ButtonConfirm", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public enum ButtonCategory
    {
        Enabled,
        Disabled,
        Highlighted,
        Pushed,
        HighlightedFocused,
        Focused,
        DisabledFocused,
    }

    ButtonCategory? _buttonCategoryState;
    public ButtonCategory? ButtonCategoryState
    {
        get => _buttonCategoryState;
        set
        {
            _buttonCategoryState = value;
            if(value != null)
            {
                if(Visual.Categories.ContainsKey("ButtonCategory"))
                {
                    var category = Visual.Categories["ButtonCategory"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
                else
                {
                    var category = ((Gum.DataTypes.ElementSave)this.Visual.Tag).Categories.FirstOrDefault(item => item.Name == "ButtonCategory");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
            }
        }
    }
    public NineSliceRuntime Background { get; protected set; }
    public TextRuntime TextInstance { get; protected set; }
    public NineSliceRuntime FocusedIndicator { get; protected set; }

    public string ButtonDisplayText
    {
        get => TextInstance.Text;
        set => TextInstance.Text = value;
    }

    public ButtonConfirm(InteractiveGue visual) : base(visual) { }
    public ButtonConfirm()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        Background = this.Visual?.GetGraphicalUiElementByName("Background") as NineSliceRuntime;
        TextInstance = this.Visual?.GetGraphicalUiElementByName("TextInstance") as TextRuntime;
        FocusedIndicator = this.Visual?.GetGraphicalUiElementByName("FocusedIndicator") as NineSliceRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
