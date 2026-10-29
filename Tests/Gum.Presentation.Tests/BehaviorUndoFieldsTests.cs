using CommunityToolkit.Mvvm.Messaging;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.ToolStates;
using Gum.Undo;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Behavior undo restores every field a <see cref="BehaviorSave"/> serializes, not only categories,
/// required variables and required instances (#7019).
/// </summary>
public class BehaviorUndoFieldsTests : BaseTestClass
{
    private readonly Mock<IUndoRenameLogic> _renameLogic = new Mock<IUndoRenameLogic>();
    private readonly BehaviorSave _behavior = new BehaviorSave { Name = "MyBehavior" };
    private readonly UndoManager _undoManager;

    public BehaviorUndoFieldsTests()
    {
        // The real rename logic gives the behavior its new name.
        _renameLogic.Setup(x => x.RenameBehavior(It.IsAny<BehaviorSave>(), It.IsAny<string>()))
            .Callback<BehaviorSave, string>((behavior, newName) => behavior.Name = newName);
        Mock<ISelectedState> selectedState = new Mock<ISelectedState>();
        selectedState.Setup(x => x.SelectedBehavior).Returns(_behavior);
        Mock<IReferenceFinderProjectProvider> projectProvider = new Mock<IReferenceFinderProjectProvider>();
        projectProvider.Setup(x => x.GumProjectSave).Returns(new GumProjectSave());

        _undoManager = new UndoManager(
            selectedState.Object,
            _renameLogic.Object,
            new Mock<IGuiCommands>().Object,
            new Mock<IFileCommands>().Object,
            new Mock<IMessenger>().Object,
            new Mock<IUndoPluginNotifier>().Object,
            new Mock<IAnimationUndoProvider>().Object,
            projectProvider.Object,
            new Lazy<IVariableReferenceLogic>(() => new Mock<IVariableReferenceLogic>().Object));
    }

    [Fact]
    public void PerformUndoAndRedo_OnAChangeToEveryOtherField_RestoresEachField()
    {
        _undoManager.RecordBehaviorState();

        _behavior.FormsProperties.Add(new VariableSave { Name = "IsToggled", Type = "bool" });
        _behavior.ToolOnlyVariableReferences.Add("ButtonCategoryState = IsEnabled");
        _behavior.RequiredAnimations.Add("Pressed");
        _behavior.DefaultImplementation = "Controls/Button";
        _undoManager.RecordBehaviorUndo();

        _undoManager.PerformUndo();
        _behavior.FormsProperties.ShouldBeEmpty();
        _behavior.ToolOnlyVariableReferences.ShouldBeEmpty();
        _behavior.RequiredAnimations.ShouldBeEmpty();
        _behavior.DefaultImplementation.ShouldBeNull();

        _undoManager.PerformRedo();
        _behavior.FormsProperties.Select(item => item.Name).ShouldBe(new[] { "IsToggled" });
        _behavior.ToolOnlyVariableReferences.ShouldBe(new[] { "ButtonCategoryState = IsEnabled" });
        _behavior.RequiredAnimations.ShouldBe(new[] { "Pressed" });
        _behavior.DefaultImplementation.ShouldBe("Controls/Button");
    }

    [Fact]
    public void PerformUndoAndRedo_OnARename_AsksTheRenameLogicForTheOtherName()
    {
        _undoManager.RecordBehaviorState();

        _behavior.Name = "Renamed";
        _undoManager.RecordBehaviorUndo();

        _undoManager.PerformUndo();
        _renameLogic.Verify(x => x.RenameBehavior(_behavior, "MyBehavior"), Times.Once);

        _undoManager.PerformRedo();
        _renameLogic.Verify(x => x.RenameBehavior(_behavior, "Renamed"), Times.Once);
    }

    [Fact]
    public void GetBehaviorActionDescription_OnARename_NamesBothNames()
    {
        BehaviorHistoryAction action = new BehaviorHistoryAction
        {
            UndoState = new BehaviorSnapshot { Behavior = new BehaviorSave { Name = "Old" } },
            RedoState = new BehaviorSnapshot { Behavior = new BehaviorSave { Name = "New" } }
        };

        BehaviorUndoStrategy.GetBehaviorActionDescription(action).ShouldBe("Rename behavior: Old to New");
    }
}
