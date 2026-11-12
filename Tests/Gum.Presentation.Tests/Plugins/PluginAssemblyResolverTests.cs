using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using Gum.DataTypes.Variables;
using Gum.Plugins;
using PluginHostFixture;
using Shouldly;

namespace Gum.Presentation.Tests.Plugins;

/// <summary>
/// A plugin built against the NuGet package references the package's assembly revision, which can
/// be higher than the tool's (#7959). The tool must still bind its own assembly for a revision
/// difference, and only for that. Each test writes a plugin assembly whose one class derives from a
/// type of an assembly the test process already loaded, referenced at a chosen version, and loads it
/// the way the plugin scan does (Assembly.LoadFrom) with the resolver on AppDomain.AssemblyResolve.
/// </summary>
public class PluginAssemblyResolverTests
{
    private static int CountLoadablePluginTypes(string pluginPath, PluginAssemblyResolver resolver)
    {
        ResolveEventHandler handler = resolver.Resolve;
        AppDomain.CurrentDomain.AssemblyResolve += handler;
        using IDisposable withoutHostFallback = SuspendTestHostResolvingFallback();
        try
        {
            Assembly plugin = Assembly.LoadFrom(pluginPath);
            try
            {
                return plugin.GetTypes().Length;
            }
            catch (ReflectionTypeLoadException exception)
            {
                return exception.Types.OfType<Type>().Count();
            }
        }
        finally
        {
            AppDomain.CurrentDomain.AssemblyResolve -= handler;
        }
    }

    /// <summary>
    /// The test host registers an AssemblyLoadContext.Resolving handler on the default context that
    /// binds any assembly already loaded by simple name, ignoring the version. That runs before
    /// AssemblyResolve, so with it present a too-new reference binds without the tool's resolver ever
    /// being asked, which the shipped tool does not have (verified in a plain console process, where
    /// the same plugin fails with "Could not load file or assembly"). Suspend it for the load.
    /// </summary>
    private static IDisposable SuspendTestHostResolvingFallback()
    {
        FieldInfo field = typeof(AssemblyLoadContext).GetField("_resolving", BindingFlags.NonPublic | BindingFlags.Instance)!;
        object? original = field.GetValue(AssemblyLoadContext.Default);
        field.SetValue(AssemblyLoadContext.Default, null);
        return new Restore(() => field.SetValue(AssemblyLoadContext.Default, original));
    }

    private sealed class Restore(Action restore) : IDisposable
    {
        public void Dispose() => restore();
    }

    private static string WritePluginDerivingFrom(Type baseType, Version referencedVersion)
    {
        string directory = Path.Combine(Path.GetTempPath(), "GumPluginResolverTests", Guid.NewGuid().ToString("N"));
        return PluginAssemblyWriter.WriteDerivingFrom(directory, "RevPlugin" + Guid.NewGuid().ToString("N"),
            baseType.Assembly.GetName().Name!, referencedVersion, baseType.Namespace!, baseType.Name);
    }

    [Fact]
    public void Resolve_PluginReferencesGumAssemblyAtHigherRevision_PluginTypeLoads()
    {
        Version loaded = typeof(VariableSave).Assembly.GetName().Version!;
        string pluginPath = WritePluginDerivingFrom(typeof(VariableSave),
            new Version(loaded.Major, loaded.Minor, loaded.Build, loaded.Revision + 1));

        CountLoadablePluginTypes(pluginPath, new PluginAssemblyResolver(_ => { })).ShouldBe(1);
    }

    [Fact]
    public void Resolve_PluginReferencesGumAssemblyFromALaterBuild_StaysRejectedAndSaysWhy()
    {
        Version loaded = typeof(VariableSave).Assembly.GetName().Version!;
        string pluginPath = WritePluginDerivingFrom(typeof(VariableSave),
            new Version(loaded.Major, loaded.Minor, loaded.Build + 1, 0));
        List<string> output = new();

        CountLoadablePluginTypes(pluginPath, new PluginAssemblyResolver(output.Add)).ShouldBe(0);

        string message = output.ShouldHaveSingleItem();
        message.ShouldContain("Couldn't find assembly");
        message.ShouldContain("newer build of Gum");
    }

    [Fact]
    public void Resolve_PluginReferencesNonGumAssemblyAtHigherRevision_StaysRejected()
    {
        Version loaded = typeof(ShouldAssertException).Assembly.GetName().Version!;
        string pluginPath = WritePluginDerivingFrom(typeof(ShouldAssertException),
            new Version(loaded.Major, loaded.Minor, loaded.Build, loaded.Revision + 1));
        List<string> output = new();

        CountLoadablePluginTypes(pluginPath, new PluginAssemblyResolver(output.Add)).ShouldBe(0);

        output.ShouldHaveSingleItem().ShouldContain("Couldn't find assembly");
    }
}
