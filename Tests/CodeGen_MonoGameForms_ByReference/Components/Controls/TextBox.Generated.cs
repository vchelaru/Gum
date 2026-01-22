//Code for Controls/TextBox (Container)
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
namespace CodeGenProject.Components.Controls;
partial class TextBox : global::Gum.Forms.Controls.TextBox
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Controls/TextBox");
#if DEBUG
if(element == null) throw new System.InvalidOperationException("Could not find an element named Controls/TextBox - did you forget to load a Gum project?");
#endif
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new TextBox(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(TextBox)] = template;
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(global::Gum.Forms.Controls.TextBox)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Controls/TextBox", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public enum TextBoxCategory
    {
        Enabled,
        Disabled,
        Highlighted,
        Focused,
    }
    public enum LineModeCategory
    {
        Single,
        Multi,
    }

    TextBoxCategory? _textBoxCategoryState;
    public TextBoxCategory? TextBoxCategoryState
    {
        get => _textBoxCategoryState;
        set
        {
            _textBoxCategoryState = value;
            if(value != null)
            {
                if(Visual.Categories.ContainsKey("TextBoxCategory"))
                {
                    var category = Visual.Categories["TextBoxCategory"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
                else
                {
                    var category = ((global::Gum.DataTypes.ElementSave)this.Visual.Tag).Categories.FirstOrDefault(item => item.Name == "TextBoxCategory");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
            }
        }
    }

    LineModeCategory? _lineModeCategoryState;
    public LineModeCategory? LineModeCategoryState
    {
        get => _lineModeCategoryState;
        set
        {
            _lineModeCategoryState = value;
            if(value != null)
            {
                if(Visual.Categories.ContainsKey("LineModeCategory"))
                {
                    var category = Visual.Categories["LineModeCategory"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
                else
                {
                    var category = ((global::Gum.DataTypes.ElementSave)this.Visual.Tag).Categories.FirstOrDefault(item => item.Name == "LineModeCategory");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
            }
        }
    }
    public NineSliceRuntime Background { get; protected set; }
    public ContainerRuntime ClipContainer { get; protected set; }
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

    public int? MaxLettersToShow
    {
        get => TextInstance.MaxLettersToShow;
        set => TextInstance.MaxLettersToShow = value;
    }

    public int? MaxNumberOfLines
    {
        get => TextInstance.MaxNumberOfLines;
        set => TextInstance.MaxNumberOfLines = value;
    }

    public string Text
    {
        get => TextInstance.Text;
        set => TextInstance.Text = value;
    }

    public TextBox(InteractiveGue visual) : base(visual)
    {
    }
    public TextBox()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        Background = this.Visual?.GetGraphicalUiElementByName("Background") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        ClipContainer = this.Visual?.GetGraphicalUiElementByName("ClipContainer") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        SelectionInstance = this.Visual?.GetGraphicalUiElementByName("SelectionInstance") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        TextInstance = this.Visual?.GetGraphicalUiElementByName("TextInstance") as global::MonoGameGum.GueDeriving.TextRuntime;
        PlaceholderTextInstance = this.Visual?.GetGraphicalUiElementByName("PlaceholderTextInstance") as global::MonoGameGum.GueDeriving.TextRuntime;
        FocusedIndicator = this.Visual?.GetGraphicalUiElementByName("FocusedIndicator") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        CaretInstance = this.Visual?.GetGraphicalUiElementByName("CaretInstance") as global::MonoGameGum.GueDeriving.SpriteRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
