using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Gum.Avalonia.Panels;
using Gum.Plugins.Undos;
using Gum.ToolStates;
using Gum.Undo;
using Moq;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>The History tab shows the view model's entries, "No history" included, as the WPF UndoDisplay does.</summary>
public class UndosViewTests
{
    [AvaloniaFact]
    public void HistoryTab_ShowsNoHistory_ForAnElementWithoutUndos()
    {
        Mock<ISelectedState> selectedState = new Mock<ISelectedState>();
        Mock<IUndoManager> undoManager = new Mock<IUndoManager>();
        undoManager.Setup(manager => manager.CurrentElementHistory).Returns((ElementHistory?)null);
        UndosViewModel viewModel = new UndosViewModel(selectedState.Object, undoManager.Object);
        UndosView view = new UndosView { DataContext = viewModel };
        Window window = new Window { Content = view, Width = 300, Height = 200 };
        window.Show();
        window.UpdateLayout();

        // What the undo manager raises when the selection changes to an element with no history.
        undoManager.Raise(manager => manager.UndosChanged += null, undoManager.Object,
            new UndoOperationEventArgs { Operation = UndoOperation.EntireHistoryChange });
        window.UpdateLayout();

        view.GetVisualDescendants().OfType<TextBlock>().Select(text => text.Text).ShouldContain("No history");
        window.Close();
    }
}
