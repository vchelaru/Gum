//Code for Controls/PasswordBox (Container)
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

namespace CodeGen_MonoGame_ByReference.Components.Controls;
partial class PasswordBoxRuntime : ContainerRuntime
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/PasswordBox", typeof(PasswordBoxRuntime));
        global::MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsComponents[typeof(global::MonoGameGum.Forms.Controls.PasswordBox)] = typeof(PasswordBoxRuntime);
    }
    public global::MonoGameGum.Forms.Controls.PasswordBox FormsControl => FormsControlAsObject as global::MonoGameGum.Forms.Controls.PasswordBox;
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
                if(Categories.ContainsKey("PasswordBoxCategory"))
                {
                    var category = Categories["PasswordBoxCategory"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.ApplyState(state);
                }
                else
                {
                    var category = ((global::Gum.DataTypes.ElementSave)this.Tag).Categories.FirstOrDefault(item => item.Name == "PasswordBoxCategory");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.ApplyState(state);
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

    public PasswordBoxRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
    {
        if(fullInstantiation)
        {
            var element = ObjectFinder.Self.GetElementSave("Controls/PasswordBox");
            element?.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
        }



    }
    public override void AfterFullCreation()
    {
        if (FormsControl == null)
        {
            FormsControlAsObject = new global::MonoGameGum.Forms.Controls.PasswordBox(this);
        }
        Background = this.GetGraphicalUiElementByName("Background") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        SelectionInstance = this.GetGraphicalUiElementByName("SelectionInstance") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        TextInstance = this.GetGraphicalUiElementByName("TextInstance") as global::MonoGameGum.GueDeriving.TextRuntime;
        PlaceholderTextInstance = this.GetGraphicalUiElementByName("PlaceholderTextInstance") as global::MonoGameGum.GueDeriving.TextRuntime;
        FocusedIndicator = this.GetGraphicalUiElementByName("FocusedIndicator") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        CaretInstance = this.GetGraphicalUiElementByName("CaretInstance") as global::MonoGameGum.GueDeriving.SpriteRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
