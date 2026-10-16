#nullable enable
using System;
using System.Collections.Generic;

namespace Gum;

/// <summary>
/// The state one running Gum host (one <c>GumService</c>) owns, so several hosts can share a process.
/// Prototype for #6066.
/// </summary>
/// <remarks>
/// <para>
/// A context is made live by its service's <c>Initialize</c> and released by <c>Uninitialize</c>. The
/// first context to go live <see cref="AdoptsProcessStatics"/>: it uses the objects the pre-existing statics
/// always held (for example the single <c>LoaderManager</c>), so one service behaves exactly as before.
/// Later contexts get their own instances.
/// </para>
/// <para>
/// Statics that became per-context read <see cref="Current"/>: the context a thread activated with
/// <see cref="Activate()"/>, else the first live context (the "home" context), else none, in which case
/// they fall back to the old process-wide object. Code that runs no service (the Gum tool, FlatRedBall,
/// most unit tests) never has a context and sees no change.
/// </para>
/// </remarks>
public sealed partial class GumContext
{
    [ThreadStatic]
    private static GumContext? _active;

    private static readonly List<GumContext> _live = new List<GumContext>();
    private static readonly object _liveLock = new object();

    // Per-service registrations of element name -> runtime type, layered over the process-wide base layer.
    internal ElementTypeRegistry ElementTypes { get; } = new ElementTypeRegistry();

    /// <summary>
    /// The context the calling thread activated, else the first live context, else null.
    /// </summary>
    public static GumContext? Current => _active ?? Home;

    /// <summary>
    /// The context that went live first and is still live. Code that has no activation scope resolves
    /// to it, which is what keeps a lone service behaving like the old statics.
    /// </summary>
    public static GumContext? Home
    {
        get
        {
            lock (_liveLock)
            {
                return _live.Count > 0 ? _live[0] : null;
            }
        }
    }

    /// <summary>
    /// True when a live context other than <paramref name="context"/> exists. A service tears down state
    /// shared by all services only when this is false.
    /// </summary>
    public static bool HasLiveContextOtherThan(GumContext? context)
    {
        lock (_liveLock)
        {
            foreach (GumContext live in _live)
            {
                if (!ReferenceEquals(live, context))
                {
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// The context the calling thread explicitly activated, ignoring the home fallback. Registrations
    /// made outside any scope go to the process-wide base layer instead of a service.
    /// </summary>
    internal static GumContext? Active => _active;

    /// <summary>True once <see cref="Register"/> ran and <see cref="Release"/> has not.</summary>
    public bool IsLive { get; private set; }

    /// <summary>
    /// True when this context was the first to go live, so it owns the objects the old statics held
    /// instead of creating its own.
    /// </summary>
    public bool AdoptsProcessStatics { get; private set; }

    /// <summary>Makes this context live. The first live context adopts the process statics.</summary>
    public void Register()
    {
        lock (_liveLock)
        {
            if (IsLive)
            {
                return;
            }
            AdoptsProcessStatics = _live.Count == 0;
            _live.Add(this);
            IsLive = true;
        }
    }

    /// <summary>Removes this context from the live set. Safe to call twice.</summary>
    public void Release()
    {
        lock (_liveLock)
        {
            if (IsLive)
            {
                _live.Remove(this);
                IsLive = false;
            }
        }
    }

    /// <summary>
    /// Makes this the calling thread's current context until the returned scope is disposed, which
    /// restores the previous one. Use it with <c>using</c>.
    /// </summary>
    public ActivationScope Activate() => new ActivationScope(this);

    /// <summary>
    /// Activates <paramref name="context"/>, or does nothing when it is null (an object that does not
    /// belong to a service).
    /// </summary>
    public static ActivationScope Activate(GumContext? context) => new ActivationScope(context);

    /// <summary>Restores the previously active context when disposed.</summary>
    public readonly struct ActivationScope : IDisposable
    {
        private readonly GumContext? _previous;
        private readonly bool _changed;

        internal ActivationScope(GumContext? context)
        {
            if (context == null)
            {
                _previous = null;
                _changed = false;
            }
            else
            {
                _previous = _active;
                _changed = true;
                _active = context;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_changed)
            {
                _active = _previous;
            }
        }
    }
}
