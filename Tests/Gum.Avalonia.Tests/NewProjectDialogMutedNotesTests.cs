using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Gum.Avalonia.Tests.Harness;
using Gum.Dialogs;
using Gum.Managers;
using Gum.ProjectServices;
using Gum.Services.Dialogs;
using GumFormsPlugin.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>Check box labels in the New Project dialog read as normal text; the notes under them read as secondary text.</summary>
public class NewProjectDialogMutedNotesTests
{
    private static readonly string ProjectsFolder = OperatingSystem.IsWindows() ? @"C:\Users\You\Projects" : "/Users/You/Projects";

    private static ScreenshotWindow Open(ThemeVariant theme)
    {
        Mock<INewProjectService> service = new();
        service.Setup(x => x.IsSourceLinkedAvailable).Returns(true);
        NewProjectDialogViewModel viewModel = new NewProjectDialogViewModel(
            TestAppBuilder.Services.GetRequiredService<ThemeSelectionViewModel>(),
            service.Object, new Mock<IExistingCodeProjectDetector>().Object, new Mock<IDialogService>().Object, new Mock<INewProjectLocationSettings>().Object)
        {
            Location = ProjectsFolder,
            IncludeCodeProject = true,
        };
        return PrScreenshot.ShowDialog(viewModel, theme);
    }

    private static Color ColorOf(TextBlock text) => ((ISolidColorBrush)text.Foreground!).Color;

    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void CheckBoxLabels_AreNormalWeight(string theme)
    {
        using ScreenshotWindow window = Open(theme == "light" ? ThemeVariant.Light : ThemeVariant.Dark);

        foreach (string label in new[] { NewProjectDialogText.RestorePackages, NewProjectDialogText.UseLocalSource, NewProjectDialogText.GenerateCode })
        {
            TextBlock text = window.Find<TextBlock>(t => t.Text == label);
            text.FontWeight.ShouldBe(FontWeight.Normal, label);
        }
    }

    [AvaloniaTheory]
    [InlineData("dark")]
    [InlineData("light")]
    public void Notes_AreMutedComparedToCheckBoxLabels(string theme)
    {
        using ScreenshotWindow window = Open(theme == "light" ? ThemeVariant.Light : ThemeVariant.Dark);
        Color label = ColorOf(window.Find<TextBlock>(t => t.Text == NewProjectDialogText.RestorePackages));

        foreach (string noteText in new[] { NewProjectDialogText.UseLocalSourceNote, NewProjectDialogText.RestorePackagesNote })
        {
            TextBlock note = window.Find<TextBlock>(t => t.Text == noteText);
            ColorOf(note).ShouldNotBe(label, noteText);
            note.Opacity.ShouldBe(1d, noteText);
        }
    }
}
