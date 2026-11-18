using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.ProjectServices;
using Gum.ProjectServices.Editing;
using Gum.Services;
using Gum.ToolStates;
using Gum.Undo;
using Moq;
using Moq.AutoMock;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Gum.Presentation.Tests.Logic;

/// <summary>
/// The tool's Create Component with "replace" checked, run through the shared
/// <see cref="ComponentExtractionLogic"/> and <see cref="MovedInstanceReferenceDropLogic"/> that gumcli's
/// extractToComponent also uses.
/// </summary>
public class CopyPasteLogicCreateComponentTests : BaseTestClass
{
    [Fact]
    public void CreateComponentFromInstance_WithReplace_RemovesThroughDeleteLogic_SelectsTheReplacement_AndReportsDrops()
    {
        AutoMocker mocker = new AutoMocker();
        GumProjectSave project = new GumProjectSave();
        ScreenSave screen = new ScreenSave { Name = "MainScreen" };
        StateSave screenDefault = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(screenDefault);
        project.Screens.Add(screen);
        ObjectFinder.Self.GumProjectSave = project;
        InstanceSave myButton = new InstanceSave { Name = "MyButton", BaseType = "Container", ParentContainer = screen };
        InstanceSave childText = new InstanceSave { Name = "ChildText", BaseType = "Text", ParentContainer = screen };
        InstanceSave caption = new InstanceSave { Name = "Caption", BaseType = "Text", ParentContainer = screen };
        screen.Instances.AddRange(new[] { myButton, childText, caption });
        screenDefault.SetValue("ChildText.Parent", "MyButton", "string");
        screenDefault.SetValue("MyButton.X", 50f, "float");
        screenDefault.SetValue("Caption.X", 5f, "float");
        VariableListSave<string> captionReferences = new VariableListSave<string> { Name = "Caption.VariableReferences", Type = "string" };
        captionReferences.ValueAsIList.Add("X = ChildText.X");
        screenDefault.VariableLists.Add(captionReferences);

        mocker.GetMock<IReferenceFinderProjectProvider>().Setup(x => x.GumProjectSave).Returns(project);
        mocker.Use<IReferenceFinder>(mocker.CreateInstance<ReferenceFinder>());
        mocker.Use<IMovedInstanceReferenceDropLogic>(mocker.CreateInstance<MovedInstanceReferenceDropLogic>());
        mocker.Use<IMovedInstanceReferenceDropper>(mocker.CreateInstance<MovedInstanceReferenceDropper>());
        mocker.Use<IComponentExtractionLogic>(new ComponentExtractionLogic(new ElementCreationLogic()));
        List<string> removed = new List<string>();
        InstanceRemovalLogic removal = new InstanceRemovalLogic(mocker.Get<IReferenceFinder>(), new VariableOverrideRemover());
        mocker.GetMock<IDeleteLogic>()
            .Setup(x => x.RemoveInstance(It.IsAny<InstanceSave>(), It.IsAny<ElementSave>()))
            .Callback((InstanceSave instance, ElementSave element) =>
            {
                removed.Add(instance.Name);
                removal.RemoveInstance(instance, element);
            });
        string output = "";
        mocker.GetMock<IOutputManager>().Setup(x => x.AddOutput(It.IsAny<string>())).Callback((string text) => output += text);
        CopyPasteLogic logic = mocker.CreateInstance<CopyPasteLogic>();

        ComponentSave component = logic.CreateComponentFromInstance(myButton, "ButtonComponent", replaceWithInstance: true);

        component.Instances.Select(item => item.Name).ShouldBe(new[] { "ChildText" });
        mocker.GetMock<ICopyPasteProjectCommands>().Verify(x => x.AddComponent(component), Times.Once);
        mocker.GetMock<IUndoManager>().Verify(x => x.RequestLock(), Times.Once);
        removed.ShouldBe(new[] { "ChildText", "MyButton" });
        screen.Instances.Select(item => item.Name).ShouldBe(new[] { "MyButton", "Caption" });
        InstanceSave replacement = screen.Instances[0];
        replacement.BaseType.ShouldBe("ButtonComponent");
        screenDefault.GetValue("MyButton.X").ShouldBe(50f);
        screenDefault.VariableLists.ShouldBeEmpty();
        screenDefault.GetValue("Caption.X").ShouldBe(5f);
        mocker.GetMock<ISelectedState>().VerifySet(x => x.SelectedInstance = replacement);
        output.ShouldContain("X = ChildText.X");
    }
}
