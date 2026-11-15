using Gum.DataTypes;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Moq;
using Shouldly;
using ToolsUtilities;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// GUM0005 in the shared checker, so <c>gumcli check</c> and the tool's Errors tab agree on orphaned
/// code files (#8097).
/// </summary>
public class HeadlessErrorCheckerOrphanCodeFileTests : BaseTestClass
{
    private readonly Mock<IOrphanCodeFileProvider> _provider = new Mock<IOrphanCodeFileProvider>();

    private HeadlessErrorChecker CreateChecker(bool withProvider = true) =>
        new HeadlessErrorChecker(new DefaultTypeResolver(), Array.Empty<IAdditionalErrorSource>(),
            withProvider ? _provider.Object : null);

    private void ArrangeOrphans(params OrphanCodeFile[] orphans) =>
        _provider.Setup(x => x.GetOrphans(It.IsAny<GumProjectSave>()))
            .Returns(new OrphanCodeFileScanResult(orphans, isTruncated: false, codeRoot: null));

    [Fact]
    public void GetAllErrors_ReportsOrphanAsWarningWithDetails_WhenNoElementMatches()
    {
        ArrangeOrphans(new OrphanCodeFile(new FilePath("/game/Screens/Gone.Generated.cs"), OrphanCodeFileKind.Generated, "Gone"));

        ErrorResult error = CreateChecker().GetAllErrors(Project).Single(e => e.Code == "GUM0005");

        error.Severity.ShouldBe(ErrorSeverity.Warning);
        error.ElementName.ShouldBe("Gone");
        error.Message.ShouldContain("Gone.Generated.cs");
        OrphanCodeFileDetails details = error.Details.ShouldBeOfType<OrphanCodeFileDetails>();
        details.Kind.ShouldBe(OrphanCodeFileKind.Generated);
        details.ElementExists.ShouldBeFalse();
    }

    [Fact]
    public void GetAllErrors_MarksElementExists_WhenTheOrphansElementIsStillInTheProject()
    {
        ScreenSave screen = new ScreenSave { Name = "Kept" };
        Project.Screens.Add(screen);
        ObjectFinder.Self.GumProjectSave = Project;
        ArrangeOrphans(new OrphanCodeFile(new FilePath("/old/Kept.Generated.cs"), OrphanCodeFileKind.Generated, "Kept"));

        ErrorResult error = CreateChecker().GetAllErrors(Project).Single(e => e.Code == "GUM0005");

        ((OrphanCodeFileDetails)error.Details!).ElementExists.ShouldBeTrue();
        error.Message.ShouldContain("old path");
    }

    [Fact]
    public void GetProjectErrors_ReportsOrphans_SoTheErrorsTabAndTheCliAgree()
    {
        ArrangeOrphans(new OrphanCodeFile(new FilePath("/game/Gone.codsj"), OrphanCodeFileKind.ElementSettings, "Gone"));

        CreateChecker().GetProjectErrors(Project).ShouldContain(e => e.Code == "GUM0005");
    }

    [Fact]
    public void GetAllErrors_ReportsNothing_WhenNoProviderWasGiven()
    {
        CreateChecker(withProvider: false).GetAllErrors(Project).ShouldNotContain(e => e.Code == "GUM0005");
    }
}
