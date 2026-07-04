using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Moq;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Regression for a Screen with no BaseType, code-generated FullyInCode against the unified
/// runtime (OutputLibrary.MonoGame, which KNI/FNA projects also select): GetInheritance resolves
/// such a Screen to bare Gum.Wireframe.GraphicalUiElement (see
/// CodeGeneratorGetInheritanceTests.GetInheritance_MonoGame_Screen_NoBaseType_NoDefaultScreenBase_ReturnsGraphicalUiElement),
/// which never assigns itself a contained renderable. The constructor generator only emitted
/// SetContainedObject(new InvisibleRenderable()) when element.BaseType == "Container", which a
/// Screen's BaseType never is - so the emitted constructor called AssignParents() (and thus
/// this.AddChild(...)) while Children was still the read-only GraphicalUiElementCollection.Empty
/// singleton, throwing "Cannot modify the empty collection" at runtime.
/// </summary>
public class CodeGeneratorScreenContainedObjectTests : BaseTestClass
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

    private static CodeOutputProjectSettings CreateMonoGame() => new CodeOutputProjectSettings
    {
        OutputLibrary = OutputLibrary.MonoGame,
        RootNamespace = "MyGame",
    };

    [Fact]
    public void FullyInCode_MonoGame_ScreenWithNoBaseTypeAndChildInstance_AssignsContainedObjectBeforeAssigningParents()
    {
        GumProjectSave project = Project;

        ScreenSave mainMenu = new ScreenSave { Name = "MainMenuUI" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = mainMenu };
        mainMenu.States.Add(defaultState);
        InstanceSave textInstance = new InstanceSave
        {
            Name = "TextInstance",
            BaseType = "Text",
            ParentContainer = mainMenu,
        };
        mainMenu.Instances.Add(textInstance);
        project.Screens.Add(mainMenu);

        ObjectFinder.Self.GumProjectSave = project;
        try
        {
            string code = CreateCodeGenerator().GetGeneratedCodeForElement(
                mainMenu, elementSettings: null!, CreateMonoGame());

            // The Screen has no BaseType, so it inherits bare GraphicalUiElement and must set its
            // own contained renderable before AssignParents() touches Children.
            code.ShouldContain("SetContainedObject(new InvisibleRenderable());");
            int containedObjectIndex = code.IndexOf("SetContainedObject(new InvisibleRenderable());");
            int assignParentsCallIndex = code.IndexOf("AssignParents();");
            containedObjectIndex.ShouldBeGreaterThanOrEqualTo(0);
            assignParentsCallIndex.ShouldBeGreaterThanOrEqualTo(0);
            containedObjectIndex.ShouldBeLessThan(assignParentsCallIndex);
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }

    [Fact]
    public void FullyInCode_MonoGame_ScreenWithChildInstance_AddsDirectlyToChildrenWithoutDeadNullCheck()
    {
        // The generated "if(this.Children != null) ... else this.WhatThisContains.Add(...)" guard
        // was dead: Children is a property that always returns a collection (real or the read-only
        // Empty singleton) and is never itself null, so the else branch could never run - it was a
        // (non-functional) attempt to guard against exactly the Empty-collection crash fixed above.
        // Now that a contained object is always assigned first, this can be a plain, unconditional
        // Children.Add(...) - matching what non-Screen elements already emit (see the "else" branch
        // a few lines below this one in CodeGenerator.FillWithParentAssignments).
        GumProjectSave project = Project;

        ScreenSave mainMenu = new ScreenSave { Name = "MainMenuUI" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = mainMenu };
        mainMenu.States.Add(defaultState);
        InstanceSave textInstance = new InstanceSave
        {
            Name = "TextInstance",
            BaseType = "Text",
            ParentContainer = mainMenu,
        };
        mainMenu.Instances.Add(textInstance);
        project.Screens.Add(mainMenu);

        ObjectFinder.Self.GumProjectSave = project;
        try
        {
            string code = CreateCodeGenerator().GetGeneratedCodeForElement(
                mainMenu, elementSettings: null!, CreateMonoGame());

            code.ShouldContain("this.Children.Add(TextInstance);");
            code.ShouldNotContain("this.Children != null");
            code.ShouldNotContain("this.WhatThisContains.Add(TextInstance);");
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }
}
