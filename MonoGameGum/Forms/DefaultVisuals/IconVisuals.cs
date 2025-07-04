using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;

namespace MonoGameGum.Forms.DefaultVisuals
{

    /// <summary>
    /// The purpose for this class is to serve as the default image details from a sprite sheet
    /// While also allowing someone to create instances of this, and change out the active SpriteSheet at any time
    /// Each icon from the Gum Tool should be available here
    /// </summary>
    public class IconVisuals
    {

        public static IconVisuals ActiveVisual {  get; set; }
        public Texture2D SpriteSheet { get; set; }

        public IconVisuals(Texture2D spriteSheet) {
            if (spriteSheet == null)
            {
                this.SpriteSheet = (Texture2D)RenderingLibrary.Content.LoaderManager.Self.GetDisposable($"EmbeddedResource.{SystemManagers.AssemblyPrefix}.UISpriteSheet.png");
            }
            else
            {
                this.SpriteSheet = spriteSheet;
            }

            if (ActiveVisual == null)
            {
                ActiveVisual = this;
            }
        }


        public IconTextureInfo Arrow1 { get; set; } = new IconTextureInfo { TextureLeft = 288, TextureTop = 256, TextureWidth = 32, TextureHeight = 32 }; 
        public IconTextureInfo Arrow2 { get; set; } = new IconTextureInfo { TextureLeft = 320, TextureTop = 256, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo Arrow3 { get; set; } = new IconTextureInfo { TextureLeft = 352, TextureTop = 256, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo Basket { get; set; } = new IconTextureInfo { TextureLeft = 288, TextureTop = 224, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo Battery { get; set; } = new IconTextureInfo { TextureLeft = 320, TextureTop = 224, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo Check { get; set; } = new IconTextureInfo { TextureLeft = 384, TextureTop = 128, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo CheckeredFlag { get; set; } = new IconTextureInfo { TextureLeft = 384, TextureTop = 288, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo Circle1 { get; set; } = new IconTextureInfo { TextureLeft = 448, TextureTop = 128, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo Circle2 { get; set; } = new IconTextureInfo { TextureLeft = 416, TextureTop = 128, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo Close { get; set; } = new IconTextureInfo { TextureLeft = 416, TextureTop = 192, TextureWidth = 32, TextureHeight = 32 }; 
        public IconTextureInfo Crosshairs { get; set; } = new IconTextureInfo { TextureLeft = 352, TextureTop = 288, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo Currency { get; set; } = new IconTextureInfo { TextureLeft = 352, TextureTop = 224, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo Cursor { get; set; } = new IconTextureInfo { TextureLeft = 384, TextureTop = 32, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo CursorText { get; set; } = new IconTextureInfo { TextureLeft = 416, TextureTop = 32, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo Dash { get; set; } = new IconTextureInfo { TextureLeft = 352, TextureTop = 204, TextureWidth = 32, TextureHeight = 20 };
        public IconTextureInfo Delete { get; set; } = new IconTextureInfo { TextureLeft = 288, TextureTop = 320, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo Enter { get; set; } = new IconTextureInfo { TextureLeft = 320, TextureTop = 320, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo Expand { get; set; } = new IconTextureInfo { TextureLeft = 384, TextureTop = 192, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo Gamepad { get; set; } = new IconTextureInfo { TextureLeft = 352, TextureTop = 320, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo GamepadNES { get; set; } = new IconTextureInfo { TextureLeft = 416, TextureTop = 320, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo GamepadSNES { get; set; } = new IconTextureInfo { TextureLeft = 448, TextureTop = 320, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo GamepadNintendo64 { get; set; } = new IconTextureInfo { TextureLeft = 384, TextureTop = 352, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo GamepadGamecube { get; set; } = new IconTextureInfo { TextureLeft = 416, TextureTop = 352, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo GamepadSwitchPro { get; set; } = new IconTextureInfo { TextureLeft = 352, TextureTop = 320, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo GamepadXbox { get; set; } = new IconTextureInfo { TextureLeft = 384, TextureTop = 320, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo GamepadPlaystationDualShock { get; set; } = new IconTextureInfo { TextureLeft = 352, TextureTop = 352, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo GamepadSegaGenesis { get; set; } = new IconTextureInfo { TextureLeft = 448, TextureTop = 352, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo Gear { get; set; } = new IconTextureInfo { TextureLeft = 320, TextureTop = 96, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo FastForward { get; set; } = new IconTextureInfo { TextureLeft = 384, TextureTop = 160, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo FastForwardBar { get; set; } = new IconTextureInfo { TextureLeft = 416, TextureTop = 160, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo FitToScreen { get; set; } = new IconTextureInfo { TextureLeft = 288, TextureTop = 192, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo Flame1 { get; set; } = new IconTextureInfo { TextureLeft = 320, TextureTop = 64, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo Flame2 { get; set; } = new IconTextureInfo { TextureLeft = 352, TextureTop = 64, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo Heart { get; set; } = new IconTextureInfo { TextureLeft = 320, TextureTop = 128, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo Info { get; set; } = new IconTextureInfo { TextureLeft = 416, TextureTop = 256, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo Keyboard { get; set; } = new IconTextureInfo { TextureLeft = 320, TextureTop = 32, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo Leaf { get; set; } = new IconTextureInfo { TextureLeft = 288, TextureTop = 64, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo Lightning { get; set; } = new IconTextureInfo { TextureLeft = 416, TextureTop = 64, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo Minimize { get; set; } = new IconTextureInfo { TextureLeft = 352, TextureTop = 192, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo Monitor { get; set; } = new IconTextureInfo { TextureLeft = 448, TextureTop = 192, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo Mouse { get; set; } = new IconTextureInfo { TextureLeft = 448, TextureTop = 32, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo Music { get; set; } = new IconTextureInfo { TextureLeft = 384, TextureTop = 224, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo Pause { get; set; } = new IconTextureInfo { TextureLeft = 320, TextureTop = 160, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo Pencil { get; set; } = new IconTextureInfo { TextureLeft = 288, TextureTop = 96, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo Play { get; set; } = new IconTextureInfo { TextureLeft = 288, TextureTop = 160, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo PlayBar { get; set; } = new IconTextureInfo { TextureLeft = 448, TextureTop = 160, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo Power { get; set; } = new IconTextureInfo { TextureLeft = 288, TextureTop = 288, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo Radiation { get; set; } = new IconTextureInfo { TextureLeft = 384, TextureTop = 64, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo Reduce { get; set; } = new IconTextureInfo { TextureLeft = 320, TextureTop = 192, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo Shield { get; set; } = new IconTextureInfo { TextureLeft = 416, TextureTop = 288, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo Shot { get; set; } = new IconTextureInfo { TextureLeft = 320, TextureTop = 288, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo Skull { get; set; } = new IconTextureInfo { TextureLeft = 448, TextureTop = 288, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo Sliders { get; set; } = new IconTextureInfo { TextureLeft = 352, TextureTop = 96, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo SoundMaximum { get; set; } = new IconTextureInfo { TextureLeft = 448, TextureTop = 224, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo SoundMinimum { get; set; } = new IconTextureInfo { TextureLeft = 416, TextureTop = 224, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo Speech { get; set; } = new IconTextureInfo { TextureLeft = 448, TextureTop = 96, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo Star { get; set; } = new IconTextureInfo { TextureLeft = 352, TextureTop = 128, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo Stop { get; set; } = new IconTextureInfo { TextureLeft = 352, TextureTop = 160, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo Temperature { get; set; } = new IconTextureInfo { TextureLeft = 448, TextureTop = 64, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo Touch { get; set; } = new IconTextureInfo { TextureLeft = 352, TextureTop = 32, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo Trash { get; set; } = new IconTextureInfo { TextureLeft = 416, TextureTop = 96, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo Trophy { get; set; } = new IconTextureInfo { TextureLeft = 288, TextureTop = 128, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo User { get; set; } = new IconTextureInfo { TextureLeft = 288, TextureTop = 0, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo UserAdd { get; set; } = new IconTextureInfo { TextureLeft = 384, TextureTop = 0, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo UserDelete { get; set; } = new IconTextureInfo { TextureLeft = 416, TextureTop = 0, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo UserGear { get; set; } = new IconTextureInfo { TextureLeft = 352, TextureTop = 0, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo UserMulti { get; set; } = new IconTextureInfo { TextureLeft = 320, TextureTop = 0, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo UserRemove { get; set; } = new IconTextureInfo { TextureLeft = 448, TextureTop = 0, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo Warning { get; set; } = new IconTextureInfo { TextureLeft = 448, TextureTop = 256, TextureWidth = 32, TextureHeight = 32 };
        public IconTextureInfo Wrench { get; set; } = new IconTextureInfo { TextureLeft = 384, TextureTop = 96, TextureWidth = 32, TextureHeight = 32 };

    }
}
