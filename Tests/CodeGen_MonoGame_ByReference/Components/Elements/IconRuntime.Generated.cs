//Code for Elements/Icon (Container)
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
namespace CodeGen_MonoGame_ByReference.Components.Elements;
partial class IconRuntime : ContainerRuntime
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Elements/Icon", typeof(IconRuntime));
    }
    public enum IconCategory
    {
        None,
        ArrowUpDown,
        Arrow1,
        Arrow2,
        Arrow3,
        Basket,
        Battery,
        Check,
        CheckeredFlag,
        Circle1,
        Circle2,
        Close,
        Crosshairs,
        Currency,
        Cursor,
        CursorText,
        Dash,
        Delete,
        Enter,
        Expand,
        Gamepad,
        GamepadNES,
        GamepadSNES,
        GamepadNintendo64,
        GamepadGamecube,
        GamepadSwitchPro,
        GamepadXbox,
        GamepadPlaystationDualShock,
        GamepadSegaGenesis,
        Gear,
        FastForward,
        FastForwardBar,
        FitToScreen,
        Flame1,
        Flame2,
        Heart,
        Info,
        Keyboard,
        Leaf,
        Lightning,
        Minimize,
        Monitor,
        Mouse,
        Music,
        Pause,
        Pencil,
        Play,
        PlayBar,
        Power,
        Radiation,
        Reduce,
        Shield,
        Shot,
        Skull,
        Sliders,
        SoundMaximum,
        SoundMinimum,
        Speech,
        Star,
        Stop,
        Temperature,
        Touch,
        Trash,
        Trophy,
        User,
        UserAdd,
        UserDelete,
        UserGear,
        UserMulti,
        UserRemove,
        Warning,
        Wrench,
    }

    IconCategory? _iconCategoryState;
    public IconCategory? IconCategoryState
    {
        get => _iconCategoryState;
        set
        {
            _iconCategoryState = value;
            if(value != null)
            {
                if(Categories.ContainsKey("IconCategory"))
                {
                    var category = Categories["IconCategory"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.ApplyState(state);
                }
                else
                {
                    var category = ((global::Gum.DataTypes.ElementSave)this.Tag).Categories.FirstOrDefault(item => item.Name == "IconCategory");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.ApplyState(state);
                }
            }
        }
    }
    public SpriteRuntime IconSprite { get; protected set; }

    public string IconColor
    {
        set => IconSprite.SetProperty("ColorCategoryState", value?.ToString());
    }

    public IconRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
    {
        if(fullInstantiation)
        {
            var element = ObjectFinder.Self.GetElementSave("Elements/Icon");
            element?.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
        }



    }
    public override void AfterFullCreation()
    {
        IconSprite = this.GetGraphicalUiElementByName("IconSprite") as global::MonoGameGum.GueDeriving.SpriteRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
