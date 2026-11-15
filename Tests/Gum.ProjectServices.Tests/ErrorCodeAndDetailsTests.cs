using Gum.Content.AnimationChain;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.ProjectServices;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Gum.StateAnimation.SaveClasses;
using Moq;
using RenderingLibrary.Graphics;
using Shouldly;
using ToolsUtilities;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Pins each checker problem's code and <see cref="ErrorDetails"/>, which tools read instead of the message (#7966).
/// </summary>
public class ErrorCodeAndDetailsTests : BaseTestClass
{
    private readonly HeadlessErrorChecker _sut;
    private readonly Mock<ITypeResolver> _mockTypeResolver;

    public ErrorCodeAndDetailsTests()
    {
        _mockTypeResolver = new Mock<ITypeResolver>();
        _sut = new HeadlessErrorChecker(_mockTypeResolver.Object);
    }

    #region HeadlessErrorChecker

    [Fact]
    public void GetErrorsFor_InstanceOfMissingType_HasGum0035AndDetails()
    {
        ComponentSave component = new ComponentSave { Name = "Holder", BaseType = "Container" };
        component.Instances.Add(new InstanceSave { Name = "Child", BaseType = "Nope" });
        Project.Components.Add(component);

        ErrorResult error = _sut.GetErrorsFor(component, Project).ShouldHaveSingleItem();

        error.Code.ShouldBe("GUM0035");
        TypeReferenceDetails details = error.Details.ShouldBeOfType<TypeReferenceDetails>();
        details.InstanceName.ShouldBe("Child");
        details.Type.ShouldBe("Nope");
    }

