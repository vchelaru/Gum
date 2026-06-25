using System;
using Gum.Undo;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Characterization tests for UndoLock after it moved into the headless Gum.Presentation
/// assembly (ADR-0005 Phase 3). Its back-edge to UndoManager was replaced with an injected
/// dispose callback; this project references ONLY Gum.Presentation (no WPF/WinForms), so a
/// green run proves the lock's disposal contract holds with no UI framework present.
/// </summary>
public class UndoLockTests
{
    [Fact]
    public void Dispose_InvokesOnDisposeCallback()
    {
        var disposeCount = 0;
        var undoLock = new UndoLock(() => disposeCount++);

        undoLock.Dispose();

        disposeCount.ShouldBe(1);
    }

    [Fact]
    public void Dispose_DoesNotThrow_WhenCallbackIsNull()
    {
        var undoLock = new UndoLock(null!);

        Should.NotThrow(() => undoLock.Dispose());
    }
}
