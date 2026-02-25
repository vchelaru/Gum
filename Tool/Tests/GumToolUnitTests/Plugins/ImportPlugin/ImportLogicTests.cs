using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins.ImportPlugin.Manager;
using Moq.AutoMock;
using Shouldly;
using System;
using System.IO;

namespace GumToolUnitTests.Plugins.ImportPlugin;

public class ImportLogicTests : BaseTestClass
{
    private readonly AutoMocker _mocker;
    private readonly ImportLogic _importLogic;
    private readonly string _testDirectory;
    private readonly string _componentsDirectory;

    public ImportLogicTests()
    {
        _mocker = new AutoMocker();

        _testDirectory = Path.Combine(Path.GetTempPath(), "GumTests", Guid.NewGuid().ToString());
        _componentsDirectory = Path.Combine(_testDirectory, "Components");
        Directory.CreateDirectory(_componentsDirectory);

        var gumProject = new GumProjectSave();
        gumProject.FullFileName = Path.Combine(_testDirectory, "TestProject.gumx");
        ObjectFinder.Self.GumProjectSave = gumProject;

        StandardElementsManager.Self.Initialize();

        _mocker.GetMock<IProjectManager>()
            .Setup(x => x.GumProjectSave)
            .Returns(gumProject);

        _importLogic = _mocker.CreateInstance<ImportLogic>();
    }

    public override void Dispose()
    {
        base.Dispose();

        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    [Fact]
    public void ImportComponent_ShouldDeserializeBasicComponent()
    {
        // Arrange
        var filePath = Path.Combine(_componentsDirectory, "ButtonDerived.gucx");
        File.WriteAllText(filePath,
@"<?xml version=""1.0"" encoding=""utf-8""?>
<ComponentSave xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <Name>ButtonDerived</Name>
  <BaseType>Controls/ButtonStandard</BaseType>
  <State>
    <Name>Default</Name>
    <Variable Type=""State"" Name=""State"" Category=""States and Visibility"" SetsValue=""false"">
      <Value xsi:type=""xsd:string"">Default</Value>
    </Variable>
  </State>
  <Instance Name=""Background"" BaseType=""NineSlice"" DefinedByBase=""true"" />
  <Instance Name=""TextInstance"" BaseType=""Text"" DefinedByBase=""true"" />
  <Instance Name=""FocusedIndicator"" BaseType=""NineSlice"" DefinedByBase=""true"" />
  <Behaviors />
</ComponentSave>");

        // Act
        var result = _importLogic.ImportComponent(filePath, saveProject: false);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("ButtonDerived");
        result.BaseType.ShouldBe("Controls/ButtonStandard");
        result.Instances.Count.ShouldBe(3);
        result.Instances[0].Name.ShouldBe("Background");
        result.Instances[1].Name.ShouldBe("TextInstance");
        result.Instances[2].Name.ShouldBe("FocusedIndicator");
    }
}
