//Code for Controls/PlayerJoinViewItem (Container)
using GumRuntime;
using System.Linq;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using CodeGenProject.Components.Elements;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

namespace CodeGenProject.Components.Controls;
partial class PlayerJoinViewItem : MonoGameGum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::MonoGameGum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Controls/PlayerJoinViewItem");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new PlayerJoinViewItem(visual);
            return visual;
        });
        global::MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(PlayerJoinViewItem)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Controls/PlayerJoinViewItem", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public enum PlayerJoinCategory
    {
        NotConnected,
        Connected,
        ConnectedAndJoined,
    }
    public enum PlayerIndexCategory
    {
        Player1,
        Player2,
        Player3,
        Player4,
    }
    public enum GamepadLayoutCategory
    {
        Unknown,
        Keyboard,
        NES,
        SuperNintendo,
        Nintendo64,
        GameCube,
        SwitchPro,
        Genesis,
        Xbox360,
        PlayStationDualShock,
    }

    PlayerJoinCategory? _playerJoinCategoryState;
    public PlayerJoinCategory? PlayerJoinCategoryState
    {
        get => _playerJoinCategoryState;
        set
        {
            _playerJoinCategoryState = value;
            if(value != null)
            {
                if(Visual.Categories.ContainsKey("PlayerJoinCategory"))
                {
                    var category = Visual.Categories["PlayerJoinCategory"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
                else
                {
                    var category = ((global::Gum.DataTypes.ElementSave)this.Visual.Tag).Categories.FirstOrDefault(item => item.Name == "PlayerJoinCategory");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
            }
        }
    }

    PlayerIndexCategory? _playerIndexCategoryState;
    public PlayerIndexCategory? PlayerIndexCategoryState
    {
        get => _playerIndexCategoryState;
        set
        {
            _playerIndexCategoryState = value;
            if(value != null)
            {
                if(Visual.Categories.ContainsKey("PlayerIndexCategory"))
                {
                    var category = Visual.Categories["PlayerIndexCategory"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
                else
                {
                    var category = ((global::Gum.DataTypes.ElementSave)this.Visual.Tag).Categories.FirstOrDefault(item => item.Name == "PlayerIndexCategory");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
            }
        }
    }

    GamepadLayoutCategory? _gamepadLayoutCategoryState;
    public GamepadLayoutCategory? GamepadLayoutCategoryState
    {
        get => _gamepadLayoutCategoryState;
        set
        {
            _gamepadLayoutCategoryState = value;
            if(value != null)
            {
                if(Visual.Categories.ContainsKey("GamepadLayoutCategory"))
                {
                    var category = Visual.Categories["GamepadLayoutCategory"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
                else
                {
                    var category = ((global::Gum.DataTypes.ElementSave)this.Visual.Tag).Categories.FirstOrDefault(item => item.Name == "GamepadLayoutCategory");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
            }
        }
    }
    public NineSliceRuntime Background { get; protected set; }
    public TextRuntime ControllerDisplayNameTextInstance { get; protected set; }
    public Icon InputDeviceIcon { get; protected set; }

    public PlayerJoinViewItem(InteractiveGue visual) : base(visual)
    {
    }
    public PlayerJoinViewItem()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        Background = this.Visual?.GetGraphicalUiElementByName("Background") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        ControllerDisplayNameTextInstance = this.Visual?.GetGraphicalUiElementByName("ControllerDisplayNameTextInstance") as global::MonoGameGum.GueDeriving.TextRuntime;
        InputDeviceIcon = global::MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<Icon>(this.Visual,"InputDeviceIcon");
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
