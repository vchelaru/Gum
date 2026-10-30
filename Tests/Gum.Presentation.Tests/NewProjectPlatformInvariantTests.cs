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
/// Facts the New Project dialog states in its own text, checked against what the real shared service writes for each
/// platform, so a new or changed platform template cannot silently contradict the dialog.
/// </summary>
public class NewProjectPlatformInvariantTests
{
    public static IEnumerable<object[]> MainPlatformNames() =>
        NewProjectPlatforms.MainNames.Select(entry => new object[] { entry.Name });

    [Theory]
    [MemberData(nameof(MainPlatformNames))]
    public async Task ExistingCodeProjectTip_MentionsGame1_ExactlyWhenThePlatformWritesIt(string platform)
    {
        NewProjectService service = new NewProjectService(
            new FormsThemeLocator(new string[0]), new Mock<IRestoreRunner>().Object, new string[0]);
        NewProjectPlan? plan = service.GetPlan(new NewProjectRequest
        {
            Path = Path.Combine(Path.GetTempPath(), "GumPlatformInvariant", "MyGame"),
            Platform = platform,
            Template = "empty"
        });
        plan.ShouldNotBeNull();
        bool writesGame1 = plan.Files.Any(file => file == "Game1.cs" || file.EndsWith("/Game1.cs"));

        Mock<IFormsFileService> formsFileService = new Mock<IFormsFileService>();
        formsFileService.Setup(x => x.DefaultThemeName).Returns("Standard");
        formsFileService.Setup(x => x.GetAvailableThemes()).Returns(new List<string> { "Standard" });
        Mock<IExistingCodeProjectDetector> detector = new Mock<IExistingCodeProjectDetector>();
        detector.Setup(x => x.FindExistingCodeProject(It.IsAny<string>())).Returns(Path.Combine(Path.GetTempPath(), "Other.csproj"));
        NewProjectDialogViewModel viewModel = new NewProjectDialogViewModel(
            new ThemeSelectionViewModel(formsFileService.Object, new Mock<IProjectState>().Object),
            service, detector.Object, new Mock<IDialogService>().Object)
        {
            ProjectName = "MyGame",
            Location = Path.Combine(Path.GetTempPath(), "GumPlatformInvariant"),
            IncludeCodeProject = true,
            PlatformName = platform,
        };

        await viewModel.PlanRefresh;

        viewModel.ExistingCodeProjectTip.Contains("Game1.cs").ShouldBe(writesGame1);
    }
}
