using System;
using System.Reflection;
using System.Runtime.Loader;

namespace Gum.Services;

/// <summary>
/// Loads a managed plugin assembly (and its private dependencies) into an isolated, collectible
/// <see cref="AssemblyLoadContext"/> and invokes a static method on it by reflection.
/// </summary>
/// <remarks>
/// This exists so the Gum tool can run gumcli's SVG export logic in-process instead of shipping
/// a second self-contained .NET runtime purely to spawn gumcli as a subprocess (issue #4723).
/// A direct project reference to gumcli's assemblies is not an option: gumcli shares process-wide
/// static state with the tool (<c>ObjectFinder.Self</c>, <c>RenderingLibrary.SystemManagers</c>),
/// and each rendering backend compiles its own copy of <c>RenderingLibrary</c>, so a second copy in
/// the same compile-time reference graph as the tool's own is a <c>CS0433</c> ambiguous-type error.
/// Loading gumcli's assemblies into their own <see cref="AssemblyLoadContext"/> instead - never the
/// default one the tool itself runs in - gives every type gumcli defines a distinct runtime identity
/// from the tool's own copies, so their static fields do not collide, without needing gumcli's own
/// code to be rewritten to avoid globals. The caller must restrict itself to primitive/string
/// arguments and return values across the boundary: any type the isolated assembly declares (or that
/// only it references) cannot be named by the caller without re-creating the exact conflict above.
/// </remarks>
public class IsolatedPluginHost : IDisposable
{
    private readonly PluginLoadContext _context;

    /// <summary>
    /// Creates a new isolated context, resolving the plugin's private dependencies (managed and
    /// native) relative to <paramref name="mainAssemblyPath"/>'s <c>.deps.json</c>.
    /// </summary>
    public IsolatedPluginHost(string mainAssemblyPath)
    {
        _context = new PluginLoadContext(mainAssemblyPath);
    }

    /// <summary>
    /// Loads <paramref name="assemblyPath"/> into this host's isolated context (if not already
    /// loaded) and invokes the public static method <paramref name="methodName"/> on
    /// <paramref name="typeName"/>, passing <paramref name="args"/>. Returns the method's return
    /// value. Throws <see cref="InvalidOperationException"/> if the type or method cannot be
    /// found, or the original exception thrown by the invoked method (unwrapped from the
    /// <see cref="TargetInvocationException"/> reflection wraps it in).
    /// </summary>
    public object? InvokeStaticMethod(string assemblyPath, string typeName, string methodName, object?[] args)
    {
        Assembly assembly = _context.LoadFromAssemblyPath(assemblyPath);

        Type type = assembly.GetType(typeName)
            ?? throw new InvalidOperationException($"Type '{typeName}' not found in '{assemblyPath}'.");

        MethodInfo method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException($"Public static method '{methodName}' not found on '{typeName}'.");

        try
        {
            return method.Invoke(null, args);
        }
        catch (TargetInvocationException e) when (e.InnerException != null)
        {
            throw e.InnerException;
        }
    }

    /// <summary>
    /// Unloads the isolated context, allowing the plugin's assemblies to be garbage collected.
    /// </summary>
    public void Dispose()
    {
        _context.Unload();
        GC.SuppressFinalize(this);
    }

    private sealed class PluginLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;

        public PluginLoadContext(string mainAssemblyPath) : base(isCollectible: true)
        {
            _resolver = new AssemblyDependencyResolver(mainAssemblyPath);
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            return assemblyPath != null ? LoadFromAssemblyPath(assemblyPath) : null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            string? libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            return libraryPath != null ? LoadUnmanagedDllFromPath(libraryPath) : IntPtr.Zero;
        }
    }
}
