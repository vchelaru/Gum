using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Gum.Bundle;
using Gum.DataTypes;
using Gum.StateAnimation.SaveClasses;
using Shouldly;
using ToolsUtilities;
using Xunit;

namespace MonoGameGum.Tests.DataTypes;

/// <summary>
/// Tests for <see cref="GumService.LoadAnimationsFromProvider"/>: the enumeration-based animation
/// loader that replaced the per-element <c>FileManager.FileExists</c> probing. Driving it through an
/// in-memory <see cref="BundleGumFileProvider"/> proves it does zero per-element I/O and derives the
/// element name from the bundle path — including nested component folders, which the old
/// "**/*Animations.ganx" glob (no recursive <c>**</c> support in GlobMatcher) would have missed.
/// </summary>
public class LoadAnimationsFromProviderTests
{
    [Fact]
    public void LoadAnimationsFromProvider_loads_one_animation_file_per_ganx_and_derives_element_name_from_path()
    {
        IGumFileProvider provider = Provider(
            ("Screens/MainScreenAnimations.ganx", Ganx()),
            ("Components/Buttons/MyButtonAnimations.ganx", Ganx()));

        GumProjectSave project = new GumProjectSave();

        int loaded = GumService.LoadAnimationsFromProvider(project, provider);

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

        int loaded = GumService.LoadAnimationsFromProvider(project, provider);

        loaded.ShouldBe(1);
        project.ElementAnimations.Single().ElementName.ShouldBe("MainScreen");
    }

    [Fact]
    public void LoadAnimationsFromProvider_loads_nothing_when_no_ganx_present()
    {
        IGumFileProvider provider = Provider(
            ("Screens/MainScreen.gusx", new byte[] { 1, 2, 3 }));

        GumProjectSave project = new GumProjectSave();

        int loaded = GumService.LoadAnimationsFromProvider(project, provider);

        loaded.ShouldBe(0);
        project.ElementAnimations.ShouldBeEmpty();
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
