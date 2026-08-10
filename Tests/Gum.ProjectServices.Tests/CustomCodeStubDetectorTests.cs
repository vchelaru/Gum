using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Moq;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Tests for <see cref="CustomCodeStubDetector"/>, which decides whether an element's custom
/// code file still holds nothing but the generated stub. A false negative only costs an extra
/// prompt; a false positive would silently recycle user-authored code, so the interesting cases
/// here are the ones that must come back false.
/// </summary>
public class CustomCodeStubDetectorTests : BaseTestClass
{
    private readonly CustomCodeStubDetector _detector = new();

    [Fact]
    public void IsUntouchedStub_ReturnsFalse_WhenCustomInitializeHasBody()
    {
        string contents = """
            namespace MyGame.Components
            {
                partial class MyComponent
                {
                    partial void CustomInitialize()
                    {
                        DoSomething();
                    }
                }
            }
            """;

        _detector.IsUntouchedStub(contents).ShouldBeFalse();
    }

    [Fact]
    public void IsUntouchedStub_ReturnsFalse_WhenExtraFieldExists()
    {
        string contents = """
            namespace MyGame.Components
            {
                partial class MyComponent
                {
                    private int _score;

                    partial void CustomInitialize()
                    {

                    }
                }
            }
            """;

        _detector.IsUntouchedStub(contents).ShouldBeFalse();
    }

    [Fact]
    public void IsUntouchedStub_ReturnsFalse_WhenExtraMethodExists()
    {
        string contents = """
            namespace MyGame.Components
            {
                partial class MyComponent
                {
                    partial void CustomInitialize()
                    {

                    }

                    public void Reset()
                    {
                    }
                }
            }
            """;

        _detector.IsUntouchedStub(contents).ShouldBeFalse();
    }

    [Fact]
    public void IsUntouchedStub_ReturnsTrue_ForCustomCodeGeneratorOutput()
    {
        // Pins the detector against the live template rather than a hand-written copy of it, so a
        // change to CustomCodeGenerator that stops matching shows up here instead of silently
        // turning every freshly-created stub into a prompt.
        ComponentSave component = new ComponentSave { Name = "MyComponent", BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });

        Mock<INameVerifier> nameVerifier = new();
        string whyNotValid;
        CommonValidationError error;
        nameVerifier
            .Setup(v => v.IsValidCSharpName(It.IsAny<string>(), out whyNotValid, out error))
            .Returns(true);

        CodeGenerationNameVerifier codeGenNameVerifier = new(nameVerifier.Object);
        FixedProjectDirectoryProvider directoryProvider = new(projectDirectory: null);
        CodeOutputElementSettingsManager elementSettingsManager = new(directoryProvider);
        CodeGenerator codeGenerator = new(
            codeGenNameVerifier,
            new LocalizationService(),
            elementSettingsManager,
            directoryProvider);
        CustomCodeGenerator customCodeGenerator = new(codeGenerator, codeGenNameVerifier);

        CodeOutputProjectSettings projectSettings = new CodeOutputProjectSettings
        {
            RootNamespace = "MyGame",
            OutputLibrary = OutputLibrary.MonoGameForms
        };

        string contents = customCodeGenerator.GetCustomCodeForElement(
            component, new CodeOutputElementSettings(), projectSettings);

        _detector.IsUntouchedStub(contents).ShouldBeTrue();
    }

    [Fact]
    public void IsUntouchedStub_ReturnsTrue_ForEmptyFile()
    {
        _detector.IsUntouchedStub("   \r\n  ").ShouldBeTrue();
    }

    [Fact]
    public void IsUntouchedStub_ReturnsTrue_ForFileScopedNamespaceWithBaseListAndComments()
    {
        string contents = """
            using System;
            using MonoGameGum.Forms.Controls;

            namespace MyGame.Components;

            // A comment the user added but no actual code
            public partial class MyComponent : Button
            {
                /* still empty */
                partial void CustomInitialize()
                {
                }
            }
            """;

        _detector.IsUntouchedStub(contents).ShouldBeTrue();
    }

    [Fact]
    public void IsUntouchedStub_ReturnsTrue_ForStubWithNoNamespace()
    {
        // RootNamespace empty means codegen emits no namespace at all.
        string contents = """
            partial class MyComponent
            {
                partial void CustomInitialize()
                {

                }
            }
            """;

        _detector.IsUntouchedStub(contents).ShouldBeTrue();
    }
}
