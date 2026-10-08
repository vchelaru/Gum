namespace Gum.ProjectServices;

/// <summary>
/// The runtime a scaffolded host game project targets.
/// </summary>
public enum HostPlatform
{
    /// <summary>MonoGame DesktopGL, referencing the <c>Gum.MonoGame</c> package.</summary>
    MonoGame,

    /// <summary>KNI DesktopGL, referencing the <c>Gum.KNI</c> package.</summary>
    Kni,

    /// <summary>raylib-cs, referencing the <c>Gum.raylib</c> package.</summary>
    Raylib,

    /// <summary>Stride 3D, referencing the <c>Gum.Stride</c> package.</summary>
    Stride
}
