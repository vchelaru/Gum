//Code for Controls/PasswordBox (Container)
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
partial class PasswordBox : MonoGameGum.Forms.Controls.PasswordBox
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new MonoGameGum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Controls/PasswordBox");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new PasswordBox(visual);
            return visual;
        });
        MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(PasswordBox)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Controls/PasswordBox", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public enum PasswordBoxCategory
    {
        Enabled,
        Disabled,
        Highlighted,
        Selected,
    }

    PasswordBoxCategory? _passwordBoxCategoryState;
    public PasswordBoxCategory? PasswordBoxCategoryState
    {
        get => _passwordBoxCategoryState;
        set
        {
            _passwordBoxCategoryState = value;
            if(value != null)
            {
                if(Visual.Categories.ContainsKey("PasswordBoxCategory"))
                {
                    var category = Visual.Categories["PasswordBoxCategory"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
                else
                {
                    var category = ((Gum.DataTypes.ElementSave)this.Visual.Tag).Categories.FirstOrDefault(item => item.Name == "PasswordBoxCategory");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
            }
        }
    }
    public NineSliceRuntime Background { get; protected set; }
    public NineSliceRuntime SelectionInstance { get; protected set; }
    public TextRuntime TextInstance { get; protected set; }
    public TextRuntime PlaceholderTextInstance { get; protected set; }
    public NineSliceRuntime FocusedIndicator { get; protected set; }
    public SpriteRuntime CaretInstance { get; protected set; }

    public string PlaceholderText
    {
        get => PlaceholderTextInstance.Text;
        set => PlaceholderTextInstance.Text = value;
    }

    public PasswordBox(InteractiveGue visual) : base(visual) { }
    public PasswordBox()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        Background = this.Visual?.GetGraphicalUiElementByName("Background") as NineSliceRuntime;
        SelectionInstance = this.Visual?.GetGraphicalUiElementByName("SelectionInstance") as NineSliceRuntime;
        TextInstance = this.Visual?.GetGraphicalUiElementByName("TextInstance") as TextRuntime;
        PlaceholderTextInstance = this.Visual?.GetGraphicalUiElementByName("PlaceholderTextInstance") as TextRuntime;
        FocusedIndicator = this.Visual?.GetGraphicalUiElementByName("FocusedIndicator") as NineSliceRuntime;
        CaretInstance = this.Visual?.GetGraphicalUiElementByName("CaretInstance") as SpriteRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
