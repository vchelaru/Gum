using System;
using System.IO;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Logic.FileWatch;
using Gum.StateAnimation.SaveClasses;
using Moq;
using Shouldly;
using ToolsUtilities;
using Xunit;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// End-to-end pins for the JSON project format: a .gumj project's element, behavior, and animation
/// files must round-trip through the same on-disk paths the tool writes to and the loader reads
/// from. The bug these guard against is silent — a write lands on a path the project never reads,
/// so the tool reports success and the edit is gone at the next load.
///
/// Path resolution for the individual tool services is pinned in Gum.Presentation.Tests
/// (FileCommandsTests, AnimationFilePathServiceTests); this file pins the data-model layer those
/// services write through, so a serializer/extension disagreement fails here regardless of which
/// caller introduced it.
/// </summary>
public class JsonProjectFormatRoundTripTests : IDisposable
{
    private readonly string _tempDirectory;

    public JsonProjectFormatRoundTripTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumJsonRoundTrip_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDirectory, recursive: true); } catch { }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ProjectSave_ThenLoad_RoundTripsEveryElement(bool isJsonFormat)
    {
        GumProjectSave project = BuildProject(isJsonFormat);

        project.Save(project.FullFileName, saveElements: true);

        GumProjectSave? reloaded = GumProjectSave.Load(project.FullFileName, out GumLoadResult result);

        reloaded.ShouldNotBeNull();
        result.MissingFiles.ShouldBeEmpty();
        reloaded!.Components.Single(c => c.Name == "MyComponent").IsSourceFileMissing.ShouldBeFalse();
        reloaded.Screens.Single(s => s.Name == "MyScreen").IsSourceFileMissing.ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ElementSavedIndividually_IsSeenByASubsequentProjectLoad(bool isJsonFormat)
    {
        // The per-element save path (Ctrl+S on a component) is the one that lost data: it wrote the
        // XML extension regardless of the project's format, so the JSON project reloaded the stale
        // file and the edit vanished. Saving an element and reloading the project must surface it.
        GumProjectSave project = BuildProject(isJsonFormat);
        project.Save(project.FullFileName, saveElements: true);

        ComponentSave component = project.Components.Single(c => c.Name == "MyComponent");
        component.DefaultState.Variables.Add(new VariableSave
        {
            Name = "Width",
            Value = 123.0f,
            SetsValue = true,
            Type = "float"
        });

        string elementPath = Path.Combine(_tempDirectory, component.Subfolder,
            component.Name + "." + component.GetFileExtension(isJsonFormat));
        component.Save(elementPath);

        GumProjectSave reloaded = GumProjectSave.Load(project.FullFileName, out _)!;

        reloaded.Components.Single(c => c.Name == "MyComponent")
            .DefaultState.Variables.Single(v => v.Name == "Width")
            .Value.ShouldBe(123.0f);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BehaviorSavedIndividually_IsSeenByASubsequentProjectLoad(bool isJsonFormat)
    {
        GumProjectSave project = BuildProject(isJsonFormat);
        project.Save(project.FullFileName, saveElements: true);

        BehaviorSave behavior = new() { Name = "MyBehavior", DefaultImplementation = "Controls/Button" };
        BehaviorReference reference = project.BehaviorReferences.Single(b => b.Name == "MyBehavior");
        string behaviorPath = Path.Combine(_tempDirectory, reference.GetRelativeFilePath(isJsonFormat));
        Directory.CreateDirectory(Path.GetDirectoryName(behaviorPath)!);
        behavior.Save(behaviorPath);

        GumProjectSave reloaded = GumProjectSave.Load(project.FullFileName, out _)!;

        reloaded.Behaviors.Single(b => b.Name == "MyBehavior")
            .DefaultImplementation.ShouldBe("Controls/Button");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AnimationSidecar_RoundTripsThroughTheProvider(bool isJsonFormat)
    {
        // The animation sidecar has two independent decisions - the file's extension and the
        // serializer used to write it - and they were made in different places. Writing through
        // ElementAnimationsSave.Save and reading through the provider crosses both.
        GumProjectSave project = BuildProject(isJsonFormat);
        project.Save(project.FullFileName, saveElements: true);

        ComponentSave component = project.Components.Single(c => c.Name == "MyComponent");
        string elementPath = Path.Combine(_tempDirectory, component.Subfolder,
            component.Name + "." + component.GetFileExtension(isJsonFormat));

        ElementAnimationsSave animations = new();
        animations.Animations.Add(new AnimationSave { Name = "Blink" });
        string sidecarPath = FileManager.RemoveExtension(elementPath)
            + ElementAnimationsSave.GetFileNameSuffix(isJsonFormat);
        animations.Save(sidecarPath);

        ElementAnimationsSave? loaded = new FileElementAnimationsProvider()
            .GetAnimationsFor(component, project);

        loaded.ShouldNotBeNull();
        loaded!.Animations.Single().Name.ShouldBe("Blink");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AnimationSidecar_SerializerAndExtensionAgree(bool isJsonFormat)
    {
        // Directly pins the corruption case: a .ganj holding XML (or a .ganx holding JSON) reads
        // back in the tool but fails in the runtime's animation loader, and vice versa.
        ElementAnimationsSave animations = new();
        animations.Animations.Add(new AnimationSave { Name = "Blink" });

        string path = Path.Combine(_tempDirectory,
            "FooAnimations." + (isJsonFormat ? "ganj" : "ganx"));
        animations.Save(path);

        string content = File.ReadAllText(path).TrimStart('﻿', ' ', '\r', '\n');
        if (isJsonFormat)
        {
            content.ShouldStartWith("{");
        }
        else
        {
            content.ShouldStartWith("<");
        }

        ElementAnimationsSave.Load(path).Animations.Single().Name.ShouldBe("Blink");
    }

    [Fact]
    public void ConvertedProject_LoadsEveryConvertedFile()
    {
        // The conversion is the moment both formats exist side by side. After it, loading the .gumj
        // must resolve every element from its JSON file rather than falling back to a stale .gumx
        // sibling or reporting it missing.
        GumProjectSave project = BuildProject(isJsonFormat: false);
        project.Save(project.FullFileName, saveElements: true);

        BehaviorReference behaviorReference = project.BehaviorReferences.Single();
        string xmlBehaviorPath = Path.Combine(_tempDirectory, behaviorReference.GetRelativeFilePath(false));
        Directory.CreateDirectory(Path.GetDirectoryName(xmlBehaviorPath)!);
        new BehaviorSave { Name = "MyBehavior", DefaultImplementation = "Controls/Button" }.Save(xmlBehaviorPath);

        ComponentSave component = project.Components.Single();
        string xmlElementPath = Path.Combine(_tempDirectory, component.Subfolder,
            component.Name + "." + component.GetFileExtension(isJsonFormat: false));
        ElementAnimationsSave animations = new();
        animations.Animations.Add(new AnimationSave { Name = "Blink" });
        animations.Save(FileManager.RemoveExtension(xmlElementPath)
            + ElementAnimationsSave.GetFileNameSuffix(isJsonFormat: false));

        project = GumProjectSave.Load(project.FullFileName, out _)!;

        ConvertProjectToJsonResult conversion =
            new ConvertProjectToJsonService(Mock.Of<IFileWatchIgnoreList>()).ConvertToJson(project);

        GumProjectSave? converted = GumProjectSave.Load(conversion.ProjectFilePath, out GumLoadResult result);

        converted.ShouldNotBeNull();
        result.MissingFiles.ShouldBeEmpty();
        converted!.Components.Single().IsSourceFileMissing.ShouldBeFalse();
        converted.Behaviors.Single().DefaultImplementation.ShouldBe("Controls/Button");
        conversion.AnimationCount.ShouldBe(1);

        ElementAnimationsSave? convertedAnimations = new FileElementAnimationsProvider()
            .GetAnimationsFor(converted.Components.Single(), converted);
        convertedAnimations.ShouldNotBeNull();
        convertedAnimations!.Animations.Single().Name.ShouldBe("Blink");
    }

    private GumProjectSave BuildProject(bool isJsonFormat)
    {
        string extension = isJsonFormat
            ? GumProjectSave.ProjectJsonExtension
            : GumProjectSave.ProjectExtension;

        GumProjectSave project = new()
        {
            FullFileName = Path.Combine(_tempDirectory, "MyProject." + extension)
        };

        ComponentSave component = new() { Name = "MyComponent" };
        component.States.Add(new StateSave { Name = "Default" });
        project.Components.Add(component);
        project.ComponentReferences.Add(new ElementReference
        {
            Name = component.Name,
            ElementType = ElementType.Component
        });

        ScreenSave screen = new() { Name = "MyScreen" };
        screen.States.Add(new StateSave { Name = "Default" });
        project.Screens.Add(screen);
        project.ScreenReferences.Add(new ElementReference
        {
            Name = screen.Name,
            ElementType = ElementType.Screen
        });

        project.BehaviorReferences.Add(new BehaviorReference { Name = "MyBehavior" });

        return project;
    }

}
