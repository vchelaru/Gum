using Gum.DataTypes;
using Gum.Services.Dialogs;
using Moq;
using Shouldly;
using StateAnimationPlugin.Managers;
using System;
using System.IO;
using ToolsUtilities;

namespace Gum.Presentation.Tests;

/// <summary>
/// Pins <see cref="ElementDeleteService"/>'s behavior after it moved off a live WPF CheckBox field
/// to the framework-neutral <see cref="DeleteOptionCheckboxViewModel"/> seam (ADR-0005, issue #3754).
/// </summary>
public class ElementDeleteServiceTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly Mock<IAnimationFilePathService> _animationFilePathService;
    private readonly Mock<IDialogService> _dialogService;
    private readonly ElementDeleteService _service;

    public ElementDeleteServiceTests()
    {
        _tempDirectory = Path.Combine(
            Path.GetTempPath(), "GumElementDeleteServiceTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);

        _animationFilePathService = new Mock<IAnimationFilePathService>();
        _dialogService = new Mock<IDialogService>();
        _service = new ElementDeleteService(_animationFilePathService.Object, _dialogService.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void HandleConfirmDelete_DeletesAnimationFile_WhenIsCheckedTrue()
    {
        ComponentSave component = new ComponentSave { Name = "Foo" };
        FilePath ganxPath = CreateAnimationFile("Foo");
        _animationFilePathService.Setup(x => x.GetAbsoluteAnimationFileNameFor(component)).Returns(ganxPath);

        _service.HandleConfirmDelete(new object[] { component }, isChecked: true);

        File.Exists(ganxPath.FullPath).ShouldBeFalse();
    }

    [Fact]
    public void HandleConfirmDelete_DoesNotDeleteAnimationFile_WhenIsCheckedFalse()
    {
        ComponentSave component = new ComponentSave { Name = "Foo" };
        FilePath ganxPath = CreateAnimationFile("Foo");
        _animationFilePathService.Setup(x => x.GetAbsoluteAnimationFileNameFor(component)).Returns(ganxPath);

        _service.HandleConfirmDelete(new object[] { component }, isChecked: false);

        File.Exists(ganxPath.FullPath).ShouldBeTrue();
    }

    [Fact]
    public void HandleDeleteOptionsWindowShow_ReturnsCheckedCheckbox_WhenComponentHasAnimationFile()
    {
        ComponentSave component = new ComponentSave { Name = "Foo" };
        FilePath ganxPath = CreateAnimationFile("Foo");
        _animationFilePathService.Setup(x => x.GetAbsoluteAnimationFileNameFor(component)).Returns(ganxPath);

        DeleteOptionCheckboxViewModel? checkbox = _service.HandleDeleteOptionsWindowShow(new object[] { component });

        checkbox.ShouldNotBeNull();
        checkbox.IsChecked.ShouldBeTrue();
        checkbox.Label.ShouldBe("Delete Animation file (.ganx)");
    }

    [Fact]
    public void HandleDeleteOptionsWindowShow_ReturnsCheckedCheckbox_WhenMultipleElementsHaveAnimationFiles()
    {
        // Pins a boyscout fix (issue #3754): the original WPF-CheckBox-field version reset
        // IsChecked to false whenever a second matching element was processed after the first,
        // because it toggled one shared field's IsChecked per iteration instead of computing the
        // default once from the whole set.
        ComponentSave first = new ComponentSave { Name = "Foo" };
        ScreenSave second = new ScreenSave { Name = "Bar" };
        _animationFilePathService.Setup(x => x.GetAbsoluteAnimationFileNameFor(first)).Returns(CreateAnimationFile("Foo"));
        _animationFilePathService.Setup(x => x.GetAbsoluteAnimationFileNameFor(second)).Returns(CreateAnimationFile("Bar"));

        DeleteOptionCheckboxViewModel? checkbox = _service.HandleDeleteOptionsWindowShow(new object[] { first, second });

        checkbox.ShouldNotBeNull();
        checkbox.IsChecked.ShouldBeTrue();
    }

    [Fact]
    public void HandleDeleteOptionsWindowShow_ReturnsNull_WhenNoElementsHaveAnimationFile()
    {
        ComponentSave component = new ComponentSave { Name = "Foo" };
        _animationFilePathService.Setup(x => x.GetAbsoluteAnimationFileNameFor(component))
            .Returns(new FilePath(Path.Combine(_tempDirectory, "FooAnimations.ganx")));

        DeleteOptionCheckboxViewModel? checkbox = _service.HandleDeleteOptionsWindowShow(new object[] { component });

        checkbox.ShouldBeNull();
    }

    private FilePath CreateAnimationFile(string elementName)
    {
        string path = Path.Combine(_tempDirectory, elementName + "Animations.ganx");
        File.WriteAllText(path, "<ElementAnimationsSave />");
        return new FilePath(path);
    }
}
