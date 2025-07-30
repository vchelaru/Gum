using System.Diagnostics;

#if FRB
namespace FlatRedBall.Forms.Data;
#endif

#if !FRB
namespace Gum.Forms.Data;

#endif

public class GumProperty
{
    [DebuggerDisplay("UnsetValue")]
    public static readonly object UnsetValue = new();
}
