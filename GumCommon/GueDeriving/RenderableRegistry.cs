using Gum.Wireframe;
using System;
using System.Collections.Generic;

namespace Gum.GueDeriving;

/// <summary>
/// Registration point that lets optional packages — or consumers — supply renderable
/// implementations to core <see cref="Gum.Wireframe.GraphicalUiElement"/>-derived runtimes
/// without core MonoGameGum referencing those packages.
/// </summary>
/// <remarks>
/// <para>
/// Mirrors the existing optional-package extension pattern used by
/// <c>ElementSaveExtensions.RegisterGueInstantiation</c>,
/// <c>StandardElementsManager.CustomGetDefaultState</c>, and
/// <c>CustomSetPropertyOnRenderable.AdditionalPropertyOnRenderable</c>: core defines the
/// hook, an optional package (or test setup) fills it. Capabilities are keyed by type —
/// typically a role or capability interface such as <c>IFilledCircleRenderable</c>.
/// </para>
/// <para>
/// Two registration semantics are supported, layered by priority. The general factory
/// (<see cref="RegisterFactory{T}(Func{T})"/>) is the active layer; the default factory
/// (<see cref="RegisterDefaultFactory{T}(Func{T})"/>) is the fallback. <see cref="GetFactory{T}"/>
/// returns the active factory if one is registered, otherwise the default, otherwise null.
/// </para>
/// <para>
/// Each layer has both a context-free overload (<c>Func&lt;T&gt;</c>) and a context-bearing
/// overload (<c>Func&lt;GraphicalUiElement, T&gt;</c>). The context-bearing variant lets a
/// factory wire optional-package <c>internal</c> hooks onto the freshly built renderable
/// using the caller GUE — see Apos.Shapes' <c>OnPreRender</c> wiring on <c>Circle</c>. The
/// two buckets are independent: registering one does not clear the other, and resolution
/// order in <see cref="Create{T}(GraphicalUiElement)"/> is active-context-bearing →
/// active-context-free → default-context-bearing → default-context-free. Context-free
/// <see cref="Create{T}()"/> only resolves context-free factories — there is no safe
/// default <see cref="GraphicalUiElement"/> to auto-pass.
/// </para>
/// <para>
/// Use the active layer alone (no default) for capabilities core cannot construct itself —
/// the optional package registers a factory, callers handle null as graceful degradation.
/// Use both layers for capabilities core ships a default implementation of that a consumer
/// may choose to replace.
/// </para>
/// </remarks>
public static class RenderableRegistry
{
    private static readonly Dictionary<Type, Delegate> _factories = new();
    private static readonly Dictionary<Type, Delegate> _defaultFactories = new();
    private static readonly Dictionary<Type, Delegate> _contextFactories = new();
    private static readonly Dictionary<Type, Delegate> _defaultContextFactories = new();
    private static readonly object _sync = new();

    /// <summary>
    /// Register the active factory for capability <typeparamref name="T"/>. Replaces any
    /// previously registered active factory for the same capability. Used either as the
    /// sole registration (factory-or-nothing — no default) or to override a registered
    /// default (default-plus-override).
    /// </summary>
    public static void RegisterFactory<T>(Func<T> factory) where T : class
    {
        if (factory == null)
        {
            throw new ArgumentNullException(nameof(factory));
        }
        lock (_sync)
        {
            _factories[typeof(T)] = factory;
        }
    }

    /// <summary>
    /// Register a context-bearing active factory for capability <typeparamref name="T"/>.
    /// The factory receives the calling <see cref="GraphicalUiElement"/>, so it can wire
    /// optional-package internal hooks (e.g. Apos.Shapes' <c>OnPreRender</c>) that core
    /// cannot reach. Stored in a separate bucket from the context-free overload — they do
    /// not overwrite each other.
    /// </summary>
    public static void RegisterFactory<T>(Func<GraphicalUiElement, T> factory) where T : class
    {
        if (factory == null)
        {
            throw new ArgumentNullException(nameof(factory));
        }
        lock (_sync)
        {
            _contextFactories[typeof(T)] = factory;
        }
    }

    /// <summary>
    /// Register the fallback factory for capability <typeparamref name="T"/>. Used when core
    /// ships a default implementation that a consumer may optionally replace via
    /// <see cref="RegisterFactory{T}(Func{T})"/>. Replaces any previously registered default.
    /// </summary>
    public static void RegisterDefaultFactory<T>(Func<T> factory) where T : class
    {
        if (factory == null)
        {
            throw new ArgumentNullException(nameof(factory));
        }
        lock (_sync)
        {
            _defaultFactories[typeof(T)] = factory;
        }
    }

