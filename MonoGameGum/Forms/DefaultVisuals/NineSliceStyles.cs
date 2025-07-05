using Gum.DataTypes.Variables;
using MonoGameGum.GueDeriving;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Forms.DefaultVisuals;

public static class NineSliceStyles
{
    public static StateSave Solid = CreateTextureCoordinateState(0, 48, 24, 24);
    public static StateSave Bordered = CreateTextureCoordinateState(24, 48, 24, 24);
    public static StateSave BracketVertical = CreateTextureCoordinateState(48, 72, 24, 24);
    public static StateSave BracketHorizontal = CreateTextureCoordinateState(72, 72, 24, 24);
    public static StateSave Tab = CreateTextureCoordinateState(48, 48, 24, 24);
    public static StateSave TabBordered = CreateTextureCoordinateState(72, 48, 24, 24);
    public static StateSave Outlined = CreateTextureCoordinateState(0, 72, 24, 24);
    public static StateSave OutlinedHeavy = CreateTextureCoordinateState(24, 72, 24, 24);
    public static StateSave Panel = CreateTextureCoordinateState(96, 48, 24, 24);

    private static StateSave CreateTextureCoordinateState(int left, int top, int width, int height)
    {
        return new()
        {
            Variables = new()
            {
                new () { Name = "TextureLeft", Type = "int", Value = left },
                new () { Name = "TextureTop", Type = "int", Value = top },
                new () { Name = "TextureWidth", Type = "int", Value = width },
                new () { Name = "TextureHeight", Type = "int", Value = height },
                new () { Name = "TextureAddress", Type = "int", Value = Gum.Managers.TextureAddress.Custom }
            }
        };
    }
}
