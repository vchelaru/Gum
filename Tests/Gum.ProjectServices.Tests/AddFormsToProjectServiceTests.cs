using System.Linq;
using Gum.DataTypes;
using Gum.ProjectServices;
using Shouldly;

namespace Gum.ProjectServices.Tests;

public class AddFormsToProjectServiceTests : IDisposable
{
    private readonly AddFormsToProjectService _sut;
    private readonly string _tempDirectory;

    public AddFormsToProjectServiceTests()
    {
        _sut = new AddFormsToProjectService();
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumAddFormsToProjectServiceTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void AddFormsTo_OnEmptyProject_ShouldSucceedAndAddComponentsStandardsAndBehaviors()
    {
        string filePath = Path.Combine(_tempDirectory, "TestProject.gumx");
        new ProjectCreator().Create(filePath);

        AddFormsResult result = _sut.AddFormsTo(filePath);

        result.Success.ShouldBeTrue();
        result.AddedComponents.ShouldContain("Controls/ButtonStandard");
        result.AddedBehaviors.ShouldContain("ButtonBehavior");

        ProjectLoadResult loadResult = new ProjectLoader().Load(filePath);
        loadResult.Success.ShouldBeTrue();
        loadResult.LoadErrors.ShouldBeEmpty();
        loadResult.Project!.Behaviors.First(b => b.Name == "ButtonBehavior").IsSourceFileMissing.ShouldBeFalse();
    }

    [Fact]
    public void AddFormsTo_WhenComponentAlreadyPresent_ShouldSkipItWithoutDuplicating()
    {
        string filePath = Path.Combine(_tempDirectory, "TestProject.gumx");
        new FormsTemplateCreator().Create(filePath);

        AddFormsResult result = _sut.AddFormsTo(filePath);

        result.Success.ShouldBeTrue();
        result.AddedComponents.ShouldNotContain("Controls/ButtonStandard");

        ProjectLoadResult loadResult = new ProjectLoader().Load(filePath);
        loadResult.Project!.ComponentReferences.Count(r => r.Name == "Controls/ButtonStandard").ShouldBe(1);
    }

    [Fact]
    public void AddFormsTo_WhenProjectFileMissing_ShouldFailWithoutThrowing()
    {
        string filePath = Path.Combine(_tempDirectory, "DoesNotExist.gumx");

        AddFormsResult result = _sut.AddFormsTo(filePath);

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldNotBeNull();
        result.ErrorMessage!.ShouldContain("not found");
    }

    [Fact]
    public void AddFormsTo_WhenProjectIsOlderThanTemplate_ShouldRejectAndLeaveProjectUntouched()
    {
        string filePath = Path.Combine(_tempDirectory, "TestProject.gumx");
        new ProjectCreator().Create(filePath);
        ProjectLoadResult created = new ProjectLoader().Load(filePath);
        created.Project!.Version = (int)GumProjectSave.GumxVersions.AttributeVersion;
        created.Project.Save(filePath, saveElements: false);
        string before = File.ReadAllText(filePath);

        AddFormsResult result = _sut.AddFormsTo(filePath);

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldNotBeNull();
        result.ErrorMessage!.ShouldContain("docs.flatredball.com/gum/gum-tool/upgrading/upgrading-file-gumx-version");
        result.AddedComponents.ShouldBeEmpty();
        File.ReadAllText(filePath).ShouldBe(before);
        Directory.GetFiles(_tempDirectory, "*", SearchOption.AllDirectories)
            .Where(f => f.Contains("Components") || f.Contains("Behaviors") || f.EndsWith("UISpriteSheet.png"))
            .ShouldBeEmpty();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
