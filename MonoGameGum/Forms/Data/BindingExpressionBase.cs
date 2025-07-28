using System;

#if FRB
namespace FlatRedBall.Forms.Data;
#elif RAYLIB
namespace Gum.Forms.Data;
#else
namespace MonoGameGum.Forms.Data;
#endif

public abstract class BindingExpressionBase : IDisposable
{
    public virtual void UpdateSource() { }
    public virtual void UpdateTarget() { }

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}