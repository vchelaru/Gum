using System.Collections.Generic;

namespace MonoGameGum.TestsCommon;

/// <summary>
/// One expected table for the built-in BBCode effects, linked into the MonoGame, raylib and Skia test projects so
/// each runtime is checked against the same numbers: the first letter of a white text (x = 0, stripped index 0) at a
/// fixed time, its offset from where it draws without the effect, and its color.
/// </summary>
public static class BbCodeEffectParityCases
{
    // Open tag, close tag, time, expected x offset, expected y offset, expected red, green, blue, alpha.
    public static IEnumerable<object[]> All => new[]
    {
        // sin(freq * t) * amp / 10.
        new object[] { "[Wave amp=100 freq=5]", "[/Wave]", 0.1, 0f, 4.794f, 255, 255, 255, 255 },
        new object[] { "[Wave amp=100 freq=5]", "[/Wave]", 0.3, 0f, 9.975f, 255, 255, 255, 255 },
        // (sin(freq * t), cos(freq * t)) * radius.
        new object[] { "[Tornado radius=20 freq=1]", "[/Tornado]", 0.0, 0f, 20f, 255, 255, 255, 255 },
        new object[] { "[Tornado radius=20 freq=1]", "[/Tornado]", 1.5707963, 20f, 0f, 255, 255, 255, 255 },
        // The deterministic shake angle for letter 0 (pinned from the shared math), after the half period it eases over.
        new object[] { "[Shake level=50 rate=10]", "[/Shake]", 0.06, ShakeX06, ShakeY06, 255, 255, 255, 255 },
        new object[] { "[Shake level=50 rate=10]", "[/Shake]", 0.36, ShakeX36, ShakeY36, 255, 255, 255, 255 },
        // 1 - (index - start) / length = 1 - (0 + 1) / 2.
        new object[] { "[Fade start=-1 length=2]", "[/Fade]", 0.0, 0f, 0f, 255, 255, 255, 128 },
        new object[] { "[Fade start=-1 length=2]", "[/Fade]", 5.0, 0f, 0f, 255, 255, 255, 128 },
        // from_ok_hsv(freq * |t * speed + x / 50|, 0.8, 0.8), pinned from the Okhsv port of ok_color.h.
        new object[] { "[Rainbow]", "[/Rainbow]", 0.0, 0f, 0f, Rainbow0R, Rainbow0G, Rainbow0B, 255 },
        new object[] { "[Rainbow]", "[/Rainbow]", 0.5, 0f, 0f, Rainbow5R, Rainbow5G, Rainbow5B, 255 },
        // White toward white at 25% alpha by ease(pingpong(t, 1), -2).
        new object[] { "[Pulse]", "[/Pulse]", 0.5, 0f, 0f, 255, 255, 255, 160 },
        new object[] { "[Pulse]", "[/Pulse]", 1.0, 0f, 0f, 255, 255, 255, 64 },
    };

    const float ShakeX06 = 0f;
    const float ShakeY06 = 5f;
    const float ShakeX36 = -4.2804165f;
    const float ShakeY36 = 2.5841894f;
    const int Rainbow0R = 201;
    const int Rainbow0G = 66;
    const int Rainbow0B = 119;
    const int Rainbow5R = 59;
    const int Rainbow5G = 199;
    const int Rainbow5B = 177;
}
