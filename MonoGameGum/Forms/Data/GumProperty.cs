using System.Diagnostics;

#if FRB
namespace FlatRedBall.Forms.Data;
#elif RAYLIB
using Gum.Forms.Data;
#else
namespace MonoGameGum.Forms.Data;
#endif

public class GumProperty
{
    [DebuggerDisplay("UnsetValue")]
    public static readonly object UnsetValue = new();
}
