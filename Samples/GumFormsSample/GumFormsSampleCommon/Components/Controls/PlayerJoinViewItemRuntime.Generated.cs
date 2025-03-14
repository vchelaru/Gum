//Code for Controls/PlayerJoinViewItem (Container)
using GumRuntime;
using GumFormsSample.Components;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
namespace GumFormsSample.Components
{
    public partial class PlayerJoinViewItemRuntime:ContainerRuntime
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/PlayerJoinViewItem", typeof(PlayerJoinViewItemRuntime));
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

        public PlayerJoinCategory PlayerJoinCategoryState
        {
            set
            {
                if(Categories.ContainsKey("PlayerJoinCategory"))
                {
                    var category = Categories["PlayerJoinCategory"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.ApplyState(state);
                }
                else
                {
                    var category = ((Gum.DataTypes.ElementSave)this.Tag).Categories.FirstOrDefault(item => item.Name == "PlayerJoinCategory");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.ApplyState(state);
                }
            }
        }

        public PlayerIndexCategory PlayerIndexCategoryState
        {
            set
            {
                if(Categories.ContainsKey("PlayerIndexCategory"))
                {
                    var category = Categories["PlayerIndexCategory"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.ApplyState(state);
                }
                else
                {
                    var category = ((Gum.DataTypes.ElementSave)this.Tag).Categories.FirstOrDefault(item => item.Name == "PlayerIndexCategory");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.ApplyState(state);
                }
            }
        }

        public GamepadLayoutCategory GamepadLayoutCategoryState
        {
            set
            {
                if(Categories.ContainsKey("GamepadLayoutCategory"))
                {
                    var category = Categories["GamepadLayoutCategory"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.ApplyState(state);
                }
                else
                {
                    var category = ((Gum.DataTypes.ElementSave)this.Tag).Categories.FirstOrDefault(item => item.Name == "GamepadLayoutCategory");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.ApplyState(state);
                }
            }
        }
        public NineSliceRuntime Background { get; protected set; }
        public TextRuntime ControllerDisplayNameTextInstance { get; protected set; }
        public IconRuntime InputDeviceIcon { get; protected set; }

        public PlayerJoinViewItemRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {
            if(fullInstantiation)
            {
                var element = ObjectFinder.Self.GetElementSave("Controls/PlayerJoinViewItem");
                element?.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
            }



        }
        public override void AfterFullCreation()
        {
            Background = this.GetGraphicalUiElementByName("Background") as NineSliceRuntime;
            ControllerDisplayNameTextInstance = this.GetGraphicalUiElementByName("ControllerDisplayNameTextInstance") as TextRuntime;
            InputDeviceIcon = this.GetGraphicalUiElementByName("InputDeviceIcon") as IconRuntime;
            CustomInitialize();
        }
        //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
        partial void CustomInitialize();
    }
}
