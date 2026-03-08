using Gum.ProjectServices;
using Shouldly;

namespace Gum.ProjectServices.Tests;

public class ProjectLoaderTests
{
    private readonly ProjectLoader _sut;

    public ProjectLoaderTests()
    {
        _sut = new ProjectLoader();
    }

    [Fact]
    public void Load_ShouldLoadComponent_WhenReferenceIsCompactFormat()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "GumLoaderTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        string gumxPath = Path.Combine(tempDir, "Test.gumx");
        try
        {
            ProjectCreator creator = new ProjectCreator();
            creator.Create(gumxPath);

            string componentDir = Path.Combine(tempDir, "Components");
            string componentPath = Path.Combine(componentDir, "TestComp.gucx");
            File.WriteAllText(componentPath, """
                <?xml version="1.0" encoding="utf-8"?>
                <ComponentSave xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
                  <Name>TestComp</Name>
                  <BaseType>Container</BaseType>
                </ComponentSave>
                """);

            string gumxContent = File.ReadAllText(gumxPath);
            const string componentRef = """  <ComponentReference Name="TestComp" />""";
            gumxContent = gumxContent.Replace("</GumProjectSave>", componentRef + "\n</GumProjectSave>");
            File.WriteAllText(gumxPath, gumxContent);

            ProjectLoadResult result = _sut.Load(gumxPath);

            result.Success.ShouldBeTrue();
            result.Project!.Components.Count.ShouldBe(1);
            result.Project.Components[0].Name.ShouldBe("TestComp");
            result.Project.Components[0].Instances.Count.ShouldBe(0);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Load_ShouldNotReportError_WhenBehaviorFileUsesInstanceSaveInRequiredInstances()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "GumLoaderTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        string gumxPath = Path.Combine(tempDir, "Test.gumx");
        try
        {
            ProjectCreator creator = new ProjectCreator();
            creator.Create(gumxPath);

            string behaviorsDir = Path.Combine(tempDir, "Behaviors");
            Directory.CreateDirectory(behaviorsDir);
            File.WriteAllText(Path.Combine(behaviorsDir, "ToggleBehavior.behx"), """
                <?xml version="1.0" encoding="utf-8"?>
                <BehaviorSave xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
                  <Name>ToggleBehavior</Name>
                  <RequiredInstances>
                    <InstanceSave>
                      <Name>ToggleSprite</Name>
                      <BaseType>Sprite</BaseType>
                    </InstanceSave>
                  </RequiredInstances>
                </BehaviorSave>
                """);

            string gumxContent = File.ReadAllText(gumxPath);
            gumxContent = gumxContent.Replace("</GumProjectSave>",
                """  <BehaviorReference Name="ToggleBehavior" />""" + "\n</GumProjectSave>");
            File.WriteAllText(gumxPath, gumxContent);

            ProjectLoadResult result = _sut.Load(gumxPath);

            result.Success.ShouldBeTrue();
            result.LoadErrors.ShouldNotContain(e => e.ElementName == "ToggleBehavior");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Load_ShouldPreserveComponentInstances_WhenComponentFileHasInstances()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "GumLoaderTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        string gumxPath = Path.Combine(tempDir, "Test.gumx");
        try
        {
            ProjectCreator creator = new ProjectCreator();
            creator.Create(gumxPath);

            string componentDir = Path.Combine(tempDir, "Components");
            string componentPath = Path.Combine(componentDir, "BrokenComponent.gucx");
            File.WriteAllText(componentPath, """
                <?xml version="1.0" encoding="utf-8"?>
                <ComponentSave xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
                  <Name>BrokenComponent</Name>
                  <BaseType>Container</BaseType>
                  <Instance>
                    <Name>BadChild</Name>
                    <BaseType>NonExistentType</BaseType>
                  </Instance>
                </ComponentSave>
                """);

            string gumxContent = File.ReadAllText(gumxPath);
            const string componentRef = """  <ComponentReference Name="BrokenComponent" />""";
            gumxContent = gumxContent.Replace("</GumProjectSave>", componentRef + "\n</GumProjectSave>");
            File.WriteAllText(gumxPath, gumxContent);

            ProjectLoadResult result = _sut.Load(gumxPath);

            result.Success.ShouldBeTrue();
            result.Project!.Components.Count.ShouldBe(1);
            result.Project.Components[0].Name.ShouldBe("BrokenComponent");
            result.Project.Components[0].Instances.Count.ShouldBe(1);
            result.Project.Components[0].Instances[0].BaseType.ShouldBe("NonExistentType");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Load_ShouldReportError_WhenComponentFileUsesWrongInstanceElementName()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "GumLoaderTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        string gumxPath = Path.Combine(tempDir, "Test.gumx");
        try
        {
            ProjectCreator creator = new ProjectCreator();
            creator.Create(gumxPath);

            string componentDir = Path.Combine(tempDir, "Components");
            File.WriteAllText(Path.Combine(componentDir, "BadComponent.gucx"), """
                <?xml version="1.0" encoding="utf-8"?>
                <ComponentSave xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
                  <Name>BadComponent</Name>
                  <BaseType>Container</BaseType>
                  <Instances>
                    <InstanceSave>
                      <Name>Child</Name>
                      <BaseType>Sprite</BaseType>
                    </InstanceSave>
                  </Instances>
                </ComponentSave>
                """);

            string gumxContent = File.ReadAllText(gumxPath);
            gumxContent = gumxContent.Replace("</GumProjectSave>",
                """  <ComponentReference Name="BadComponent" />""" + "\n</GumProjectSave>");
            File.WriteAllText(gumxPath, gumxContent);

            ProjectLoadResult result = _sut.Load(gumxPath);

            result.Success.ShouldBeTrue();
            result.LoadErrors.ShouldContain(e =>
                e.ElementName == "BadComponent" &&
                e.Severity == ErrorSeverity.Error &&
                e.Message.Contains("InstanceSave"));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Load_ShouldReportError_WhenBehaviorFileUsesBehaviorInstanceSaveElement()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "GumLoaderTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        string gumxPath = Path.Combine(tempDir, "Test.gumx");
        try
        {
            ProjectCreator creator = new ProjectCreator();
            creator.Create(gumxPath);

            string behaviorsDir = Path.Combine(tempDir, "Behaviors");
            File.WriteAllText(Path.Combine(behaviorsDir, "BadBehavior.behx"), """
                <?xml version="1.0" encoding="utf-8"?>
                <BehaviorSave xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
                  <Name>BadBehavior</Name>
                  <RequiredInstances>
                    <BehaviorInstanceSave>
                      <Name>RequiredChild</Name>
                      <BaseType>Sprite</BaseType>
                    </BehaviorInstanceSave>
                  </RequiredInstances>
                </BehaviorSave>
                """);

            string gumxContent = File.ReadAllText(gumxPath);
            gumxContent = gumxContent.Replace("</GumProjectSave>",
                """  <BehaviorReference Name="BadBehavior" />""" + "\n</GumProjectSave>");
            File.WriteAllText(gumxPath, gumxContent);

            ProjectLoadResult result = _sut.Load(gumxPath);

            result.Success.ShouldBeTrue();
            result.LoadErrors.ShouldContain(e =>
                e.ElementName == "BadBehavior" &&
                e.Severity == ErrorSeverity.Error &&
                e.Message.Contains("BehaviorInstanceSave"));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Load_ShouldReportError_WhenComponentFileUsesWrappedCategoriesElement()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "GumLoaderTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        string gumxPath = Path.Combine(tempDir, "Test.gumx");
        try
        {
            ProjectCreator creator = new ProjectCreator();
            creator.Create(gumxPath);

            string componentDir = Path.Combine(tempDir, "Components");
            File.WriteAllText(Path.Combine(componentDir, "BadComponent.gucx"), """
                <?xml version="1.0" encoding="utf-8"?>
                <ComponentSave xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
                  <Name>BadComponent</Name>
                  <BaseType>Container</BaseType>
                  <Categories>
                    <StateSaveCategory>
                      <Name>MyCategory</Name>
                    </StateSaveCategory>
                  </Categories>
                </ComponentSave>
                """);

            string gumxContent = File.ReadAllText(gumxPath);
            gumxContent = gumxContent.Replace("</GumProjectSave>",
                """  <ComponentReference Name="BadComponent" />""" + "\n</GumProjectSave>");
            File.WriteAllText(gumxPath, gumxContent);

            ProjectLoadResult result = _sut.Load(gumxPath);

            result.Success.ShouldBeTrue();
            result.LoadErrors.ShouldContain(e =>
                e.ElementName == "BadComponent" &&
                e.Severity == ErrorSeverity.Error &&
                e.Message.Contains("Categories"));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Load_ShouldReportError_WhenComponentFileUsesWrappedStatesElement()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "GumLoaderTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        string gumxPath = Path.Combine(tempDir, "Test.gumx");
        try
        {
            ProjectCreator creator = new ProjectCreator();
            creator.Create(gumxPath);

            string componentDir = Path.Combine(tempDir, "Components");
            File.WriteAllText(Path.Combine(componentDir, "BadComponent.gucx"), """
                <?xml version="1.0" encoding="utf-8"?>
                <ComponentSave xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
                  <Name>BadComponent</Name>
                  <BaseType>Container</BaseType>
                  <States>
                    <StateSave>
                      <Name>Default</Name>
                    </StateSave>
                  </States>
                </ComponentSave>
                """);

            string gumxContent = File.ReadAllText(gumxPath);
            gumxContent = gumxContent.Replace("</GumProjectSave>",
                """  <ComponentReference Name="BadComponent" />""" + "\n</GumProjectSave>");
            File.WriteAllText(gumxPath, gumxContent);

            ProjectLoadResult result = _sut.Load(gumxPath);

            result.Success.ShouldBeTrue();
            result.LoadErrors.ShouldContain(e =>
                e.ElementName == "BadComponent" &&
                e.Severity == ErrorSeverity.Error &&
                e.Message.Contains("States"));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Load_ShouldReportError_WhenComponentFileUsesWrappedVariableListsElement()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "GumLoaderTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        string gumxPath = Path.Combine(tempDir, "Test.gumx");
        try
        {
            ProjectCreator creator = new ProjectCreator();
            creator.Create(gumxPath);

            string componentDir = Path.Combine(tempDir, "Components");
            File.WriteAllText(Path.Combine(componentDir, "BadComponent.gucx"), """
                <?xml version="1.0" encoding="utf-8"?>
                <ComponentSave xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
                  <Name>BadComponent</Name>
                  <BaseType>Container</BaseType>
                  <State>
                    <Name>Default</Name>
                    <VariableLists>
                      <VariableListSave>
                        <Name>Children</Name>
                      </VariableListSave>
                    </VariableLists>
                  </State>
                </ComponentSave>
                """);

            string gumxContent = File.ReadAllText(gumxPath);
            gumxContent = gumxContent.Replace("</GumProjectSave>",
                """  <ComponentReference Name="BadComponent" />""" + "\n</GumProjectSave>");
            File.WriteAllText(gumxPath, gumxContent);

            ProjectLoadResult result = _sut.Load(gumxPath);

            result.Success.ShouldBeTrue();
            result.LoadErrors.ShouldContain(e =>
                e.ElementName == "BadComponent" &&
                e.Severity == ErrorSeverity.Error &&
                e.Message.Contains("VariableLists"));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Load_ShouldReportError_WhenComponentFileUsesWrappedVariablesElement()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "GumLoaderTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        string gumxPath = Path.Combine(tempDir, "Test.gumx");
        try
        {
            ProjectCreator creator = new ProjectCreator();
            creator.Create(gumxPath);

            string componentDir = Path.Combine(tempDir, "Components");
            File.WriteAllText(Path.Combine(componentDir, "BadComponent.gucx"), """
                <?xml version="1.0" encoding="utf-8"?>
                <ComponentSave xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
                  <Name>BadComponent</Name>
                  <BaseType>Container</BaseType>
                  <State>
                    <Name>Default</Name>
                    <Variables>
                      <VariableSave>
                        <Name>X</Name>
                        <Value>10</Value>
                      </VariableSave>
                    </Variables>
                  </State>
                </ComponentSave>
                """);

            string gumxContent = File.ReadAllText(gumxPath);
            gumxContent = gumxContent.Replace("</GumProjectSave>",
                """  <ComponentReference Name="BadComponent" />""" + "\n</GumProjectSave>");
            File.WriteAllText(gumxPath, gumxContent);

            ProjectLoadResult result = _sut.Load(gumxPath);

            result.Success.ShouldBeTrue();
            result.LoadErrors.ShouldContain(e =>
                e.ElementName == "BadComponent" &&
                e.Severity == ErrorSeverity.Error &&
                e.Message.Contains("Variables"));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Load_ShouldReportError_WhenScreenFileUsesWrongInstanceElementName()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "GumLoaderTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        string gumxPath = Path.Combine(tempDir, "Test.gumx");
        try
        {
            ProjectCreator creator = new ProjectCreator();
            creator.Create(gumxPath);

            string screensDir = Path.Combine(tempDir, "Screens");
            Directory.CreateDirectory(screensDir);
            File.WriteAllText(Path.Combine(screensDir, "BadScreen.gusx"), """
                <?xml version="1.0" encoding="utf-8"?>
                <ScreenSave xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
                  <Name>BadScreen</Name>
                  <Instances>
                    <InstanceSave>
                      <Name>Child</Name>
                      <BaseType>Sprite</BaseType>
                    </InstanceSave>
                  </Instances>
                </ScreenSave>
                """);

            string gumxContent = File.ReadAllText(gumxPath);
            gumxContent = gumxContent.Replace("</GumProjectSave>",
                """  <ScreenReference Name="BadScreen" />""" + "\n</GumProjectSave>");
            File.WriteAllText(gumxPath, gumxContent);

            ProjectLoadResult result = _sut.Load(gumxPath);

            result.Success.ShouldBeTrue();
            result.LoadErrors.ShouldContain(e =>
                e.ElementName == "BadScreen" &&
                e.Severity == ErrorSeverity.Error &&
                e.Message.Contains("InstanceSave"));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Load_ShouldReturnError_WhenFileDoesNotExist()
    {
        ProjectLoadResult result = _sut.Load("C:/nonexistent/path/project.gumx");

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldContain("not found");
        result.Project.ShouldBeNull();
    }

    [Fact]
    public void Load_ShouldReturnError_WhenPathIsEmpty()
    {
        ProjectLoadResult result = _sut.Load("");

        result.Success.ShouldBeFalse();
        result.Project.ShouldBeNull();
    }

    [Fact]
    public void Load_ShouldReturnFatalError_WhenGumxFileIsMalformed()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "GumCliTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        string gumxPath = Path.Combine(tempDir, "Bad.gumx");
        try
        {
            File.WriteAllText(gumxPath, "<GumProjectSave><INVALID XML");

            ProjectLoadResult result = _sut.Load(gumxPath);

            result.Success.ShouldBeFalse();
            result.ErrorMessage.ShouldNotBeNullOrEmpty();
            result.Project.ShouldBeNull();
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Load_ShouldReturnSuccessTrue_WhenMissingComponentIsWarningNotError()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "GumLoaderTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        string gumxPath = Path.Combine(tempDir, "Test.gumx");
        try
        {
            ProjectCreator creator = new ProjectCreator();
            creator.Create(gumxPath);

            string gumxContent = File.ReadAllText(gumxPath);
            const string componentRef = """  <ComponentReference Name="Missing" />""";
            gumxContent = gumxContent.Replace("</GumProjectSave>", componentRef + "\n</GumProjectSave>");
            File.WriteAllText(gumxPath, gumxContent);

            ProjectLoadResult result = _sut.Load(gumxPath);

            result.Success.ShouldBeTrue();
            result.LoadErrors.ShouldContain(e => e.Message.Contains("not found"));
            result.LoadErrors.ShouldNotContain(e => e.Severity == ErrorSeverity.Error);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Load_ShouldPopulateLoadErrors_WhenElementFileIsMissing()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "GumCliTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        string screensDir = Path.Combine(tempDir, "Screens");
        Directory.CreateDirectory(screensDir);
        string gumxPath = Path.Combine(tempDir, "Test.gumx");
        try
        {
            // Create a .gumx that references a screen that doesn't exist on disk
            string gumxContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<GumProjectSave>
  <ScreenReference>
    <Name>MissingScreen</Name>
  </ScreenReference>
</GumProjectSave>";
            File.WriteAllText(gumxPath, gumxContent);

            ProjectLoadResult result = _sut.Load(gumxPath);

            result.Success.ShouldBeTrue();
            result.Project.ShouldNotBeNull();
            result.LoadErrors.ShouldContain(e =>
                e.ElementName == "MissingScreen" &&
                e.Message.Contains("not found"));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
