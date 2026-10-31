using Gum.Managers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Gum.Dialogs;
using Gum.ProjectServices;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using GumFormsPlugin.Services;
using GumFormsPlugin.ViewModels;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Facts the New Project dialog states in its own text, checked against the layout rules for each platform (which
/// <c>NewProjectLayoutTests</c> pins against what the real creation writes), so a new or changed platform template
/// cannot silently contradict the dialog.
/// </summary>
public class NewProjectPlatformInvariantTests
{
    public static IEnumerable<object[]> MainPlatformNames() =>
        NewProjectPlatforms.MainNames.Select(entry => new object[] { entry.Name });

    [Theory]
    [MemberData(nameof(MainPlatformNames))]
    public async Task ExistingCodeProjectTip_MentionsGame1_ExactlyWhenThePlatformWritesIt(string platform)
    {
        NewProjectPlatforms.TryParse(platform, out HostPlatform hostPlatform).ShouldBeTrue();
        bool writesGame1 = NewProjectLayout.ForCodeProject("root", "MyGame", hostPlatform, useXmlFormat: false)
            .HostFiles.Contains(NewProjectLayout.Game1FileName);
        Mock<INewProjectService> service = new Mock<INewProjectService>();
        service.Setup(x => x.Validate(It.IsAny<NewProjectRequest>())).Returns(new NewProjectValidation());

        Mock<IFormsFileService> formsFileService = new Mock<IFormsFileService>();
        formsFileService.Setup(x => x.DefaultThemeName).Returns("Standard");
        formsFileService.Setup(x => x.GetAvailableThemes()).Returns(new List<string> { "Standard" });
        Mock<IExistingCodeProjectDetector> detector = new Mock<IExistingCodeProjectDetector>();
        detector.Setup(x => x.FindExistingCodeProject(It.IsAny<string>())).Returns(Path.Combine(Path.GetTempPath(), "Other.csproj"));
        NewProjectDialogViewModel viewModel = new NewProjectDialogViewModel(
            new ThemeSelectionViewModel(formsFileService.Object, new Mock<IProjectState>().Object),
            service.Object, detector.Object, new Mock<IDialogService>().Object, new Mock<INewProjectLocationSettings>().Object)
        {
            ProjectName = "MyGame",
            Location = Path.Combine(Path.GetTempPath(), "GumPlatformInvariant"),
            IncludeCodeProject = true,
            PlatformName = platform,
            IncludeRecommendedPackages = false,
            IsIncludeDemoScreenGum = false,
        };

        await viewModel.ChecksRefresh;

        viewModel.ExistingCodeProjectTip.Contains("Game1.cs").ShouldBe(writesGame1);
    }
}
