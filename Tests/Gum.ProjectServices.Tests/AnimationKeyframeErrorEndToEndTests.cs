using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.StateAnimation.SaveClasses;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ToolsUtilities;
using Xunit;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// End-to-end: the real <see cref="HeadlessErrorChecker"/> wired with the real
/// <see cref="AnimationKeyframeErrorSource"/> + <see cref="FileElementAnimationsProvider"/> reports a
/// dangling animation keyframe reference for a project on disk. This covers the integration seam
/// (checker actually invokes the source; provider resolves the real <c>.ganx</c> path) that the
/// unit tests mock out.
/// </summary>
public class AnimationKeyframeErrorEndToEndTests : BaseTestClass, IDisposable
{
    private readonly string _tempDirectory;
    private readonly GumProjectSave _project;

    public AnimationKeyframeErrorEndToEndTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumAnimE2E_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
        _project = new GumProjectSave { FullFileName = Path.Combine(_tempDirectory, "Project.gumx") };
    }

    public override void Dispose()
    {
        try { Directory.Delete(_tempDirectory, recursive: true); } catch { }
        base.Dispose();
    }

    [Fact]
    public void GetErrorsFor_ReportsDanglingAnimationKeyframe_ThroughTheRealChecker()
    {
        ComponentSave element = new ComponentSave { Name = "Foo" };
        StateSaveCategory category = new StateSaveCategory { Name = "Cat" };
        category.States.Add(new StateSave { Name = "Idle" });
        element.Categories.Add(category);
        _project.Components.Add(element);

        WriteGanx("Components", "FooAnimations.ganx", animationName: "Anim", keyframeStateName: "Cat/Missing");

        HeadlessErrorChecker checker = new HeadlessErrorChecker(
            Mock.Of<ITypeResolver>(),
            new IAdditionalErrorSource[] { new AnimationKeyframeErrorSource(new FileElementAnimationsProvider()) });

        IReadOnlyList<ErrorResult> errors = checker.GetErrorsFor(element, _project);

        errors.ShouldContain(error => error.Message.Contains("Cat/Missing"));
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
