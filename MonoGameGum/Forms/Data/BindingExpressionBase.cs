using System;

#if FRB
namespace FlatRedBall.Forms.Data;
#endif

#if !FRB
namespace Gum.Forms.Data;
#endif

public abstract class BindingExpressionBase : IDisposable
{
    public virtual void UpdateSource() { }
    public virtual void UpdateTarget() { }

    /// <summary>
    /// Stops listening to the source without touching the target, for a target whose subtree left the root.
    /// </summary>
    public virtual void Pause() { }

    /// <summary>Reattaches to the target's current context and refreshes the target, undoing <see cref="Pause"/>.</summary>
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Re-resolves the binding path on the current context's type.")]
    public virtual void Resume() { }

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}