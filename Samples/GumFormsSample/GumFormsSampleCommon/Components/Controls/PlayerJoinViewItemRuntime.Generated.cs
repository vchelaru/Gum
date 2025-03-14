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

        PlayerJoinCategory mPlayerJoinCategoryState;
        public PlayerJoinCategory PlayerJoinCategoryState
        {
            get => mPlayerJoinCategoryState;
            set
            {
                mPlayerJoinCategoryState = value;
                var appliedDynamically = false;
                if(!appliedDynamically)
                {
                    switch (value)
                    {
                        case PlayerJoinCategory.NotConnected:
                            Background.SetProperty("ColorCategoryState", "Gray");
                            this.ControllerDisplayNameTextInstance.Visible = false;
                            break;
                        case PlayerJoinCategory.Connected:
                            Background.SetProperty("ColorCategoryState", "Primary");
                            this.ControllerDisplayNameTextInstance.Visible = true;
                            break;
                        case PlayerJoinCategory.ConnectedAndJoined:
                            Background.SetProperty("ColorCategoryState", "Success");
                            this.ControllerDisplayNameTextInstance.Visible = true;
                            break;
                    }
                }
            }
        }

        PlayerIndexCategory mPlayerIndexCategoryState;
        public PlayerIndexCategory PlayerIndexCategoryState
        {
            get => mPlayerIndexCategoryState;
            set
            {
                mPlayerIndexCategoryState = value;
                var appliedDynamically = false;
                if(!appliedDynamically)
                {
                    switch (value)
                    {
                        case PlayerIndexCategory.Player1:
                            break;
                        case PlayerIndexCategory.Player2:
                            break;
                        case PlayerIndexCategory.Player3:
                            break;
                        case PlayerIndexCategory.Player4:
                            break;
                    }
                }
            }
        }

        GamepadLayoutCategory mGamepadLayoutCategoryState;
        public GamepadLayoutCategory GamepadLayoutCategoryState
        {
            get => mGamepadLayoutCategoryState;
            set
            {
                mGamepadLayoutCategoryState = value;
                var appliedDynamically = false;
                if(!appliedDynamically)
                {
                    switch (value)
                    {
                        case GamepadLayoutCategory.Unknown:
                            this.InputDeviceIcon.IconCategoryState = IconRuntime.IconCategory.GamepadXbox;
                            this.InputDeviceIcon.Visible = false;
                            break;
                        case GamepadLayoutCategory.Keyboard:
                            this.InputDeviceIcon.IconCategoryState = IconRuntime.IconCategory.Keyboard;
                            this.InputDeviceIcon.Visible = true;
                            break;
                        case GamepadLayoutCategory.NES:
                            this.InputDeviceIcon.IconCategoryState = IconRuntime.IconCategory.GamepadNES;
                            this.InputDeviceIcon.Visible = true;
                            break;
                        case GamepadLayoutCategory.SuperNintendo:
                            this.InputDeviceIcon.IconCategoryState = IconRuntime.IconCategory.GamepadSNES;
                            this.InputDeviceIcon.Visible = true;
                            break;
                        case GamepadLayoutCategory.Nintendo64:
                            this.InputDeviceIcon.IconCategoryState = IconRuntime.IconCategory.GamepadNintendo64;
                            this.InputDeviceIcon.Visible = true;
                            break;
                        case GamepadLayoutCategory.GameCube:
                            this.InputDeviceIcon.IconCategoryState = IconRuntime.IconCategory.GamepadGamecube;
                            this.InputDeviceIcon.Visible = true;
                            break;
                        case GamepadLayoutCategory.SwitchPro:
                            this.InputDeviceIcon.IconCategoryState = IconRuntime.IconCategory.GamepadSwitchPro;
                            this.InputDeviceIcon.Visible = true;
                            break;
                        case GamepadLayoutCategory.Genesis:
                            this.InputDeviceIcon.IconCategoryState = IconRuntime.IconCategory.GamepadSegaGenesis;
                            this.InputDeviceIcon.Visible = true;
                            break;
                        case GamepadLayoutCategory.Xbox360:
                            this.InputDeviceIcon.IconCategoryState = IconRuntime.IconCategory.GamepadXbox;
                            this.InputDeviceIcon.Visible = true;
                            break;
                        case GamepadLayoutCategory.PlayStationDualShock:
                            this.InputDeviceIcon.IconCategoryState = IconRuntime.IconCategory.GamepadPlaystationDualShock;
                            this.InputDeviceIcon.Visible = true;
                            break;
                    }
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
            }

            this.Height = 80f;
             

            InitializeInstances();

            ApplyDefaultVariables();
            AssignParents();
            if(tryCreateFormsObject)
            {
            }
            CustomInitialize();
        }
        protected virtual void InitializeInstances()
        {
            Background = new NineSliceRuntime();
            Background.Name = "Background";
            ControllerDisplayNameTextInstance = new TextRuntime();
            ControllerDisplayNameTextInstance.Name = "ControllerDisplayNameTextInstance";
            InputDeviceIcon = new IconRuntime();
            InputDeviceIcon.Name = "InputDeviceIcon";
        }
        protected virtual void AssignParents()
        {
            this.Children.Add(Background);
            this.Children.Add(ControllerDisplayNameTextInstance);
            this.Children.Add(InputDeviceIcon);
        }
        private void ApplyDefaultVariables()
        {
Background.SetProperty("ColorCategoryState", "Black");
Background.SetProperty("StyleCategoryState", "Panel");
            this.Background.Height = 0f;
            this.Background.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.Background.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Bottom;
            this.Background.YUnits = GeneralUnitType.PixelsFromLarge;

ControllerDisplayNameTextInstance.SetProperty("ColorCategoryState", "White");
ControllerDisplayNameTextInstance.SetProperty("StyleCategoryState", "Tiny");
            this.ControllerDisplayNameTextInstance.Height = 0f;
            this.ControllerDisplayNameTextInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.ControllerDisplayNameTextInstance.HorizontalAlignment = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.ControllerDisplayNameTextInstance.Text = @"<Controller Type>";
            this.ControllerDisplayNameTextInstance.VerticalAlignment = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            this.ControllerDisplayNameTextInstance.Width = -16f;
            this.ControllerDisplayNameTextInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.ControllerDisplayNameTextInstance.X = 0f;
            this.ControllerDisplayNameTextInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.ControllerDisplayNameTextInstance.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.ControllerDisplayNameTextInstance.Y = -29f;
            this.ControllerDisplayNameTextInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            this.ControllerDisplayNameTextInstance.YUnits = GeneralUnitType.PixelsFromLarge;

this.InputDeviceIcon.IconCategoryState = IconRuntime.IconCategory.GamepadXbox;
            this.InputDeviceIcon.X = 0f;
            this.InputDeviceIcon.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.InputDeviceIcon.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.InputDeviceIcon.Y = 0f;
            this.InputDeviceIcon.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.InputDeviceIcon.YUnits = GeneralUnitType.PixelsFromMiddle;

        }
        partial void CustomInitialize();
    }
}
