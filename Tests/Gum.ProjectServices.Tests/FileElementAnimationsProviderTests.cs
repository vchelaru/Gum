using Gum.DataTypes;
using Gum.StateAnimation.SaveClasses;
using Shouldly;
using System;
using System.IO;
using System.Linq;
using ToolsUtilities;
using Xunit;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Pins how the disk provider locates an element's <c>.ganx</c> (relative to the project, by element
/// type), deserializes it, and caches the result. The <c>.ganx</c> is written to the convention path
/// independently of the provider so a path-resolution regression is actually caught.
/// </summary>
public class FileElementAnimationsProviderTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly GumProjectSave _project;

    public FileElementAnimationsProviderTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumAnimProvider_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
        _project = new GumProjectSave { FullFileName = Path.Combine(_tempDirectory, "Project.gumx") };
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDirectory, recursive: true); } catch { }
    }

    [Fact]
    public void GetAnimationsFor_ReturnsDeserializedAnimations_FromTheComponentsGanx()
    {
        ComponentSave element = new ComponentSave { Name = "Foo" };
        WriteGanx("Components", "FooAnimations.ganx", animationName: "Anim", keyframeStateName: "Cat/Idle");

        ElementAnimationsSave? result = new FileElementAnimationsProvider().GetAnimationsFor(element, _project);

        result.ShouldNotBeNull();
        result!.Animations.Single().States.Single().StateName.ShouldBe("Cat/Idle");
    }

    [Fact]
    public void GetAnimationsFor_ReturnsNull_WhenNoGanxExists()
    {
        ComponentSave element = new ComponentSave { Name = "Foo" };

        new FileElementAnimationsProvider().GetAnimationsFor(element, _project).ShouldBeNull();
    }

    [Fact]
    public void GetAnimationsFor_ReturnsCachedInstance_OnRepeatedCallsWithoutFileChange()
    {
        ComponentSave element = new ComponentSave { Name = "Foo" };
        WriteGanx("Components", "FooAnimations.ganx", animationName: "Anim", keyframeStateName: "Cat/Idle");
        FileElementAnimationsProvider provider = new FileElementAnimationsProvider();

        ElementAnimationsSave? first = provider.GetAnimationsFor(element, _project);
        ElementAnimationsSave? second = provider.GetAnimationsFor(element, _project);

        second.ShouldBeSameAs(first);
    }

    private void WriteGanx(string relativeDirectory, string fileName, string animationName, string keyframeStateName)
    {
        string directory = Path.Combine(_tempDirectory, relativeDirectory);
        Directory.CreateDirectory(directory);

        AnimationSave animation = new AnimationSave { Name = animationName };
        animation.States.Add(new AnimatedStateSave { StateName = keyframeStateName });
        ElementAnimationsSave animations = new ElementAnimationsSave();
        animations.Animations.Add(animation);

        FileManager.XmlSerialize(animations, Path.Combine(directory, fileName));
    }
}
