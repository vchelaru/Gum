using Gum.Plugins;
using Shouldly;

namespace Gum.Presentation.Tests;

// The report exists so a plugin missing from the "Manage Plugins" list can be told apart from one
// that was never installed. These pin the two cases that distinction rests on.
public class PluginScanReportTests
{
    [Fact]
    public void Describe_NamesTheFolderAndHowToFixIt_WhenNoPluginAssemblyWasFound()
    {
        PluginScanReport report = new(@"C:\Gum\Plugins", FolderExists: true, [
            new PluginFileScan("SomeDependency.dll", PluginFileOutcome.Loaded, CouldContainPlugins: false, null),
        ]);

        string description = report.Describe();

        report.PluginAssemblies.ShouldBeEmpty();
        description.ShouldContain(@"C:\Gum\Plugins");
        description.ShouldContain("No plugin assemblies were found");
        description.ShouldContain("GumFull.sln");
    }

    // "Empty folder" and "the scan looked somewhere else" both report zero dlls and need opposite
    // fixes, so the raw listing and the path it was derived from have to be in the text.
    [Fact]
    public void Describe_ShowsFolderContentsAndExecutablePath_WhenNothingWasFound()
    {
        PluginScanReport empty = new(@"C:\Gum\Plugins", FolderExists: true, [],
            ExecutablePath: @"C:\Gum\Gum.exe", FolderEntries: []);
        PluginScanReport withEntries = new(@"C:\Gum\Plugins", FolderExists: true, [],
            ExecutablePath: @"C:\Gum\Gum.exe", FolderEntries: [@"CodeOutputPlugin\CodeOutputPlugin.pdb"]);

        empty.Describe().ShouldContain("completely empty");
        empty.Describe().ShouldContain(@"C:\Gum\Gum.exe");
        withEntries.Describe().ShouldContain(@"CodeOutputPlugin\CodeOutputPlugin.pdb");
        withEntries.Describe().ShouldNotContain("completely empty");
    }

    [Fact]
    public void Describe_ListsPluginAssembliesAndFailures_WithoutTheMissingPluginsAdvice()
    {
        PluginScanReport report = new(@"C:\Gum\Plugins", FolderExists: true, [
            new PluginFileScan("SomeDependency.dll", PluginFileOutcome.Loaded, CouldContainPlugins: false, null),
            new PluginFileScan("EditorTabPlugin_XNA.dll", PluginFileOutcome.Loaded, CouldContainPlugins: true, null),
            new PluginFileScan("dxcompiler.dll", PluginFileOutcome.NotManagedAssembly, false, null),
            new PluginFileScan("Broken.dll", PluginFileOutcome.LoadFailed, false, "Bad IL format."),
        ]);

        string description = report.Describe();

        description.ShouldContain("EditorTabPlugin_XNA.dll");
        description.ShouldContain("Broken.dll - Bad IL format.");
        description.ShouldContain("1 holding plugins, 1 dependencies, 1 native, 1 failed to load");
        description.ShouldNotContain("No plugin assemblies were found");
        // A native DLL is expected in a plugin folder and is counted, not listed as a problem.
        description.ShouldNotContain("dxcompiler.dll");
    }
}
