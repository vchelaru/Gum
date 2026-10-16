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
/// <b>There is one "current service" pointer: <c>IGumService.Default</c>.</b> Statics that became per-context
/// read <see cref="Current"/>, which is the context of <c>IGumService.Default</c>. Assigning
/// <c>IGumService.Default</c> switches every one of them (and <c>SystemManagers.Default</c> /
/// <c>ISystemManagers.Default</c>) to that service. <c>Update</c>, <c>Draw</c> and the other framework entry
/// points make their service the default for the length of the call and put the previous default back, so
/// handlers fired inside see their own service. <c>Initialize</c> runs as the service being initialized and
/// makes it the default only when no default exists. Code that touches another service from inside an entry
/// point assigns <c>IGumService.Default</c> for it, and again for the first service when it comes back.
/// </para>
/// <para>
/// With no default assigned, <see cref="Current"/> is the only live service, or none. With two or more live
/// services and no default it throws, because silently picking one would hand out the wrong service's state.
/// Code that runs no service (the Gum tool, FlatRedBall, most unit tests) never has a context and sees no
/// change.
/// </para>
/// <para>
/// All UI work for all services is expected to run on one thread.
/// </para>
/// </remarks>
internal sealed partial class GumContext
{
    private static readonly List<GumContext> _live = new List<GumContext>();
    private static readonly object _liveLock = new object();
    private static int _nextId;

    // Kept in fields so the hot reads (Current, MultipleLive) take no lock.
    private static volatile int _liveCount;
    private static volatile GumContext? _onlyLive;

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
    /// Identifies the context in error messages.
    /// </summary>
    public int Id { get; } = System.Threading.Interlocked.Increment(ref _nextId);

    /// <summary>The runtime type name of the owning service, set by the service. Used in error messages.</summary>
    internal string ServiceTypeName { get; set; } = "service";

    /// <summary>Describes the service this context belongs to, for example <c>GumService #2</c>.</summary>
    public string Describe() => ServiceTypeName + " #" + Id;

    /// <inheritdoc/>
    public override string ToString() => Describe();

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
    /// The context of <c>IGumService.Default</c>. With no default assigned it is the only live context, or
    /// null when none is live. Throws when two or more services are live and no default is assigned.
    /// </summary>
    /// <exception cref="InvalidOperationException">Two or more services are live and
    /// <c>IGumService.Default</c> is null.</exception>
    public static GumContext? Current
    {
        get
        {
#if FRB
            return null;
#else
            IGumService? service = IGumService.Default;
            if (service != null)
            {
                return service.Context;
            }

            if (_liveCount < 2)
            {
                return _onlyLive;
            }

            throw new InvalidOperationException(
                "Two or more Gum services are initialized but IGumService.Default is not assigned, so there is no " +
                "way to tell which service this code is for. Assign IGumService.Default (or GumService.Default) to the " +
                "service you want before building or using UI.");
#endif
        }
    }

    /// <summary>True when two or more services are live. Ownership checks only run then.</summary>
    public static bool MultipleLive => _liveCount >= 2;

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

    // When true, element-type registrations go to the process-wide base layer even though a service is
    // current. Set while the optional-package reflection hooks run in Initialize, which must be seen by
    // every service.
    internal static bool RegisteringShared { get; set; }

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

    // With two or more live services, remembers which service a newly built element belongs to, so adding
    // it to another service's tree can be caught. A no-op otherwise.
    internal static void StampIfMultipleLive(GraphicalUiElement element)
    {
        if (_liveCount >= 2)
        {
            GumContext? current = Current;
            if (current != null)
            {
                _builtBy.AddOrUpdate(element, current);
            }
        }
    }

    // Throws when two or more services are live and the child and its new parent belong to different ones.
    // Elements with no known owner are not checked.
    internal static void CheckSameService(GraphicalUiElement child, GraphicalUiElement parent)
    {
        GumContext? childOwner = OwnerOf(child);
        if (childOwner == null)
        {
            return;
        }
        GumContext? parentOwner = OwnerOf(parent);
        if (parentOwner == null || ReferenceEquals(childOwner, parentOwner))
        {
            return;
        }

        string name = string.IsNullOrEmpty(child.Name) ? child.GetType().Name : child.Name;
        throw new InvalidOperationException(
            $"'{name}' was built under {childOwner.Describe()} but is being added under {parentOwner.Describe()}. " +
            $"Assign IGumService.Default = {parentOwner.Describe()} before constructing it, or add it to {childOwner.Describe()}'s tree.");
    }

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
            UpdateLiveFields();
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
                UpdateLiveFields();
            }
        }
    }

#if !FRB
    // The service this context belongs to; set by the service when it initializes.
    internal IGumService? Service { get; set; }
#endif

    // Framework entry points (Update, Draw, Renderer.Draw, building an element for a service) make that
    // service the default for the length of the call, so handlers and events fired inside it see their own
    // service, then put the previous default back. Framework code only: users assign the default
    // themselves and never write a using. A null context, or one without a service, changes nothing.
    internal static DefaultScope EnterDefault(GumContext? context)
    {
#if !FRB
        IGumService? service = context?.Service;
        if (service != null)
        {
            IGumService? previous = IGumService.Default;
            if (!ReferenceEquals(previous, service))
            {
                IGumService.Default = service;
            }
            return new DefaultScope(previous, restore: true);
        }
#endif
        return default;
    }

    // Puts the previous default back when disposed. Skips the restore when that service was uninitialized
    // while the call ran, so a dead service never becomes the default again.
    internal readonly struct DefaultScope : IDisposable
    {
#if !FRB
        private readonly IGumService? _previous;
        private readonly bool _restore;

        internal DefaultScope(IGumService? previous, bool restore)
        {
            _previous = previous;
            _restore = restore;
        }

        public void Dispose()
        {
            if (_restore && (_previous == null || _previous.IsInitialized))
            {
                IGumService.Default = _previous;
            }
        }
#else
        public void Dispose()
        {
        }
#endif
    }

    private static void UpdateLiveFields()
    {
        _liveCount = _live.Count;
        _onlyLive = _live.Count == 1 ? _live[0] : null;
    }
}