    /// <summary>
    /// Register a context-bearing fallback factory for capability <typeparamref name="T"/>.
    /// Stored in a separate bucket from the context-free default — they do not overwrite
    /// each other.
    /// </summary>
    public static void RegisterDefaultFactory<T>(Func<GraphicalUiElement, T> factory) where T : class
    {
        if (factory == null)
        {
            throw new ArgumentNullException(nameof(factory));
        }
        lock (_sync)
        {
            _defaultContextFactories[typeof(T)] = factory;
        }
    }

    /// <summary>
    /// Remove the active factory (if any) for capability <typeparamref name="T"/> — both
    /// the context-free and context-bearing variants. The resolved factory falls back to
    /// the corresponding default, or null if none was registered.
    /// </summary>
    public static void ClearFactory<T>() where T : class
    {
        lock (_sync)
        {
            _factories.Remove(typeof(T));
            _contextFactories.Remove(typeof(T));
        }
    }

    /// <summary>
    /// Return the resolved context-free factory for capability <typeparamref name="T"/>:
    /// the active factory if registered, otherwise the default, otherwise null. Callers
    /// treat null as "capability not available." Does NOT consider context-bearing
    /// factories — see <see cref="GetContextFactory{T}"/>.
    /// </summary>
    public static Func<T>? GetFactory<T>() where T : class
    {
        lock (_sync)
        {
            if (_factories.TryGetValue(typeof(T), out Delegate? active))
            {
                return (Func<T>)active;
            }
            if (_defaultFactories.TryGetValue(typeof(T), out Delegate? fallback))
            {
                return (Func<T>)fallback;
            }
            return null;
        }
    }

    /// <summary>
    /// Return the resolved context-bearing factory for capability <typeparamref name="T"/>:
    /// the active context-bearing factory if registered, otherwise the default, otherwise
    /// null. Does NOT consider context-free factories — see <see cref="GetFactory{T}"/>.
    /// </summary>
    public static Func<GraphicalUiElement, T>? GetContextFactory<T>() where T : class
    {
        lock (_sync)
        {
            if (_contextFactories.TryGetValue(typeof(T), out Delegate? active))
            {
                return (Func<GraphicalUiElement, T>)active;
            }
            if (_defaultContextFactories.TryGetValue(typeof(T), out Delegate? fallback))
            {
                return (Func<GraphicalUiElement, T>)fallback;
            }
            return null;
        }
    }

    /// <summary>
    /// Convenience: invoke the resolved context-free factory for capability
    /// <typeparamref name="T"/>, or return null when no context-free factory is registered.
    /// Does NOT auto-invoke a context-bearing factory — callers that registered one must use
    /// <see cref="Create{T}(GraphicalUiElement)"/>.
    /// </summary>
    public static T? Create<T>() where T : class
    {
        Func<T>? factory = GetFactory<T>();
        return factory?.Invoke();
    }

    /// <summary>
    /// Convenience: invoke the resolved factory for capability <typeparamref name="T"/>,
    /// passing <paramref name="context"/> to a context-bearing factory if one is registered.
    /// Resolution order: active context-bearing → active context-free → default context-
    /// bearing → default context-free → null.
    /// </summary>
    public static T? Create<T>(GraphicalUiElement context) where T : class
    {
        lock (_sync)
        {
            Type key = typeof(T);
            if (_contextFactories.TryGetValue(key, out Delegate? activeContext))
            {
                return ((Func<GraphicalUiElement, T>)activeContext).Invoke(context);
            }
            if (_factories.TryGetValue(key, out Delegate? activeFree))
            {
                return ((Func<T>)activeFree).Invoke();
            }
            if (_defaultContextFactories.TryGetValue(key, out Delegate? defaultContext))
            {
                return ((Func<GraphicalUiElement, T>)defaultContext).Invoke(context);
            }
            if (_defaultFactories.TryGetValue(key, out Delegate? defaultFree))
            {
                return ((Func<T>)defaultFree).Invoke();
            }
            return null;
        }
    }

    /// <summary>
    /// Remove all registered factories (active and default, context-free and context-
    /// bearing). Intended for test isolation; production code should not call this.
    /// </summary>
    public static void Reset()
    {
        lock (_sync)
        {
            _factories.Clear();
            _defaultFactories.Clear();
            _contextFactories.Clear();
            _defaultContextFactories.Clear();
        }
    }
}
