using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Moq;
using Shouldly;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Tests for <see cref="CodeGenerator.GenerateStandardElementsFallbackCode"/>: the per-project
/// codegen that embeds a project's Standard Element definitions as XML and registers them with
/// <see cref="Gum.Managers.ObjectFinder.RegisterFallbackStandardElements"/> via a
/// <c>[ModuleInitializer]</c>, so Standard-Element-owned category/state assignments (e.g. a
/// NineSlice's ColorCategoryState) still work in code-only games (issue #3505).
/// </summary>
public class CodeGeneratorStandardElementsFallbackTests : BaseTestClass
{
    private static CodeGenerator CreateCodeGenerator()
    {
        Mock<INameVerifier> mockNameVerifier = new Mock<INameVerifier>();
        string whyNotValid;
        CommonValidationError error;
        mockNameVerifier
            .Setup(v => v.IsValidCSharpName(It.IsAny<string>(), out whyNotValid, out error))
            .Returns(true);
        CodeGenerationNameVerifier codeGenNameVerifier = new CodeGenerationNameVerifier(mockNameVerifier.Object);
        FixedProjectDirectoryProvider directoryProvider = new FixedProjectDirectoryProvider(projectDirectory: null);
        CodeOutputElementSettingsManager elementSettingsManager = new CodeOutputElementSettingsManager(directoryProvider);
        LocalizationService localizationService = new LocalizationService();

        return new CodeGenerator(
            codeGenNameVerifier,
            localizationService,
            elementSettingsManager,
            directoryProvider);
    }

    private static void AddNineSliceWithColorCategory(GumProjectSave project)
    {
        StandardElementSave nineSlice = project.StandardElements.Find(item => item.Name == "NineSlice")!;

        StateSaveCategory colorCategory = new StateSaveCategory { Name = "ColorCategory" };
        nineSlice.Categories.Add(colorCategory);

        StateSave darkGrayState = new StateSave { Name = "DarkGray", ParentContainer = nineSlice };
        foreach (string channel in new[] { "Red", "Green", "Blue" })
        {
            darkGrayState.Variables.Add(new VariableSave
            {
                Name = channel,
                Type = "int",
                Value = 64,
                SetsValue = true
            });
        }
        colorCategory.States.Add(darkGrayState);
    }

    [Theory]
    [InlineData(OutputLibrary.XamarinForms)]
    [InlineData(OutputLibrary.WPF)]
    [InlineData(OutputLibrary.Maui)]
    public void GenerateStandardElementsFallbackCode_ForNonGumOutputLibrary_ReturnsNull(OutputLibrary outputLibrary)
    {
        GumProjectSave project = Project;
        AddNineSliceWithColorCategory(project);

        string? code = CreateCodeGenerator().GenerateStandardElementsFallbackCode(
            project,
            new CodeOutputProjectSettings { OutputLibrary = outputLibrary, RootNamespace = "MyGame" });

        code.ShouldBeNull();
    }

    [Fact]
    public void GenerateStandardElementsFallbackCode_ForMonoGame_ContainsModuleInitializerAndEmbeddedStandardElement()
    {
        GumProjectSave project = Project;
        AddNineSliceWithColorCategory(project);

        string? code = CreateCodeGenerator().GenerateStandardElementsFallbackCode(
            project,
            new CodeOutputProjectSettings { OutputLibrary = OutputLibrary.MonoGame, RootNamespace = "MyGame" });

        code.ShouldNotBeNull();
        code.ShouldContain("[ModuleInitializer]");
        code.ShouldContain("ObjectFinder.Self.RegisterFallbackStandardElements(");
        code.ShouldContain("NineSlice");
        code.ShouldContain("Color");
        code.ShouldContain("DarkGray");
    }

    [Fact]
    public void GenerateStandardElementsFallbackCode_EmbeddedXml_DeserializesBackToEquivalentStandardElement()
    {
        GumProjectSave project = Project;
        AddNineSliceWithColorCategory(project);

        string? code = CreateCodeGenerator().GenerateStandardElementsFallbackCode(
            project,
            new CodeOutputProjectSettings { OutputLibrary = OutputLibrary.MonoGame, RootNamespace = "MyGame" });

        code.ShouldNotBeNull();

        // Extract the embedded verbatim string literal the same way the generated
        // [ModuleInitializer] method reads it, then deserialize it exactly as that method would -
        // proving the escaping in GenerateStandardElementsFallbackCode round-trips correctly
        // without needing to actually compile/run the generated C#.
        const string marker = "string xml = @\"";
        int startIndex = code!.IndexOf(marker) + marker.Length;
        int endIndex = code.IndexOf("\";" + System.Environment.NewLine, startIndex);
        string escapedXml = code.Substring(startIndex, endIndex - startIndex);
        string xml = escapedXml.Replace("\"\"", "\"");

        XmlSerializer serializer = GumFileSerializer.GetCompactSerializer(typeof(List<StandardElementSave>));
        using StringReader reader = new StringReader(xml);
        List<StandardElementSave> standards = (List<StandardElementSave>)serializer.Deserialize(reader)!;

        StandardElementSave? nineSlice = standards.Find(item => item.Name == "NineSlice");
        nineSlice.ShouldNotBeNull();

        StateSaveCategory? colorCategory = nineSlice.Categories.Find(item => item.Name == "ColorCategory");
        colorCategory.ShouldNotBeNull();

        StateSave? darkGrayState = colorCategory.States.Find(item => item.Name == "DarkGray");
        darkGrayState.ShouldNotBeNull();
        darkGrayState.Variables.Single(item => item.Name == "Red").Value.ShouldBe(64);
        darkGrayState.Variables.Single(item => item.Name == "Green").Value.ShouldBe(64);
        darkGrayState.Variables.Single(item => item.Name == "Blue").Value.ShouldBe(64);
    }
}
