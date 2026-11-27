using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Moq;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// The custom code stub declares <c>GumService GumUI => GumService.Default;</c> so docs snippets
/// that use <c>GumUI</c> compile in a generated element's custom code file. Only output libraries
/// whose runtimes have a <c>GumService</c> get the line.
/// </summary>
public class CustomCodeGeneratorGumUITests : BaseTestClass
{
    private const string GumUILine = "GumService GumUI => GumService.Default;";

    [Fact]
    public void GetCustomCodeForElement_GumForms_DeclaresGumUIAndFormsUsings()
    {
        string stub = GetStub(OutputLibrary.GumForms, syntaxVersion: "3", commonUsings: "");

        stub.ShouldContain(GumUILine);
        GetLines(stub).ShouldContain("using Gum;");
        GetLines(stub).ShouldContain("using Gum.Forms.Controls;");
        stub.IndexOf(GumUILine).ShouldBeGreaterThan(stub.IndexOf("partial class Host"));
        stub.IndexOf(GumUILine).ShouldBeLessThan(stub.IndexOf("partial void CustomInitialize()"));
    }

    [Fact]
    public void GetCustomCodeForElement_GumForms_DoesNotRepeatUsingsAlreadyInCommonUsings()
    {
        string stub = GetStub(OutputLibrary.GumForms, syntaxVersion: "3",
            commonUsings: "using Gum;\r\nusing Gum.Forms.Controls;\r\n");

        GetLines(stub).Count(line => line == "using Gum;").ShouldBe(1);
        GetLines(stub).Count(line => line == "using Gum.Forms.Controls;").ShouldBe(1);
    }

    [Fact]
    public void GetCustomCodeForElement_GumFormsBelowVersion3_UsesLegacyGumServiceNamespace()
    {
        string stub = GetStub(OutputLibrary.GumForms, syntaxVersion: "0", commonUsings: "");

        stub.ShouldContain(GumUILine);
        GetLines(stub).ShouldContain("using MonoGameGum;");
        GetLines(stub).ShouldNotContain("using Gum;");
    }

    [Theory]
    [InlineData(OutputLibrary.MonoGame)]
    [InlineData(OutputLibrary.Raylib)]
    [InlineData(OutputLibrary.Silk)]
    public void GetCustomCodeForElement_UnifiedRuntime_DeclaresGumUIWithoutFormsUsing(OutputLibrary outputLibrary)
    {
        string stub = GetStub(outputLibrary, syntaxVersion: "3", commonUsings: "");

        stub.ShouldContain(GumUILine);
        GetLines(stub).ShouldContain("using Gum;");
        GetLines(stub).ShouldNotContain("using Gum.Forms.Controls;");
    }

    [Theory]
    [InlineData(OutputLibrary.Skia)]
    [InlineData(OutputLibrary.XamarinForms)]
    [InlineData(OutputLibrary.WPF)]
    [InlineData(OutputLibrary.Maui)]
    public void GetCustomCodeForElement_LibraryWithoutGumService_OmitsGumUI(OutputLibrary outputLibrary)
    {
        string stub = GetStub(outputLibrary, syntaxVersion: "3", commonUsings: "");

        stub.ShouldNotContain("GumUI");
        GetLines(stub).ShouldNotContain("using Gum;");
    }

    private string GetStub(OutputLibrary outputLibrary, string syntaxVersion, string commonUsings)
    {
        ComponentSave component = new ComponentSave { Name = "Widgets/Host", BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        Project.Components.Add(component);
        ObjectFinder.Self.GumProjectSave = Project;

        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            OutputLibrary = outputLibrary,
            ObjectInstantiationType = ObjectInstantiationType.FindByName,
            RootNamespace = "MyGame",
            CommonUsingStatements = commonUsings,
            SyntaxVersion = syntaxVersion,
        };

        Mock<INameVerifier> mockNameVerifier = new Mock<INameVerifier>();
        string whyNotValid;
        CommonValidationError error;
        mockNameVerifier
            .Setup(v => v.IsValidCSharpName(It.IsAny<string>(), out whyNotValid, out error))
            .Returns(true);
        CodeGenerationNameVerifier codeGenNameVerifier = new CodeGenerationNameVerifier(mockNameVerifier.Object);
        FixedProjectDirectoryProvider directoryProvider = new FixedProjectDirectoryProvider(projectDirectory: null);
        CodeGenerator generator = new CodeGenerator(
            codeGenNameVerifier,
            new LocalizationService(),
            new CodeOutputElementSettingsManager(directoryProvider),
            directoryProvider);
        CustomCodeGenerator customCodeGenerator = new CustomCodeGenerator(generator, codeGenNameVerifier);

        return customCodeGenerator.GetCustomCodeForElement(component, new CodeOutputElementSettings(), settings);
    }

    private static List<string> GetLines(string text) =>
        text.Split('\n').Select(line => line.Trim()).ToList();
}
