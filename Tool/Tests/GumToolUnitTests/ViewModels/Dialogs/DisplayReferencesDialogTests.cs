using Gum.DataTypes;
using Gum.Dialogs;
using Gum.Managers;
using Gum.ToolStates;
using Moq;
using Shouldly;

namespace GumToolUnitTests.ViewModels.Dialogs;

public class DisplayReferencesDialogTests : BaseTestClass
{
    private readonly Mock<ISelectedState> _selectedState;

    public DisplayReferencesDialogTests()
    {
        _selectedState = new Mock<ISelectedState>();

        // GetElementReferencesToThis reads ObjectFinder.Self.GumProjectSave directly,
        // so it must not be null when ElementSave is set.
        ObjectFinder.Self.GumProjectSave = new GumProjectSave();
    }

    private DisplayReferencesDialog CreateSut() => new(_selectedState.Object);

    [Fact]
    public void Message_RaisesPropertyChanged_WhenElementSaveChanges()
    {
        DisplayReferencesDialog sut = CreateSut();
        ScreenSave screen = new ScreenSave { Name = "MyScreen" };

        List<string> raisedPropertyNames = new List<string>();
        sut.PropertyChanged += (_, e) => raisedPropertyNames.Add(e.PropertyName!);

        sut.ElementSave = screen;

        raisedPropertyNames.ShouldContain(nameof(DisplayReferencesDialog.Message));
    }
}
