using Gum.Renderables;
using RenderingLibrary;
using Shouldly;

namespace RaylibGum.Tests.Renderables;

/// <summary>
/// Every Raylib renderable rejects NaN and infinity on X, Y, Width, Height and Rotation with an ArgumentException,
/// as the MonoGame and Skia renderables do (#6477). RaylibGum defines FULL_DIAGNOSTICS in Debug and Release.
/// </summary>
public class RenderableSizeNonFiniteTests
{
    private static readonly string[] Variables = { "X", "Y", "Width", "Height", "Rotation" };
    private static readonly float[] NonFinite = { float.PositiveInfinity, float.NegativeInfinity, float.NaN };

    private static readonly string[] Renderables =
    {
        "SolidRectangle", "Sprite", "NineSlice", "LineRectangle", "LineCircle", "LinePolygon", "LineArc", "Text",
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
        "SolidRectangle" => new SolidRectangle(),
        "Sprite" => new Sprite(),
        "NineSlice" => new NineSlice(),
        "LineRectangle" => new LineRectangle(),
        "LineCircle" => new LineCircle(),
        "LinePolygon" => new LinePolygon(),
        "LineArc" => new LineArc(),
        "Text" => new Text(),
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
    public void NonFiniteValue_IsRejected(string renderable, string variable, float value)
    {
        IPositionedSizedObject target = Create(renderable);
        float before = Read(target, variable);

        Should.Throw<ArgumentException>(() => Assign(target, variable, value));
        BitConverter.SingleToInt32Bits(Read(target, variable)).ShouldBe(BitConverter.SingleToInt32Bits(before),
            "because a rejected value is not stored");
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
                if (renderable == "LineCircle" && variable == "Height")
                {
                    continue; // a circle's Width and Height are the same radius
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
