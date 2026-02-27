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
    private readonly string _behaviorsDirectory;
    private readonly string _componentsDirectory;

    public ImportLogicTests()
    {
        _mocker = new AutoMocker();

        _testDirectory = Path.Combine(Path.GetTempPath(), "GumTests", Guid.NewGuid().ToString());
        _behaviorsDirectory = Path.Combine(_testDirectory, "Behaviors");
        _componentsDirectory = Path.Combine(_testDirectory, "Components");
        Directory.CreateDirectory(_behaviorsDirectory);
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
    public void ImportBehavior_ShouldDeserializeV1BehaviorWithNoVariables()
    {
        // Arrange — v1 format with no variables; absence of <Variable> must not be misread as compact
        var filePath = Path.Combine(_behaviorsDirectory, "ButtonBehavior.behx");
        File.WriteAllText(filePath,
@"<?xml version=""1.0"" encoding=""utf-8""?>
<BehaviorSave xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <Name>ButtonBehavior</Name>
  <RequiredVariables />
  <Category>
    <Name>ButtonCategory</Name>
    <State>
      <Name>Enabled</Name>
    </State>
    <State>
      <Name>Disabled</Name>
    </State>
  </Category>
  <RequiredInstances />
</BehaviorSave>");

        // Act
        var result = _importLogic.ImportBehavior(filePath, saveProject: false);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("ButtonBehavior");
        result.Categories.Count.ShouldBe(1);
        result.Categories[0].Name.ShouldBe("ButtonCategory");
        result.Categories[0].States.Count.ShouldBe(2);
        result.RequiredVariables.Variables.Count.ShouldBe(0);
    }

    [Fact]
    public void ImportBehavior_ShouldDeserializeV1Behavior()
    {
        // Arrange — v1 format: VariableSave properties as child elements, detected by <Variable> tag
        var filePath = Path.Combine(_behaviorsDirectory, "MyV1Behavior.behx");
        File.WriteAllText(filePath,
@"<?xml version=""1.0"" encoding=""utf-8""?>
<BehaviorSave xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <Name>MyV1Behavior</Name>
  <RequiredVariables>
    <Name>Default</Name>
    <Variable>
      <Type>float</Type>
      <Name>Width</Name>
    </Variable>
  </RequiredVariables>
</BehaviorSave>");

        // Act
        var result = _importLogic.ImportBehavior(filePath, saveProject: false);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("MyV1Behavior");
        result.RequiredVariables.Variables.Count.ShouldBe(1);
        result.RequiredVariables.Variables[0].Name.ShouldBe("Width");
        result.RequiredVariables.Variables[0].Type.ShouldBe("float");
    }

    [Fact]
    public void ImportBehavior_ShouldDeserializeV2Behavior()
    {
        // Arrange — v2 format: VariableSave properties as XML attributes
        var filePath = Path.Combine(_behaviorsDirectory, "MyV2Behavior.behx");
        File.WriteAllText(filePath,
@"<?xml version=""1.0"" encoding=""utf-8""?>
<BehaviorSave xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <Name>MyV2Behavior</Name>
  <RequiredVariables>
    <Name>Default</Name>
    <Variable Type=""float"" Name=""Width"" />
  </RequiredVariables>
</BehaviorSave>");

        // Act
        var result = _importLogic.ImportBehavior(filePath, saveProject: false);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("MyV2Behavior");
        result.RequiredVariables.Variables.Count.ShouldBe(1);
        result.RequiredVariables.Variables[0].Name.ShouldBe("Width");
        result.RequiredVariables.Variables[0].Type.ShouldBe("float");
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
