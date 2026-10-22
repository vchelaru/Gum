using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using Shouldly;
using System;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries;

/// <summary>
/// Every renderable treats NaN and infinity the same on X, Y, Width, Height and Rotation (#6477): with
/// FULL_DIAGNOSTICS each one rejects all three values with an ArgumentException and does not store them, and
/// without it none of them throws. Before, InvisibleRenderable rejected only a +infinity Height, Sprite only a NaN
/// Height, NineSlice only a NaN X/Y, and most of the others rejected nothing.
/// </summary>
public class RenderableSizeNonFiniteTests
{
    private static readonly string[] Variables = { "X", "Y", "Width", "Height", "Rotation" };
    private static readonly float[] NonFinite = { float.PositiveInfinity, float.NegativeInfinity, float.NaN };

    private static readonly string[] Renderables =
    {
        "InvisibleRenderable", "Sprite", "NineSlice", "SolidRectangle", "LineRectangle", "Line", "LineCircle",
        "FilledStrokedRectangle", "Text", "LinePolygon", "LineGrid",
    };

    public static TheoryData<string, string, float> Cases()
    {
        TheoryData<string, string, float> data = new();
        foreach (string renderable in Renderables)
        {
            foreach (string variable in Variables)
            {
                foreach (float value in NonFinite)
                {
                    data.Add(renderable, variable, value);
                }
            }
        }
        return data;
    }

    private static IPositionedSizedObject Create(string name) => name switch
    {
        "InvisibleRenderable" => new InvisibleRenderable(),
        "Sprite" => new Sprite((Texture2D?)null),
        "NineSlice" => new NineSlice(),
        "SolidRectangle" => new SolidRectangle(),
        "LineRectangle" => new LineRectangle(null),
        "Line" => new Line(null),
        "LineCircle" => new LineCircle(null),
        "FilledStrokedRectangle" => new FilledStrokedRectangle(null),
        "Text" => new Text(null, "hi"),
        "LinePolygon" => new LinePolygon(null),
        "LineGrid" => new LineGrid(null),
        _ => throw new ArgumentException(name),
    };

    private static void Assign(IPositionedSizedObject target, string variable, float value)
    {
        switch (variable)
        {
            case "X": target.X = value; break;
            case "Y": target.Y = value; break;
            case "Width": target.Width = value; break;
            case "Height": target.Height = value; break;
            case "Rotation": target.Rotation = value; break;
        }
    }

    private static float Read(IPositionedSizedObject target, string variable) => variable switch
    {
        "X" => target.X,
        "Y" => target.Y,
        "Width" => target.Width,
        "Height" => target.Height,
        _ => target.Rotation,
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void NonFiniteValue_IsRejectedOnlyWithDiagnostics(string renderable, string variable, float value)
    {
        IPositionedSizedObject target = Create(renderable);
        float before = Read(target, variable);

#if FULL_DIAGNOSTICS
        Should.Throw<ArgumentException>(() => Assign(target, variable, value));
        BitConverter.SingleToInt32Bits(Read(target, variable)).ShouldBe(BitConverter.SingleToInt32Bits(before),
            "because a rejected value is not stored");
#else
        Should.NotThrow(() => Assign(target, variable, value));
#endif
    }

    [Fact]
    public void FiniteValues_AreStoredUnchanged()
    {
        Random random = new Random(6477);
        foreach (string renderable in Renderables)
        {
            IPositionedSizedObject target = Create(renderable);
            foreach (string variable in Variables)
            {
                // A circle's Width and Height are the same radius, and a line grid and a polygon derive their size.
                bool derivedSize = variable == "Width" || variable == "Height";
                if (renderable == "LineCircle" && variable == "Height" || (renderable == "LineGrid" || renderable == "LinePolygon") && derivedSize || renderable == "LineGrid" && variable == "Rotation")
                {
                    continue;
                }

                for (int i = 0; i < 6; i++)
                {
                    float value = (float)(random.NextDouble() * 400 - 200);
                    Assign(target, variable, value);
                    Read(target, variable).ShouldBe(value, $"{renderable}.{variable}");
                }
            }
        }
    }
}