    [Fact]
    public void GetErrorsFor_MissingBehavior_HasGum0032AndDetails()
    {
        ComponentSave component = new ComponentSave { Name = "Holder", BaseType = "Container" };
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "IGone" });
        Project.Components.Add(component);

        ErrorResult error = _sut.GetErrorsFor(component, Project).ShouldHaveSingleItem();

        error.Code.ShouldBe("GUM0032");
        error.Details.ShouldBeOfType<MissingBehaviorDetails>().BehaviorName.ShouldBe("IGone");
    }

    [Fact]
    public void GetErrorsFor_BehaviorVariableMissing_HasGum0033AndDetails()
    {
        BehaviorSave behavior = new BehaviorSave { Name = "IToggle" };
        behavior.RequiredVariables.Variables.Add(new VariableSave { Name = "IsOn", Type = "bool" });
        Project.Behaviors.Add(behavior);
        ComponentSave component = new ComponentSave { Name = "Holder", BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "IToggle" });
        Project.Components.Add(component);

        ErrorResult error = _sut.GetErrorsFor(component, Project).ShouldHaveSingleItem();

        error.Code.ShouldBe("GUM0033");
        BehaviorVariableRequirementDetails details = error.Details.ShouldBeOfType<BehaviorVariableRequirementDetails>();
        details.BehaviorName.ShouldBe("IToggle");
        details.VariableName.ShouldBe("IsOn");
        details.RequiredType.ShouldBe("bool");
        details.ActualType.ShouldBeNull();
    }

    [Fact]
    public void GetErrorsFor_BehaviorVariableWrongType_HasGum0033AndActualType()
    {
        BehaviorSave behavior = new BehaviorSave { Name = "IToggle" };
        behavior.RequiredVariables.Variables.Add(new VariableSave { Name = "IsOn", Type = "bool" });
        Project.Behaviors.Add(behavior);
        ComponentSave component = new ComponentSave { Name = "Holder", BaseType = "Container" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
        defaultState.Variables.Add(new VariableSave { Name = "IsOn", Type = "int", Value = 0 });
        component.States.Add(defaultState);
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "IToggle" });
        Project.Components.Add(component);
        _mockTypeResolver.Setup(r => r.GetTypeFromString("int")).Returns(typeof(int));

        ErrorResult error = _sut.GetErrorsFor(component, Project).ShouldHaveSingleItem();

        error.Code.ShouldBe("GUM0033");
        BehaviorVariableRequirementDetails details = error.Details.ShouldBeOfType<BehaviorVariableRequirementDetails>();
        details.RequiredType.ShouldBe("bool");
        details.ActualType.ShouldBe("int");
    }

    [Fact]
    public void GetErrorsFor_ParentThatDoesNotExist_HasGum0034AndDetails()
    {
        ComponentSave component = new ComponentSave { Name = "Holder" };
        component.Instances.Add(new InstanceSave { Name = "ChildSprite", BaseType = "Sprite" });
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
        defaultState.Variables.Add(new VariableSave { Name = "ChildSprite.Parent", Value = "Ghost.Inner", Type = "string" });
        component.States.Add(defaultState);
        Project.Components.Add(component);

        ErrorResult error = _sut.GetErrorsFor(component, Project).ShouldHaveSingleItem();

        error.Code.ShouldBe("GUM0034");
        MissingParentDetails details = error.Details.ShouldBeOfType<MissingParentDetails>();
        details.InstanceName.ShouldBe("ChildSprite");
        details.ParentValue.ShouldBe("Ghost.Inner");
        details.StateName.ShouldBe("Default");
    }

    [Fact]
    public void GetErrorsFor_StateNameWithoutSuffix_HasGum0036AndSuggestedName()
    {
        ComponentSave component = new ComponentSave { Name = "Holder" };
        component.Categories.Add(new StateSaveCategory { Name = "ButtonMode" });
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
        defaultState.Variables.Add(new VariableSave { Name = "ButtonMode", Type = "ButtonMode" });
        component.States.Add(defaultState);
        Project.Components.Add(component);

        ErrorResult error = _sut.GetErrorsFor(component, Project).ShouldHaveSingleItem();

        error.Code.ShouldBe("GUM0036");
        StateSuffixDetails details = error.Details.ShouldBeOfType<StateSuffixDetails>();
        details.VariableName.ShouldBe("ButtonMode");
        details.VariableType.ShouldBe("ButtonMode");
        details.SuggestedVariableName.ShouldBe("ButtonModeState");
        details.SuggestedVariableType.ShouldBeNull();
    }

    [Fact]
    public void GetErrorsFor_AchxOffsetsWithNonCenterOrigin_HasGum0037AndDetails()
    {
        AnimationChainListSave achx = new AnimationChainListSave();
        AnimationChainSave chain = new AnimationChainSave { Name = "Chain1" };
        chain.Frames.Add(new AnimationFrameSave { TextureName = "tex.png", FrameLength = 0.1f, RelativeY = -3f });
        achx.AnimationChains.Add(chain);
        string achxPath = Path.Combine(Path.GetTempPath(), "Gum7966_" + Guid.NewGuid().ToString("N") + ".achx");
        FileManager.XmlSerialize(achx, achxPath);
        try
        {
            ComponentSave component = new ComponentSave { Name = "Animated" };
            component.Instances.Add(new InstanceSave { Name = "AnimatedSprite", BaseType = "Sprite" });
            StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
            defaultState.Variables.Add(new VariableSave { Name = "AnimatedSprite.SourceFile", Value = achxPath, Type = "string" });
            defaultState.Variables.Add(new VariableSave { Name = "AnimatedSprite.XOrigin", Value = HorizontalAlignment.Left, Type = nameof(HorizontalAlignment) });
            defaultState.Variables.Add(new VariableSave { Name = "AnimatedSprite.YOrigin", Value = VerticalAlignment.Top, Type = nameof(VerticalAlignment) });
            component.States.Add(defaultState);
            Project.Components.Add(component);

            ErrorResult error = _sut.GetErrorsFor(component, Project).ShouldHaveSingleItem();

            error.Code.ShouldBe("GUM0037");
            AnimationChainOriginDetails details = error.Details.ShouldBeOfType<AnimationChainOriginDetails>();
            details.InstanceName.ShouldBe("AnimatedSprite");
            details.SourceFile.ShouldBe(achxPath);
        }
        finally
        {
            File.Delete(achxPath);
        }
    }

    [Fact]
    public void GetErrorsFor_ElementContainingItself_HasGum0018AndPath()
    {
        ComponentSave component = new ComponentSave { Name = "Matryoshka", BaseType = "Container" };
        component.Instances.Add(new InstanceSave { Name = "Inner", BaseType = "Matryoshka" });
        Project.Components.Add(component);

        ErrorResult error = _sut.GetErrorsFor(component, Project).Single(e => e.Code == "GUM0018");

        error.Details.ShouldBeOfType<CycleDetails>().Path.ShouldBe(new[] { "Matryoshka", "Matryoshka" });
    }

    [Fact]
    public void GetErrorsFor_MissingExternalFile_HasGum0006Details()
    {
        string tempDirectory = Path.Combine(Path.GetTempPath(), "Gum7966_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        try
        {
            Project.FullFileName = Path.Combine(tempDirectory, "Project.gumx");
            ComponentSave component = SpriteHolder("Textures/DoesNotExist.png");

            ErrorResult error = _sut.GetErrorsFor(component, Project).ShouldHaveSingleItem();

            error.Code.ShouldBe("GUM0006");
            MissingExternalFileDetails details = error.Details.ShouldBeOfType<MissingExternalFileDetails>();
            details.ReferencedFrom.ShouldContain("MySprite");
            details.ReferencedPath.ShouldBe("Textures/DoesNotExist.png");
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void GetErrorsFor_FileNameCaseMismatch_HasGum0008Details()
    {
        string tempDirectory = Path.Combine(Path.GetTempPath(), "Gum7966_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(tempDirectory, "Textures"));
        try
        {
            Project.FullFileName = Path.Combine(tempDirectory, "Project.gumx");
            File.WriteAllText(Path.Combine(tempDirectory, "Textures", "hero.png"), string.Empty);
            ComponentSave component = SpriteHolder("Textures/Hero.png");

            ErrorResult error = _sut.GetErrorsFor(component, Project).ShouldHaveSingleItem();

            error.Code.ShouldBe("GUM0008");
            FileNameCaseMismatchDetails details = error.Details.ShouldBeOfType<FileNameCaseMismatchDetails>();
            details.ReferencedFrom.ShouldStartWith("SpriteHolder");
            details.ReferencedPath.ShouldBe("Textures/Hero.png");
            details.OnDiskPath.ShouldBe("Textures/hero.png");
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private ComponentSave SpriteHolder(string sourceFile)
    {
        ComponentSave component = new ComponentSave { Name = "SpriteHolder" };
        component.Instances.Add(new InstanceSave { Name = "MySprite", BaseType = "Sprite" });
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
        defaultState.Variables.Add(new VariableSave { Name = "MySprite.SourceFile", Value = sourceFile, Type = "string", IsFile = true });
        component.States.Add(defaultState);
        Project.Components.Add(component);
        return component;
    }

    #endregion

    #region Additional error sources

    [Fact]
    public void AnimationKeyframeErrorSource_UnreadableFile_HasGum0042()
    {
        ComponentSave element = new ComponentSave { Name = "Foo" };
        Mock<IElementAnimationsProvider> provider = new Mock<IElementAnimationsProvider>();
        provider.Setup(p => p.GetAnimationsFor(element, It.IsAny<GumProjectSave>())).Throws(new IOException("bad"));

        ErrorResult error = new AnimationKeyframeErrorSource(provider.Object).GetErrors(element, new GumProjectSave()).ShouldHaveSingleItem();

        error.Code.ShouldBe("GUM0042");
    }

    [Fact]
    public void AnimationKeyframeErrorSource_MissingState_HasGum0043AndDetails()
    {
        ComponentSave element = new ComponentSave { Name = "Foo" };
        AnimationSave animation = new AnimationSave { Name = "Blink" };
        animation.States.Add(new AnimatedStateSave { StateName = "Cat/Missing", Time = 0.5f });
        ElementAnimationsSave animations = new ElementAnimationsSave();
        animations.Animations.Add(animation);

        ErrorResult error = AnimationSource(element, animations).GetErrors(element, new GumProjectSave()).ShouldHaveSingleItem();

        error.Code.ShouldBe("GUM0043");
        KeyframeMissingStateDetails details = error.Details.ShouldBeOfType<KeyframeMissingStateDetails>();
        details.AnimationName.ShouldBe("Blink");
        details.Time.ShouldBe(0.5f);
        details.StateName.ShouldBe("Cat/Missing");
    }

    [Fact]
    public void AnimationKeyframeErrorSource_MissingSubAnimation_HasGum0044AndDetails()
    {
        ComponentSave element = new ComponentSave { Name = "Foo" };
        AnimationSave animation = new AnimationSave { Name = "Blink" };
        animation.Animations.Add(new AnimationReferenceSave { Name = "Gone", Time = 1f });
        ElementAnimationsSave animations = new ElementAnimationsSave();
        animations.Animations.Add(animation);

        ErrorResult error = AnimationSource(element, animations).GetErrors(element, new GumProjectSave()).ShouldHaveSingleItem();

        error.Code.ShouldBe("GUM0044");
        KeyframeMissingAnimationDetails details = error.Details.ShouldBeOfType<KeyframeMissingAnimationDetails>();
        details.AnimationName.ShouldBe("Blink");
        details.Time.ShouldBe(1f);
        details.MissingAnimationName.ShouldBe("Gone");
    }

    [Fact]
    public void CategoryNameCollisionErrorSource_HasGum0045()
    {
        ComponentSave component = new ComponentSave { Name = "Panel", BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        component.Categories.Add(new StateSaveCategory { Name = "Mode" });
        component.Categories.Add(new StateSaveCategory { Name = "ModeState" });
        CategoryNameCollisionErrorSource source = new CategoryNameCollisionErrorSource(
            CreateCodeGenerator(), new CodeOutputProjectSettings { OutputLibrary = OutputLibrary.MonoGame });

        ErrorResult error = source.GetErrors(component, Project).ShouldHaveSingleItem();

        error.Code.ShouldBe("GUM0045");
    }

    [Fact]
    public void FormsCodegenBaseTypeErrorSource_HasGum0046()
    {
        ObjectFinder.Self.GumProjectSave = Project;
        ComponentSave component = new ComponentSave { Name = "MyComponent", BaseType = "Sprite" };
        FormsCodegenBaseTypeErrorSource source = new FormsCodegenBaseTypeErrorSource(
            new CodeOutputProjectSettings { OutputLibrary = OutputLibrary.GumForms });

        ErrorResult error = source.GetErrors(component, Project).ShouldHaveSingleItem();

        error.Code.ShouldBe("GUM0046");
    }

    private static AnimationKeyframeErrorSource AnimationSource(ElementSave element, ElementAnimationsSave animations)
    {
        Mock<IElementAnimationsProvider> provider = new Mock<IElementAnimationsProvider>();
        provider.Setup(p => p.GetAnimationsFor(element, It.IsAny<GumProjectSave>())).Returns(animations);
        return new AnimationKeyframeErrorSource(provider.Object);
    }

    private static CodeGenerator CreateCodeGenerator()
    {
        Mock<INameVerifier> mockNameVerifier = new Mock<INameVerifier>();
        string? whyNotValid;
        CommonValidationError error;
        mockNameVerifier
            .Setup(v => v.IsValidCSharpName(It.IsAny<string>(), out whyNotValid, out error))
            .Returns(true);
        CodeGenerationNameVerifier codeGenNameVerifier = new CodeGenerationNameVerifier(mockNameVerifier.Object);
        FixedProjectDirectoryProvider directoryProvider = new FixedProjectDirectoryProvider(projectDirectory: null);
        return new CodeGenerator(codeGenNameVerifier, new Gum.Localization.LocalizationService(),
            new CodeOutputElementSettingsManager(directoryProvider), directoryProvider);
    }

    #endregion

    #region ProjectLoader

    [Fact]
    public void Load_ConflictMarkers_HasGum0041AndDetails()
    {
        LoadWithComponent("Conflicted", """
            <?xml version="1.0" encoding="utf-8"?>
            <ComponentSave>
              <Name>Conflicted</Name>
            <<<<<<< HEAD
              <BaseType>Container</BaseType>
            =======
              <BaseType>Sprite</BaseType>
            >>>>>>> other
            </ComponentSave>
            """, result =>
        {
            ErrorResult error = result.LoadErrors.Single(e => e.ElementName == "Conflicted");
            error.Code.ShouldBe("GUM0041");
            error.Details.ShouldBeOfType<ConflictMarkerDetails>().FileName.ShouldBe("Conflicted.gucx");
            error.FilePath.ShouldNotBeNull().ShouldEndWith("Conflicted.gucx");
        });
    }

    [Fact]
    public void Load_MalformedFile_HasGum0038AndDetails()
    {
        LoadWithComponent("Corrupt", """
            <?xml version="1.0" encoding="utf-8"?>
            <ComponentSave>
              <Name>Corrupt</Name>
              <BaseType>Container
            """, result =>
        {
            ErrorResult error = result.LoadErrors.Single(e => e.ElementName == "Corrupt");
            error.Code.ShouldBe("GUM0038");
            MalformedFileDetails details = error.Details.ShouldBeOfType<MalformedFileDetails>();
            details.Format.ShouldBe("XML");
            details.ParserMessage.ShouldNotBeNullOrEmpty();
        });
    }

    [Fact]
    public void Load_IgnoredXmlContent_HasGum0040AndDetails()
    {
        LoadWithComponent("Wrapped", """
            <?xml version="1.0" encoding="utf-8"?>
            <ComponentSave>
              <Name>Wrapped</Name>
              <BaseType>Container</BaseType>
              <States></States>
            </ComponentSave>
            """, result =>
        {
            ErrorResult error = result.LoadErrors.Single(e => e.ElementName == "Wrapped");
            error.Code.ShouldBe("GUM0040");
            error.Details.ShouldBeOfType<IgnoredContentDetails>().Content.ShouldBe("<States>");
        });
    }

    [Fact]
    public void Load_NameDifferingFromReference_HasGum0039()
    {
        LoadWithComponent("Renamed", """
            <?xml version="1.0" encoding="utf-8"?>
            <ComponentSave>
              <Name>OriginalName</Name>
              <BaseType>Container</BaseType>
            </ComponentSave>
            """, result =>
        {
            ErrorResult error = result.LoadErrors.Single(e => e.Message.Contains("has its name set to"));
            error.Code.ShouldBe("GUM0039");
        });
    }

    [Fact]
    public void Load_MissingElementFile_HasGum0004()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "Gum7966_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(tempDir, "Screens"));
        string gumxPath = Path.Combine(tempDir, "Test.gumx");
        try
        {
            File.WriteAllText(gumxPath, """
                <?xml version="1.0" encoding="utf-8"?>
                <GumProjectSave>
                  <ScreenReference>
                    <Name>MissingScreen</Name>
                  </ScreenReference>
                </GumProjectSave>
                """);

            ProjectLoadResult result = new ProjectLoader().Load(gumxPath);

            ErrorResult error = result.LoadErrors.Single(e => e.ElementName == "MissingScreen");
            error.Code.ShouldBe("GUM0004");
            error.Details.ShouldBeOfType<MissingFileDetails>().ExpectedPath.ShouldEndWith("MissingScreen.gusx");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Load_DerivedElementMissingBaseInstance_HasGum0016Details()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "Gum7966_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        string gumxPath = Path.Combine(tempDir, "Test.gumx");
        try
        {
            new ProjectCreator().Create(gumxPath);
            string componentDir = Path.Combine(tempDir, "Components");
            File.WriteAllText(Path.Combine(componentDir, "RepairBase.gucx"), """
                <?xml version="1.0" encoding="utf-8"?>
                <ComponentSave>
                  <Name>RepairBase</Name>
                  <BaseType>Container</BaseType>
                  <Instance><Name>A</Name><BaseType>Container</BaseType></Instance>
                </ComponentSave>
                """);
            File.WriteAllText(Path.Combine(componentDir, "RepairDerived.gucx"), """
                <?xml version="1.0" encoding="utf-8"?>
                <ComponentSave>
                  <Name>RepairDerived</Name>
                  <BaseType>RepairBase</BaseType>
                </ComponentSave>
                """);
            string gumxContent = File.ReadAllText(gumxPath).Replace("</GumProjectSave>",
                """  <ComponentReference Name="RepairBase" />""" + "\n" +
                """  <ComponentReference Name="RepairDerived" />""" + "\n</GumProjectSave>");
            File.WriteAllText(gumxPath, gumxContent);

            ProjectLoadResult result = new ProjectLoader().Load(gumxPath);

            ErrorResult error = result.LoadErrors.Single(e => e.Code == "GUM0016");
            MissingBaseInstanceDetails details = error.Details.ShouldBeOfType<MissingBaseInstanceDetails>();
            details.BaseElementName.ShouldBe("RepairBase");
            details.InstanceName.ShouldBe("A");
            details.InstanceType.ShouldBe("Container");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void JsonDroppedContentScanner_IgnoredProperty_HasGum0040AndDetails()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "Gum7966_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        Directory.CreateDirectory(Path.Combine(tempDir, ElementReference.ComponentSubfolder));
        try
        {
            File.WriteAllText(Path.Combine(tempDir, ElementReference.ComponentSubfolder, "Foo." + GumProjectSave.ComponentJsonExtension),
                """{ "Name": "Foo", "states": [] }""");
            GumProjectSave project = new GumProjectSave();
            project.ComponentReferences.Add(new ElementReference { Name = "Foo", ElementType = ElementType.Component });

            ErrorResult error = JsonDroppedContentScanner.Scan(project, tempDir).ShouldHaveSingleItem();
            error.Code.ShouldBe("GUM0040");
            error.Details.ShouldBeOfType<IgnoredContentDetails>().Content.ShouldBe("states");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    private static void LoadWithComponent(string referenceName, string fileContent, Action<ProjectLoadResult> assert)
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "Gum7966_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        string gumxPath = Path.Combine(tempDir, "Test.gumx");
        try
        {
            new ProjectCreator().Create(gumxPath);
            File.WriteAllText(Path.Combine(tempDir, "Components", referenceName + ".gucx"), fileContent);
            string gumxContent = File.ReadAllText(gumxPath).Replace("</GumProjectSave>",
                $"""  <ComponentReference Name="{referenceName}" />""" + "\n</GumProjectSave>");
            File.WriteAllText(gumxPath, gumxContent);

            assert(new ProjectLoader().Load(gumxPath));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    #endregion
}
