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