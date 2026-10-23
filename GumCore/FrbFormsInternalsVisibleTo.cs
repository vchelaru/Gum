using System.Runtime.CompilerServices;

// FlatRedBall.Forms compiles Gum's Forms controls from source against this GumCore build, and
// those controls call GumCore internals such as TextHitTesting. GumCommon grants the same access
// to the Gum runtimes in GumCommon/AssemblyInfo.cs. Every new FlatRedBall.Forms head must be added to this list.
[assembly: InternalsVisibleTo("FlatRedBall.Forms.DesktopGlNet6")]
[assembly: InternalsVisibleTo("FlatRedBall.Forms.FNA")]
[assembly: InternalsVisibleTo("FlatRedBall.Forms.Kni.DesktopGL")]
[assembly: InternalsVisibleTo("FlatRedBall.Forms.Kni.Web")]
[assembly: InternalsVisibleTo("FlatRedBall.Forms.AndroidMonoGame")]
[assembly: InternalsVisibleTo("FlatRedBall.Forms.iOSMonoGame")]
