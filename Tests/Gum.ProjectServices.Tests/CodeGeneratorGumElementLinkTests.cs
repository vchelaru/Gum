using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Moq;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// From syntax version 7 a generated class names its element in a [GumElement] attribute, so tools
/// can read the link without running the module initializer, and it fetches its element with
/// ObjectFinder.GetRequiredElementSave, which throws the same way in Debug and Release.
/// </summary>
public class CodeGeneratorGumElementLinkTests : BaseTestClass
{
    private static CodeGenerator CreateCodeGenerator(int syntaxVersion)
    {
        Mock<INameVerifier> mockNameVerifier = new Mock<INameVerifier>();
        string? whyNotValid;
        CommonValidationError error;
        mockNameVerifier
            .Setup(v => v.IsValidCSharpName(It.IsAny<string>(), out whyNotValid, out error))
            .Returns(true);
        FixedProjectDirectoryProvider directoryProvider = new FixedProjectDirectoryProvider(projectDirectory: null);

        Mock<ISyntaxVersionDetectionService> detection = new Mock<ISyntaxVersionDetectionService>();
        detection
            .Setup(d => d.Detect(It.IsAny<CodeOutputProjectSettings>(), It.IsAny<string?>()))
            .Returns(new SyntaxVersionResult { Version = syntaxVersion });

        return new CodeGenerator(
            new CodeGenerationNameVerifier(mockNameVerifier.Object),
            new LocalizationService(),
            new CodeOutputElementSettingsManager(directoryProvider),
            directoryProvider,
            syntaxVersionDetectionService: detection.Object);
    }

    private string Generate(int syntaxVersion, OutputLibrary outputLibrary)
    {
        ComponentSave card = new ComponentSave { Name = "Cards/CardGum", BaseType = "Container" };
        card.States.Add(new StateSave { Name = "Default", ParentContainer = card });
        Project.Components.Add(card);
        ObjectFinder.Self.GumProjectSave = Project;

        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            OutputLibrary = outputLibrary,
            ObjectInstantiationType = ObjectInstantiationType.FindByName,
            RootNamespace = "MyGame",
        };

        return CreateCodeGenerator(syntaxVersion).GetGeneratedCodeForElement(card, new CodeOutputElementSettings(), settings);
    }

    [Theory]
    [InlineData(OutputLibrary.GumForms)]
    [InlineData(OutputLibrary.MonoGame)]
    public void GetGeneratedCodeForElement_SyntaxVersion7_LinksTheClassToItsElementAndRequiresIt(OutputLibrary outputLibrary)
    {
        string code = Generate(syntaxVersion: 7, outputLibrary);

        code.ShouldContain("[global::Gum.DataTypes.GumElement(\"Cards/CardGum\")]");
        code.ShouldContain("ObjectFinder.Self.GetRequiredElementSave(\"Cards/CardGum\")");
        code.ShouldNotContain("GetElementSave(\"Cards/CardGum\")");
    }

    [Fact]
    public void GetGeneratedCodeForElement_SyntaxVersion7_RecordsTheFormsTypeItRegistersFor()
    {
        Project.Behaviors.Add(new BehaviorSave
        {
            Name = StandardFormsBehaviorNames.ButtonBehaviorName,
            DefaultImplementation = "Controls/ButtonStandard",
        });
        ComponentSave button = new ComponentSave { Name = "Controls/ButtonStandard", BaseType = "Container" };
        button.States.Add(new StateSave { Name = "Default", ParentContainer = button });
        button.Behaviors.Add(new ElementBehaviorReference { BehaviorName = StandardFormsBehaviorNames.ButtonBehaviorName });
        Project.Components.Add(button);
        ObjectFinder.Self.GumProjectSave = Project;
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            OutputLibrary = OutputLibrary.GumForms,
            RootNamespace = "MyGame",
        };

        string code = CreateCodeGenerator(syntaxVersion: 7).GetGeneratedCodeForElement(button, new CodeOutputElementSettings(), settings);

        code.ShouldContain("[global::Gum.DataTypes.GumElement(\"Controls/ButtonStandard\", FormsType = typeof(global::Gum.Forms.Controls.Button))]");
    }

    [Fact]
    public void GetGeneratedCodeForElement_SyntaxVersion6_KeepsTheOldLookup()
    {
        string code = Generate(syntaxVersion: 6, OutputLibrary.GumForms);

        code.ShouldNotContain("GumElement(");
        code.ShouldContain("ObjectFinder.Self.GetElementSave(\"Cards/CardGum\") ?? throw");
    }
}
