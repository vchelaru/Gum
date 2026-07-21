using System;
using System.IO;
using Gum.Plugins;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Characterization (pinning) test for PluginEnablementStore, extracted out of PluginManager's
/// static mPluginSettingsSave field/LoadPluginSettings/SavePluginSettings/ShutDownPlugin/
/// ReenablePlugin logic (#3880) so plugin-enablement persistence is testable independent of the
/// MEF composition machinery PluginManager itself owns.
/// </summary>
public class PluginEnablementStoreTests : IDisposable
{
    private readonly string _fileName = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.xml");

    public void Dispose()
    {
        if (File.Exists(_fileName))
        {
            File.Delete(_fileName);
        }
    }

    [Fact]
    public void IsDisabled_ReturnsFalse_WhenNoFileExistsYet()
    {
        PluginEnablementStore store = new(_fileName);

        store.Load();

        store.IsDisabled("SomePlugin").ShouldBeFalse();
    }

    [Fact]
    public void Disable_PersistsAcrossReload()
    {
        PluginEnablementStore store = new(_fileName);
        store.Load();

        store.Disable("SomePlugin");

        PluginEnablementStore reloaded = new(_fileName);
        reloaded.Load();
        reloaded.IsDisabled("SomePlugin").ShouldBeTrue();
    }

    [Fact]
    public void Disable_CalledTwice_DoesNotDuplicateEntry()
    {
        PluginEnablementStore store = new(_fileName);
        store.Load();

        store.Disable("SomePlugin");
        store.Disable("SomePlugin");
        // A single Enable() call only removes one occurrence - if Disable() had duplicated the entry
        // (no Contains guard), a duplicate would survive and IsDisabled would still report true here.
        store.Enable("SomePlugin");

        store.IsDisabled("SomePlugin").ShouldBeFalse();
    }

    [Fact]
    public void Enable_RemovesPreviouslyDisabledPlugin_AndPersists()
    {
        PluginEnablementStore store = new(_fileName);
        store.Load();
        store.Disable("SomePlugin");

        store.Enable("SomePlugin");

        PluginEnablementStore reloaded = new(_fileName);
        reloaded.Load();
        reloaded.IsDisabled("SomePlugin").ShouldBeFalse();
    }

    [Fact]
    public void Enable_WhenNotDisabled_DoesNotCreateFile()
    {
        PluginEnablementStore store = new(_fileName);
        store.Load();

        store.Enable("SomePlugin");

        File.Exists(_fileName).ShouldBeFalse();
    }
}
