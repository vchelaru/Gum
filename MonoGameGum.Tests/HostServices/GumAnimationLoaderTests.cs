using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Gum;
using Gum.Bundle;
using Gum.DataTypes;
using Gum.StateAnimation.SaveClasses;
using Shouldly;
using ToolsUtilities;
using Xunit;

namespace MonoGameGum.Tests.HostServices;

/// <summary>
/// Tests for <see cref="GumAnimationLoader.LoadAnimationsFromProvider"/>: the enumeration-based animation
/// loader that replaced the per-element <c>FileManager.FileExists</c> probing. Driving it through an
/// in-memory <see cref="BundleGumFileProvider"/> proves it does zero per-element I/O and derives the
/// element name from the bundle path — including nested component folders, which the old
/// "**/*Animations.ganx" glob (no recursive <c>**</c> support in GlobMatcher) would have missed.
/// </summary>
public class GumAnimationLoaderTests
{
    [Fact]
    public void LoadAnimationsFromProvider_loads_one_animation_file_per_ganx_and_derives_element_name_from_path()
    {
        IGumFileProvider provider = Provider(
            ("Screens/MainScreenAnimations.ganx", Ganx()),
            ("Components/Buttons/MyButtonAnimations.ganx", Ganx()));

        GumProjectSave project = new GumProjectSave();

        int loaded = GumAnimationLoader.LoadAnimationsFromProvider(project, provider);

        loaded.ShouldBe(2);
        project.ElementAnimations
            .Select(animation => animation.ElementName)
            .ShouldBe(new[] { "MainScreen", "Buttons/MyButton" }, ignoreOrder: true);
    }

    [Fact]
    public void LoadAnimationsFromProvider_ignores_files_that_are_not_animation_files()
    {
        IGumFileProvider provider = Provider(
            ("Screens/MainScreen.gusx", new byte[] { 1, 2, 3 }),
            ("Screens/MainScreenAnimations.ganx", Ganx()));

        GumProjectSave project = new GumProjectSave();

        int loaded = GumAnimationLoader.LoadAnimationsFromProvider(project, provider);

        loaded.ShouldBe(1);
        project.ElementAnimations.Single().ElementName.ShouldBe("MainScreen");
    }

    [Fact]
    public void LoadAnimationsFromProvider_loads_nothing_when_no_ganx_present()
    {
        IGumFileProvider provider = Provider(
            ("Screens/MainScreen.gusx", new byte[] { 1, 2, 3 }));

        GumProjectSave project = new GumProjectSave();

        int loaded = GumAnimationLoader.LoadAnimationsFromProvider(project, provider);

        loaded.ShouldBe(0);
        project.ElementAnimations.ShouldBeEmpty();
    }

    [Fact]
    public void ShouldWarnEnumerationUnavailable_IsFalse_ForLooseProviderOverRealDirectoryWithNoAnimations()
    {
        string directory = Directory.CreateTempSubdirectory().FullName;
        try
        {
            LooseFileGumFileProvider provider = new LooseFileGumFileProvider(directory);

            GumAnimationLoader.ShouldWarnEnumerationUnavailable(provider, usedBundle: false, loaded: 0)
                .ShouldBeFalse();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void ShouldWarnEnumerationUnavailable_IsTrue_ForLooseProviderWithMissingRoot()
    {
        string missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        LooseFileGumFileProvider provider = new LooseFileGumFileProvider(missing);

        GumAnimationLoader.ShouldWarnEnumerationUnavailable(provider, usedBundle: false, loaded: 0)
            .ShouldBeTrue();
    }

    [Fact]
    public void ShouldWarnEnumerationUnavailable_IsFalse_ForBundleOrWhenAnimationsLoaded()
    {
        string missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        LooseFileGumFileProvider provider = new LooseFileGumFileProvider(missing);

        GumAnimationLoader.ShouldWarnEnumerationUnavailable(provider, usedBundle: true, loaded: 0).ShouldBeFalse();
        GumAnimationLoader.ShouldWarnEnumerationUnavailable(provider, usedBundle: false, loaded: 1).ShouldBeFalse();
    }

    private static byte[] Ganx()
    {
        ElementAnimationsSave save = new ElementAnimationsSave();
        // Intentionally wrong: the loader must overwrite ElementName from the bundle path, not
        // trust whatever is serialized inside the file.
        save.ElementName = "WRONG";
        XmlSerializer serializer = FileManager.GetXmlSerializer(typeof(ElementAnimationsSave));
        using MemoryStream memoryStream = new MemoryStream();
        serializer.Serialize(memoryStream, save);
        return memoryStream.ToArray();
    }

    private static IGumFileProvider Provider(params (string path, byte[] bytes)[] entries)
    {
        Dictionary<string, byte[]> dictionary = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        List<string> order = new List<string>();
        foreach ((string path, byte[] bytes) in entries)
        {
            dictionary[path] = bytes;
            order.Add(path);
        }
        return new BundleGumFileProvider(new GumBundle(version: 1, entries: dictionary, entryPathsInOrder: order));
    }
}
