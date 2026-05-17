using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Plugins.ImportPlugin.Services;
using Gum.RenderingLibrary;
using Shouldly;

namespace GumToolUnitTests.Plugins.ImportFromGumxPlugin;

/// <summary>
/// Reproduces issue #2810: GumxSourceService didn't coerce int-on-disk enum values
/// into their CLR enum types after deserialization. Without this fix-up, downstream
/// XML-string comparisons (see <c>StandardComparer</c>) produce false-positive
/// "Changed" diffs against destination standards that did go through the tool's
/// FixEnumerations pass.
/// </summary>
public class GumxSourceServiceEnumCoercionTests : IDisposable
{
    private readonly string _sourceDir;
    private readonly GumxSourceService _sut = new();

    public GumxSourceServiceEnumCoercionTests()
    {
        _sourceDir = Path.Combine(Path.GetTempPath(), $"GumxSourceEnumTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_sourceDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_sourceDir)) Directory.Delete(_sourceDir, recursive: true);
    }

    [Fact]
    public async Task LoadProjectAsync_StandardWithIntEnumValueOnDisk_CoercesValueToEnumType()
    {
        const string standardName = "TestContainer";
        WriteStandardWithIntBlend(standardName);
        string gumxPath = WriteGumxReferencing(standardName);

        GumProjectSave? loaded = await _sut.LoadProjectAsync(gumxPath);

        loaded.ShouldNotBeNull();
        StandardElementSave standard = loaded.StandardElements.Single(s => s.Name == standardName);
        VariableSave blend = standard.DefaultState.Variables.Single(v => v.Name == "Blend");
        blend.Value.ShouldBeOfType<Blend>();
        blend.Value.ShouldBe(Blend.Normal);
    }

    [Fact]
    public async Task LoadProjectAsync_ComponentWithIntEnumValueOnDisk_CoercesValueToEnumType()
    {
        const string componentName = "TestComponent";
        WriteComponentWithIntBlend(componentName);
        string gumxPath = WriteGumxReferencingComponent(componentName);

        GumProjectSave? loaded = await _sut.LoadProjectAsync(gumxPath);

        loaded.ShouldNotBeNull();
        ComponentSave component = loaded.Components.Single(c => c.Name == componentName);
        VariableSave blend = component.DefaultState.Variables.Single(v => v.Name == "Blend");
        blend.Value.ShouldBeOfType<Blend>();
        blend.Value.ShouldBe(Blend.Normal);
    }

    private void WriteStandardWithIntBlend(string standardName)
    {
        string standardsDir = Path.Combine(_sourceDir, "Standards");
        Directory.CreateDirectory(standardsDir);
        string gutxPath = Path.Combine(standardsDir, $"{standardName}.{GumProjectSave.StandardExtension}");
        File.WriteAllText(gutxPath, $@"<?xml version=""1.0"" encoding=""utf-8""?>
<StandardElementSave xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <Name>{standardName}</Name>
  <State>
    <Name>Default</Name>
    <Variable>
      <Type>Blend</Type>
      <Name>Blend</Name>
      <Value xsi:type=""xsd:int"">0</Value>
      <SetsValue>true</SetsValue>
    </Variable>
  </State>
</StandardElementSave>");
    }

    private void WriteComponentWithIntBlend(string componentName)
    {
        string componentsDir = Path.Combine(_sourceDir, "Components");
        Directory.CreateDirectory(componentsDir);
        string gucxPath = Path.Combine(componentsDir, $"{componentName}.{GumProjectSave.ComponentExtension}");
        File.WriteAllText(gucxPath, $@"<?xml version=""1.0"" encoding=""utf-8""?>
<ComponentSave xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <Name>{componentName}</Name>
  <State>
    <Name>Default</Name>
    <Variable>
      <Type>Blend</Type>
      <Name>Blend</Name>
      <Value xsi:type=""xsd:int"">0</Value>
      <SetsValue>true</SetsValue>
    </Variable>
  </State>
</ComponentSave>");
    }

    private string WriteGumxReferencing(string standardName)
    {
        string gumxPath = Path.Combine(_sourceDir, "Test.gumx");
        File.WriteAllText(gumxPath, $@"<?xml version=""1.0"" encoding=""utf-8""?>
<GumProjectSave xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <Version>1</Version>
  <StandardElementReference>
    <Name>{standardName}</Name>
    <ElementType>Standard</ElementType>
    <LinkType>ReferenceOriginal</LinkType>
  </StandardElementReference>
</GumProjectSave>");
        return gumxPath;
    }

    private string WriteGumxReferencingComponent(string componentName)
    {
        string gumxPath = Path.Combine(_sourceDir, "Test.gumx");
        File.WriteAllText(gumxPath, $@"<?xml version=""1.0"" encoding=""utf-8""?>
<GumProjectSave xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <Version>1</Version>
  <ComponentReference>
    <Name>{componentName}</Name>
    <ElementType>Component</ElementType>
    <LinkType>ReferenceOriginal</LinkType>
  </ComponentReference>
</GumProjectSave>");
        return gumxPath;
    }
}
