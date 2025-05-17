using System;

#if FRB
namespace FlatRedBall.Forms.Data;
#else
namespace MonoGameGum.Forms.Data;
#endif

public abstract class BindingExpressionBase : IDisposable
{
    public BindingMode Mode { get; protected set; }
    public virtual void UpdateSource() { }
    public virtual void UpdateTarget() { }

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}