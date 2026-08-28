using Gum.DataTypes;
using Gum.Managers;
using Shouldly;

namespace Gum.ProjectServices.Tests;

public class ScreenImportServiceTests : BaseTestClass
{
    private readonly ScreenImportService _sut = new();

    public ScreenImportServiceTests()
    {
        ObjectFinder.Self.GumProjectSave = Project;
    }

    [Fact]
    public void ImportScreen_ShouldAddScreenAndReference_WhenNameIsFree()
    {
        ScreenSave screenSave = new() { Name = "NewScreen" };

        ScreenImportResult result = _sut.ImportScreen(Project, screenSave);

        result.Success.ShouldBeTrue();
        result.ImportedScreen.ShouldBeSameAs(screenSave);
        Project.Screens.ShouldContain(screenSave);
        Project.ScreenReferences.ShouldContain(r => r.Name == "NewScreen" && r.ElementType == ElementType.Screen);
    }

    [Fact]
    public void ImportScreen_ShouldFailWithConflict_WhenNameAlreadyExists()
    {
        ScreenSave existing = new() { Name = "Dupe" };
        Project.Screens.Add(existing);
        Project.ScreenReferences.Add(new ElementReference { Name = "Dupe", ElementType = ElementType.Screen });

        ScreenSave incoming = new() { Name = "Dupe" };
        ScreenImportResult result = _sut.ImportScreen(Project, incoming);

        result.Success.ShouldBeFalse();
        result.ConflictingScreenName.ShouldBe("Dupe");
        Project.Screens.ShouldNotContain(incoming);
    }
}
