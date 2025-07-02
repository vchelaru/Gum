using Microsoft.Xna.Framework;

namespace MonoGameGum.Forms.DefaultVisuals
{
    internal class Styling
    {
        internal class Colors
        {
            public static Color Black { get; private set; } = new Color(0, 0, 0);
            public static Color DarkGray { get; private set; } = new Color(70, 70, 80);
            public static Color Gray { get; private set; } = new Color(130, 130, 130);
            public static Color LightGray { get; private set; } = new Color(170, 170, 170);
            public static Color White { get; private set; } = new Color(255, 255, 255);
            public static Color PrimaryDark { get; private set; } = new Color(4, 120, 137);
            public static Color Primary { get; private set; } = new Color(6, 159, 177);
            public static Color PrimaryLight { get; private set; } = new Color(74, 180, 193);
            public static Color Success { get; private set; } = new Color(62, 167, 48);
            public static Color Warning { get; private set; } = new Color(232, 171, 25);
            public static Color Danger { get; private set; } = new Color(212, 18, 41);

            public static Color Accent { get; private set; } = new Color(140, 48, 138);
            
        }
    }
}
