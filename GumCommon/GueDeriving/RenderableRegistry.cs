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
/// typically a small capability interface such as <c>IFilledShapeRenderable</c>.
/// </para>
/// <para>
/// Two registration semantics are supported, layered by priority. The general factory
/// (<see cref="RegisterFactory{T}"/>) is the active layer; the default factory
/// (<see cref="RegisterDefaultFactory{T}"/>) is the fallback. <see cref="GetFactory{T}"/>
/// returns the active factory if one is registered, otherwise the default, otherwise null.
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
    /// Register the fallback factory for capability <typeparamref name="T"/>. Used when core
    /// ships a default implementation that a consumer may optionally replace via
    /// <see cref="RegisterFactory{T}"/>. Replaces any previously registered default.
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
    /// Remove the active factory (if any) for capability <typeparamref name="T"/>. The
    /// resolved factory falls back to the default, or null if none was registered.
    /// </summary>
    public static void ClearFactory<T>() where T : class
    {
        lock (_sync)
        {
            _factories.Remove(typeof(T));
        }
    }

    /// <summary>
    /// Return the resolved factory for capability <typeparamref name="T"/>: the active
    /// factory if registered, otherwise the default, otherwise null. Callers treat null
    /// as "capability not available."
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
    /// Convenience: invoke the resolved factory for capability <typeparamref name="T"/>,
    /// or return null when no factory is registered.
    /// </summary>
    public static T? Create<T>() where T : class
    {
        Func<T>? factory = GetFactory<T>();
        return factory?.Invoke();
    }

    /// <summary>
    /// Remove all registered factories (active and default). Intended for test isolation;
    /// production code should not call this.
    /// </summary>
    public static void Reset()
    {
        lock (_sync)
        {
            _factories.Clear();
            _defaultFactories.Clear();
        }
    }
}
