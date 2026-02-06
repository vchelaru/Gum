//Code for Controls/PasswordBox (Container)
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using System.Linq;
namespace CodeGen_MonoGameForms_Localization_ByReference.Components.Controls;
partial class PasswordBox : global::Gum.Forms.Controls.PasswordBox
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Controls/PasswordBox");
#if DEBUG
if(element == null) throw new System.InvalidOperationException("Could not find an element named Controls/PasswordBox - did you forget to load a Gum project?");
#endif
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new PasswordBox(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(PasswordBox)] = template;
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(global::Gum.Forms.Controls.PasswordBox)] = template;
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
                    var category = ((global::Gum.DataTypes.ElementSave)this.Visual.Tag).Categories.FirstOrDefault(item => item.Name == "PasswordBoxCategory");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
            }
        }
    }
    public NineSliceRuntime SelectionInstance { get; protected set; }
    public TextRuntime TextInstance { get; protected set; }
    public TextRuntime PlaceholderTextInstance { get; protected set; }
    public SpriteRuntime CaretInstance { get; protected set; }
    public NineSliceRuntime Background { get; protected set; }
    public ContainerRuntime ClipContainer { get; protected set; }
    public NineSliceRuntime FocusedIndicator { get; protected set; }

    public string Placeholder
    {
        get => PlaceholderTextInstance.Text;
        set => PlaceholderTextInstance.Text = value;
    }

    public PasswordBox(InteractiveGue visual) : base(visual)
    {
    }
    public PasswordBox()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        SelectionInstance = this.Visual?.GetGraphicalUiElementByName("SelectionInstance") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        TextInstance = this.Visual?.GetGraphicalUiElementByName("TextInstance") as global::MonoGameGum.GueDeriving.TextRuntime;
        PlaceholderTextInstance = this.Visual?.GetGraphicalUiElementByName("PlaceholderTextInstance") as global::MonoGameGum.GueDeriving.TextRuntime;
        CaretInstance = this.Visual?.GetGraphicalUiElementByName("CaretInstance") as global::MonoGameGum.GueDeriving.SpriteRuntime;
        Background = this.Visual?.GetGraphicalUiElementByName("Background") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        ClipContainer = this.Visual?.GetGraphicalUiElementByName("ClipContainer") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        FocusedIndicator = this.Visual?.GetGraphicalUiElementByName("FocusedIndicator") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    public void ApplyLocalization()
    {
    }
    partial void CustomInitialize();
}
