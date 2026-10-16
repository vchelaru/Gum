#nullable enable
using Gum.Managers;
using RenderingLibrary;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Gum.Wireframe;
using ToolsUtilities;

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
/// Statics that became per-context read <see cref="Current"/>: the context last activated with
/// <see cref="Activate()"/>, else the first live context (the "home" context), else none, in which case
/// they fall back to the old process-wide object. Code that runs no service (the Gum tool, FlatRedBall,
/// most unit tests) never has a context and sees no change.
/// </para>
/// </remarks>
public sealed partial class GumContext
{
    // A plain static on purpose: all UI work for all services runs on one thread.
    private static GumContext? _active;

    private static readonly List<GumContext> _live = new List<GumContext>();
    private static readonly object _liveLock = new object();

    // The first live context, kept in a field so reading Current takes no lock.
    private static volatile GumContext? _home;

    static GumContext()
    {
        // FileManager sits below this assembly, so it exposes hooks instead of knowing about contexts.
        FileManager.RelativeDirectoryReader = ReadRelativeDirectory;
        FileManager.RelativeDirectoryWriter = WriteRelativeDirectory;
    }

    // Per-service registrations of element name -> runtime type, layered over the process-wide base layer.
    internal ElementTypeRegistry ElementTypes { get; } = new ElementTypeRegistry();

    // This service's project lookup. Null for the context that adopts the process-wide ObjectFinder.
    internal ObjectFinder? ObjectFinder { get; set; }

    private readonly Dictionary<Type, object> _state = new Dictionary<Type, object>();

    /// <summary>
    /// The current context when it keeps its own state, or null when the process-wide statics apply
    /// (no context is live, or the current one is the one that adopted them).
    /// </summary>
    public static GumContext? Own
    {
        get
        {
            GumContext? context = Current;
            return context != null && !context.AdoptsProcessStatics ? context : null;
        }
    }

    /// <summary>
    /// A per-service value keyed by its type, for state whose type lives in a runtime assembly this one
    /// cannot reference (Styling, the default fonts). Null when unset.
    /// </summary>
    public T? GetState<T>() where T : class
    {
        return _state.TryGetValue(typeof(T), out object? value) ? (T)value : null;
    }

    /// <summary>Stores, or with null removes, the per-service value for <typeparamref name="T"/>.</summary>
    public void SetState<T>(T? value) where T : class
    {
        if (value == null)
        {
            _state.Remove(typeof(T));
        }
        else
        {
            _state[typeof(T)] = value;
        }
    }

    // This service's value of ISystemManagers.Default and of the XNA-like SystemManagers.Default. Null until
    // set; the adopting context uses the statics those properties always had instead.
    internal ISystemManagers? Managers { get; set; }

    internal object? ConcreteManagers { get; set; }

    // This service's Renderer.TextureFilter (a TextureFilter value); null until set.
    internal int? TextureFilterSetting { get; set; }

    // This service's FileManager.RelativeDirectory. Null until set, and unused by the adopting context,
    // which keeps using the FileManager static.
    internal string? RelativeDirectory { get; set; }

    private static string? ReadRelativeDirectory()
    {
        GumContext? context = Current;
        return context == null || context.AdoptsProcessStatics ? null : context.RelativeDirectory;
    }

    private static bool WriteRelativeDirectory(string value)
    {
        GumContext? context = Current;
        if (context == null || context.AdoptsProcessStatics)
        {
            return false;
        }
        context.RelativeDirectory = value;
        return true;
    }

    /// <summary>
    /// The context most recently activated, else the first live context, else null.
    /// </summary>
    public static GumContext? Current => _active ?? Home;

    /// <summary>
    /// The context that went live first and is still live. Code that has no activation scope resolves
    /// to it, which is what keeps a lone service behaving like the old statics.
    /// </summary>
    public static GumContext? Home => _home;

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

    // Which service built an element that is not (yet) attached to any managers. Only the outermost element of
    // a build is stamped; OwnerOf walks up to it.
    private static readonly ConditionalWeakTable<GraphicalUiElement, GumContext> _builtBy =
        new ConditionalWeakTable<GraphicalUiElement, GumContext>();

    internal static void StampBuiltBy(GraphicalUiElement element, GumContext context)
    {
        _builtBy.AddOrUpdate(element, context);
    }

    /// <summary>
    /// The context an element belongs to: that of the managers it is attached to, else that of the service
    /// that built it (itself or an ancestor), else null.
    /// </summary>
    public static GumContext? OwnerOf(GraphicalUiElement element)
    {
        GumContext? attached = element.EffectiveManagers?.Context;
        if (attached != null)
        {
            return attached;
        }

        for (GraphicalUiElement? current = element; current != null; current = current.EffectiveParentGue as GraphicalUiElement)
        {
            if (_builtBy.TryGetValue(current, out GumContext? builder))
            {
                return builder;
            }
        }
        return null;
    }

    /// <summary>
    /// The context explicitly activated, ignoring the home fallback. Registrations
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

    /// <summary>
    /// Contexts are created and made live by the runtime's service (<c>GumService.Initialize</c>), not by
    /// user code.
    /// </summary>
    internal GumContext()
    {
    }

    /// <summary>Makes this context live. The first live context adopts the process statics.</summary>
    internal void Register()
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
            _home = _live[0];
        }
    }

    /// <summary>Removes this context from the live set. Safe to call twice.</summary>
    internal void Release()
    {
        lock (_liveLock)
        {
            if (IsLive)
            {
                _live.Remove(this);
                IsLive = false;
                _home = _live.Count > 0 ? _live[0] : null;
            }
        }
    }

    /// <summary>
    /// Makes this the current context until the returned scope is disposed, which restores the previous
    /// one. Use it with <c>using</c>.
    /// </summary>
    /// <remarks>
    /// <b>The active context is one plain static, shared by every thread.</b> All UI work for all services
    /// is expected to run on one thread (the game's). Building or touching a service's UI from a
    /// background thread is unsupported unless that work runs on the UI thread, and a scope opened on one
    /// thread is visible to every other thread while it lasts. A scope does persist across an
    /// <c>await</c> that resumes on the same synchronization context.
    /// </remarks>
    public ActivationScope Activate() => new ActivationScope(this);

    /// <summary>
    /// Activates <paramref name="context"/>, or does nothing when it is null (an object that does not
    /// belong to a service).
    /// </summary>
    public static ActivationScope Activate(GumContext? context) => new ActivationScope(context);

    /// <summary>
    /// Restores the previously active context when disposed. Scopes must be disposed in reverse order of
    /// creation; see the threading remarks on <see cref="Activate()"/>.
    /// </summary>
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
