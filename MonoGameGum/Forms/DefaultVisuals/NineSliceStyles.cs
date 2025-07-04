using MonoGameGum.GueDeriving;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Forms.DefaultVisuals
{
    public static class NineSliceStyles
    {
        public static IconTextureInfo Solid { get; set; } = new IconTextureInfo { TextureLeft = 0, TextureTop = 48, TextureWidth = 24, TextureHeight = 24 };
        public static IconTextureInfo Bordered { get; set; } = new IconTextureInfo { TextureLeft = 24, TextureTop = 48, TextureWidth = 24, TextureHeight = 24 };
        public static IconTextureInfo BracketVertical { get; set; } = new IconTextureInfo { TextureLeft = 48, TextureTop = 72, TextureWidth = 24, TextureHeight = 24 };
        public static IconTextureInfo BracketHorizontal { get; set; } = new IconTextureInfo { TextureLeft = 72, TextureTop = 72, TextureWidth = 24, TextureHeight = 24 };
        public static IconTextureInfo Tab { get; set; } = new IconTextureInfo { TextureLeft = 48, TextureTop = 48, TextureWidth = 24, TextureHeight = 24 };
        public static IconTextureInfo TabBordered { get; set; } = new IconTextureInfo { TextureLeft = 72, TextureTop = 48, TextureWidth = 24, TextureHeight = 24 };
        public static IconTextureInfo Outlined { get; set; } = new IconTextureInfo { TextureLeft = 0, TextureTop = 72, TextureWidth = 24, TextureHeight = 24 };
        public static IconTextureInfo OutlinedHeavy { get; set; } = new IconTextureInfo { TextureLeft = 24, TextureTop = 72, TextureWidth = 24, TextureHeight = 24 };
        public static IconTextureInfo Panel { get; set; } = new IconTextureInfo { TextureLeft = 96, TextureTop = 48, TextureWidth = 24, TextureHeight = 24 };

        public static void ApplyIconTextureInfo(NineSliceRuntime runtimeToUpdate, IconTextureInfo iconTextureInfo)
        {
            runtimeToUpdate.TextureLeft = iconTextureInfo.TextureLeft;
            runtimeToUpdate.TextureTop = iconTextureInfo.TextureTop;
            runtimeToUpdate.TextureHeight = iconTextureInfo.TextureHeight;
            runtimeToUpdate.TextureWidth = iconTextureInfo.TextureWidth;
        }

    }
}
