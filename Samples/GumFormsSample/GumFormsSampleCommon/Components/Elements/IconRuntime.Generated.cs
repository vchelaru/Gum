//Code for Elements/Icon (Container)
using GumRuntime;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
namespace GumFormsSample.Components
{
    public partial class IconRuntime:ContainerRuntime
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

        IconCategory mIconCategoryState;
        public IconCategory IconCategoryState
        {
            get => mIconCategoryState;
            set
            {
                mIconCategoryState = value;
                var appliedDynamically = false;
                if(!appliedDynamically)
                {
                    switch (value)
                    {
                        case IconCategory.None:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 288;
                            this.IconSprite.TextureTop = 0;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = false;
                            break;
                        case IconCategory.ArrowUpDown:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 384;
                            this.IconSprite.TextureTop = 256;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.Arrow1:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 288;
                            this.IconSprite.TextureTop = 256;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.Arrow2:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 320;
                            this.IconSprite.TextureTop = 256;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.Arrow3:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 352;
                            this.IconSprite.TextureTop = 256;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.Basket:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 288;
                            this.IconSprite.TextureTop = 224;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.Battery:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 320;
                            this.IconSprite.TextureTop = 224;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.Check:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 384;
                            this.IconSprite.TextureTop = 128;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.CheckeredFlag:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 384;
                            this.IconSprite.TextureTop = 288;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.Circle1:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 448;
                            this.IconSprite.TextureTop = 128;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.Circle2:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 416;
                            this.IconSprite.TextureTop = 128;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.Close:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 416;
                            this.IconSprite.TextureTop = 192;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.Crosshairs:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 352;
                            this.IconSprite.TextureTop = 288;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.Currency:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 352;
                            this.IconSprite.TextureTop = 224;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.Cursor:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 384;
                            this.IconSprite.TextureTop = 32;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.CursorText:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 416;
                            this.IconSprite.TextureTop = 32;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.Delete:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 288;
                            this.IconSprite.TextureTop = 320;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.Enter:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 320;
                            this.IconSprite.TextureTop = 320;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.Expand:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 384;
                            this.IconSprite.TextureTop = 192;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.Gamepad:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 352;
                            this.IconSprite.TextureTop = 320;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.GamepadNES:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 416;
                            this.IconSprite.TextureTop = 320;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.GamepadSNES:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 448;
                            this.IconSprite.TextureTop = 320;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.GamepadNintendo64:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 384;
                            this.IconSprite.TextureTop = 352;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.GamepadGamecube:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 416;
                            this.IconSprite.TextureTop = 352;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.GamepadSwitchPro:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 352;
                            this.IconSprite.TextureTop = 320;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.GamepadXbox:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 384;
                            this.IconSprite.TextureTop = 320;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.GamepadPlaystationDualShock:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 352;
                            this.IconSprite.TextureTop = 352;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.GamepadSegaGenesis:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 448;
                            this.IconSprite.TextureTop = 352;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.Gear:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 320;
                            this.IconSprite.TextureTop = 96;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.FastForward:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 384;
                            this.IconSprite.TextureTop = 160;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.FastForwardBar:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 416;
                            this.IconSprite.TextureTop = 160;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.FitToScreen:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 288;
                            this.IconSprite.TextureTop = 192;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.Flame1:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 320;
                            this.IconSprite.TextureTop = 64;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.Flame2:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 352;
                            this.IconSprite.TextureTop = 64;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.Heart:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 320;
                            this.IconSprite.TextureTop = 128;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.Info:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 416;
                            this.IconSprite.TextureTop = 256;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.Keyboard:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 320;
                            this.IconSprite.TextureTop = 32;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.Leaf:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 288;
                            this.IconSprite.TextureTop = 64;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.Lightning:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 416;
                            this.IconSprite.TextureTop = 64;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.Minimize:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 352;
                            this.IconSprite.TextureTop = 192;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.Monitor:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 448;
                            this.IconSprite.TextureTop = 192;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.Mouse:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 448;
                            this.IconSprite.TextureTop = 32;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.Music:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 384;
                            this.IconSprite.TextureTop = 224;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.Pause:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 320;
                            this.IconSprite.TextureTop = 160;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.Pencil:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 288;
                            this.IconSprite.TextureTop = 96;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.Play:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 288;
                            this.IconSprite.TextureTop = 160;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.PlayBar:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 448;
                            this.IconSprite.TextureTop = 160;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.Power:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 288;
                            this.IconSprite.TextureTop = 288;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.Radiation:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 384;
                            this.IconSprite.TextureTop = 64;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.Reduce:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 320;
                            this.IconSprite.TextureTop = 192;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.Shield:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 416;
                            this.IconSprite.TextureTop = 288;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.Shot:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 320;
                            this.IconSprite.TextureTop = 288;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.Skull:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 448;
                            this.IconSprite.TextureTop = 288;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.Sliders:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 352;
                            this.IconSprite.TextureTop = 96;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.SoundMaximum:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 448;
                            this.IconSprite.TextureTop = 224;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.SoundMinimum:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 416;
                            this.IconSprite.TextureTop = 224;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.Speech:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 448;
                            this.IconSprite.TextureTop = 96;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.Star:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 352;
                            this.IconSprite.TextureTop = 128;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.Stop:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 352;
                            this.IconSprite.TextureTop = 160;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.Temperature:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 448;
                            this.IconSprite.TextureTop = 64;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.Touch:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 352;
                            this.IconSprite.TextureTop = 32;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.Trash:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 416;
                            this.IconSprite.TextureTop = 96;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.Trophy:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 288;
                            this.IconSprite.TextureTop = 128;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.User:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 288;
                            this.IconSprite.TextureTop = 0;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.UserAdd:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 384;
                            this.IconSprite.TextureTop = 0;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.UserDelete:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 416;
                            this.IconSprite.TextureTop = 0;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.UserGear:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 352;
                            this.IconSprite.TextureTop = 0;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.UserMulti:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 320;
                            this.IconSprite.TextureTop = 0;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.UserRemove:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 448;
                            this.IconSprite.TextureTop = 0;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.Warning:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 448;
                            this.IconSprite.TextureTop = 256;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
                        case IconCategory.Wrench:
                            this.IconSprite.TextureHeight = 32;
                            this.IconSprite.TextureLeft = 384;
                            this.IconSprite.TextureTop = 96;
                            this.IconSprite.TextureWidth = 32;
                            this.IconSprite.Visible = true;
                            break;
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
            }

            this.Height = 32f;
             
            this.Width = 32f;

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
            IconSprite = new SpriteRuntime();
            IconSprite.Name = "IconSprite";
        }
        protected virtual void AssignParents()
        {
            this.Children.Add(IconSprite);
        }
        private void ApplyDefaultVariables()
        {
IconSprite.SetProperty("ColorCategoryState", "White");
            this.IconSprite.Height = 0f;
            this.IconSprite.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.IconSprite.SourceFileName = @"UISpriteSheet.png";
            this.IconSprite.TextureAddress = global::Gum.Managers.TextureAddress.Custom;
            this.IconSprite.TextureHeight = 32;
            this.IconSprite.TextureLeft = 288;
            this.IconSprite.TextureTop = 0;
            this.IconSprite.TextureWidth = 32;
            this.IconSprite.Width = 0f;
            this.IconSprite.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.IconSprite.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.IconSprite.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.IconSprite.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.IconSprite.YUnits = GeneralUnitType.PixelsFromMiddle;

        }
        partial void CustomInitialize();
    }
}
