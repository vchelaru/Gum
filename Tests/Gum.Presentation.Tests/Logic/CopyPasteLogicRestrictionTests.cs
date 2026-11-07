using CommunityToolkit.Mvvm.Messaging;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.Messages;
using Gum.Services;
using Gum.ToolStates;
using Moq;
using Moq.AutoMock;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Gum.Presentation.Tests.Logic;

/// <summary>
/// Pasting honors the project's restrictions (#7720): unsupported types are refused and a pasted
/// instance does not get a parent when hierarchy is unsupported.
/// </summary>
public class CopyPasteLogicRestrictionTests : BaseTestClass
{
    private readonly AutoMocker _mocker = new AutoMocker();
    private readonly CopyPasteLogic _copyPasteLogic;
    private readonly ComponentSave _component;
    private readonly InstanceSave _container;
    private readonly InstanceSave _copied;
    private readonly Mock<IProjectRestrictions> _restrictions;

    public CopyPasteLogicRestrictionTests()
    {
        WeakReferenceMessenger messenger = new WeakReferenceMessenger();
        _mocker.Use<IMessenger>(messenger);
        AddDestinationTracker tracker = new AddDestinationTracker(messenger);
        _mocker.Use<IAddDestinationTracker>(tracker);

        GumProjectSave project = new GumProjectSave();
        StandardElementSave text = new StandardElementSave { Name = "Text" };
        text.States.Add(new StateSave { Name = "Default", ParentContainer = text });
        project.StandardElements.Add(text);
        ObjectFinder.Self.GumProjectSave = project;

        _component = new ComponentSave { Name = "MyComponent" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = _component };
        _component.States.Add(defaultState);

        _container = new InstanceSave { Name = "Container", BaseType = "Container", ParentContainer = _component };
        _copied = new InstanceSave { Name = "Copied", BaseType = "Text", ParentContainer = _component };
        _component.Instances.Add(_container);
        _component.Instances.Add(_copied);

        GumTreeNode componentsRoot = new GumTreeNode("Components");
        GumTreeNode elementNode = (GumTreeNode)componentsRoot.AddChild("MyComponent");
        elementNode.SetTag(_component);
        Mock<IElementTreeRoots> elementTreeRoots = _mocker.GetMock<IElementTreeRoots>();
        elementTreeRoots.Setup(x => x.Components).Returns(componentsRoot);
        _mocker.Use(new Lazy<IElementTreeRoots>(() => elementTreeRoots.Object));

        Mock<ISelectedState> selectedState = _mocker.GetMock<ISelectedState>();
        selectedState.Setup(x => x.SelectedElement).Returns(_component);
        selectedState.Setup(x => x.SelectedInstances).Returns(new List<InstanceSave> { _copied });
        selectedState.Setup(x => x.SelectedInstance).Returns(_copied);
        selectedState.Setup(x => x.SelectedStateSave).Returns(defaultState);
        selectedState.Setup(x => x.SelectedStateCategorySave).Returns((StateSaveCategory?)null);

        _mocker.Use<ICircularReferenceManager>(new CircularReferenceManager(ObjectFinder.Self));
        _restrictions = _mocker.GetMock<IProjectRestrictions>();
        _copyPasteLogic = _mocker.CreateInstance<CopyPasteLogic>();

        _copyPasteLogic.OnCopy(CopyType.InstanceOrElement);
        messenger.Send(new SelectionChangedMessage());
        tracker.Anchor(_container);
    }

    [Fact]
    public void OnPaste_ShouldPasteNothing_WhenTheTypeIsBlocked()
    {
        _restrictions.Setup(x => x.IsInstanceTypeBlocked(It.Is<ElementSave>(item => item.Name == "Text"))).Returns(true);

        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        _component.Instances.Count.ShouldBe(2);
    }

    [Fact]
    public void OnPaste_ShouldNotParent_WhenHierarchyIsUnsupported()
    {
        _restrictions.Setup(x => x.IsVariableHidden(It.IsAny<InstanceSave>(), "Parent")).Returns(true);

        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        InstanceSave pasted = _component.Instances.Single(i => i != _container && i != _copied);
        _component.DefaultState!.GetValue($"{pasted.Name}.Parent").ShouldBeNull();
    }
}
