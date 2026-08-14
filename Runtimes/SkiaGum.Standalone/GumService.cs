// Gum.GumService is the concrete, instantiable Skia-family service -- the type every SkiaGum-based
// host (WPF, MAUI, bring-your-own-canvas) actually types. All Skia rendering/init/update logic
// lives in the shared GumServiceSkiaBase (issue #4452 phase 1); this class exists to carry the
// public GumService name and a Default singleton scoped to this assembly. Each Skia-family host
// assembly (this project, and previously each of SkiaGum.Wpf/SkiaGum.Maui before they compiled
// this type via ProjectReference instead of file-linking it) gets its own Gum.GumService with its
// own Default -- never two in one app, mirroring how MonoGame/Raylib/SilkNet each keep their own.
// EXPERIMENTAL (#4452 phase 2 spike): moved out of SkiaGum.csproj into its own project so that
// project can be referenced as a pure base (GumServiceSkiaBase + shared render source) by hosts
// that need their own differently-behaved concrete GumService, without a Gum.GumService naming
// collision -- see Runtimes/SilkNetGum/GumService.Silk.cs.
namespace Gum;

public class GumService : GumServiceSkiaBase
{
    static GumService? _default;

    /// <summary>
    /// The singleton service instance.
    /// </summary>
    public static GumService Default => _default ??= new GumService();
}
