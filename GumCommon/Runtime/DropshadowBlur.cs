namespace Gum.Wireframe;

/// <summary>
/// Maps a dropshadow's <c>DropshadowBlur</c> to the standard deviation (sigma) of the Gaussian that
/// the Gaussian-based runtimes (Skia and raylib) blur the shadow with, so both draw the same shadow.
/// </summary>
/// <remarks>
/// A blur of N must extend N pixels, measured outward from the shape edge to where the shadow's alpha
/// falls below 0.5% (a Gaussian tail never reaches zero). Measured on filled circles and rectangles
/// at blur 8, 16, 32 and 56, a sigma of 0.4 times the blur reaches between 0.9 and 1.1 times the
/// blur in both runtimes.
/// </remarks>
public static class DropshadowBlur
{
    /// <summary>The Gaussian sigma as a fraction of the blur value.</summary>
    public const float SigmaPerBlur = 0.4f;

    /// <summary>Returns the Gaussian sigma, in pixels, for a dropshadow blur in pixels.</summary>
    public static float GetSigma(float blur) => blur * SigmaPerBlur;
}
