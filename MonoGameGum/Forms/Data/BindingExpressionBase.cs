using System;
using System.Collections.Generic;

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

    private HashSet<(string Kind, Type? SourceType)>? _reportedProblems;

    /// <summary>
    /// Returns true the first time this binding meets a problem of this kind on a source of this type. A report is
    /// kept per source type, so one spent on a context the target only inherited for a moment (the parent's view
    /// model, before the target gets its own) does not hide the same problem on the view model the binding is for.
    /// </summary>
    private protected bool ShouldReport(string kind, Type? sourceType) =>
        (_reportedProblems ??= new HashSet<(string Kind, Type? SourceType)>()).Add((kind, sourceType));

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}