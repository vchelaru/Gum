using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Plugins;
using Gum.Services;
using Gum.Undo;
using Moq;
using Shouldly;
using System.Collections.Generic;
using System.Linq;

namespace Gum.Presentation.Tests.Services;

public class InstanceOverrideRemoverTests : BaseTestClass
{
    private readonly Mock<IUndoManager> _undoManager;
    private readonly Mock<IFileCommands> _fileCommands;
    private readonly Mock<IPluginManager> _pluginManager;
    private readonly InstanceOverrideRemover _remover;

    public InstanceOverrideRemoverTests()
    {
        _undoManager = new Mock<IUndoManager>();
        _fileCommands = new Mock<IFileCommands>();
        _pluginManager = new Mock<IPluginManager>();
        _remover = new InstanceOverrideRemover(_undoManager.Object, _fileCommands.Object, _pluginManager.Object);
    }

    [Fact]
    public void RemoveAndRecordForUndo_RemovesSavesNotifiesAndRecordsEachOverride()
    {
        ScreenSave screen = new ScreenSave { Name = "Title" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        InstanceSave okButton = new InstanceSave { Name = "OkButton", BaseType = "Button", ParentContainer = screen };
        screen.Instances.Add(okButton);
        VariableSave instanceValue = new VariableSave { Name = "OkButton.LabelText", Type = "string", Value = "OK" };
        screen.GetDefaultStateOrThrow().Variables.Add(instanceValue);
        List<CrossElementVariableChange>? recorded = null;
        _undoManager
            .Setup(x => x.RecordCrossElementVariableChanges(It.IsAny<IEnumerable<CrossElementVariableChange>>()))
            .Callback<IEnumerable<CrossElementVariableChange>>(changes => recorded = changes.ToList());

        _remover.RemoveAndRecordForUndo(new[]
        {
            new VariableChange { Container = screen, State = screen.GetDefaultStateOrThrow(), Variable = instanceValue }
        });

        screen.GetDefaultStateOrThrow().Variables.ShouldNotContain(instanceValue);
        _fileCommands.Verify(x => x.TryAutoSaveElement(screen), Times.Once);
        _pluginManager.Verify(x => x.VariableSet(screen, okButton, "LabelText", null, It.IsAny<bool>()), Times.Once);
        recorded.ShouldNotBeNull();
        recorded.Single().Container.ShouldBe(screen);
        recorded.Single().Before!.Name.ShouldBe("OkButton.LabelText");
        recorded.Single().After.ShouldBeNull();
    }

    [Fact]
    public void RemoveAndRecordForUndo_SkipsAnOverrideWhoseInstanceIsMissing_AndRecordsNothing()
    {
        ScreenSave screen = new ScreenSave { Name = "Title" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        VariableSave orphanValue = new VariableSave { Name = "Gone.LabelText", Type = "string", Value = "OK" };
        screen.GetDefaultStateOrThrow().Variables.Add(orphanValue);

        _remover.RemoveAndRecordForUndo(new[]
        {
            new VariableChange { Container = screen, State = screen.GetDefaultStateOrThrow(), Variable = orphanValue }
        });

        screen.GetDefaultStateOrThrow().Variables.ShouldContain(orphanValue);
        _undoManager.Verify(x => x.RecordCrossElementVariableChanges(It.IsAny<IEnumerable<CrossElementVariableChange>>()), Times.Never);
    }
}
