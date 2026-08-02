using Gum.Commands;
using Gum.Controls;
using Shouldly;
using System;
using System.Threading;

namespace GumToolUnitTests.Controls;

/// <summary>
/// The only production caller of <see cref="ISpinner.Hide"/> (<c>ToolFontGenerationCallbacks</c>)
/// runs on whichever thread a background Task continuation happens to resume on after font
/// generation, not necessarily the Spinner's owning thread (#4251/#4253 - a fast ProjectLoad
/// exposed this via a Window-affinity crash in the tool). <see cref="ISpinner.SetTotal"/> and
/// <see cref="ISpinner.IncrementProgress"/> already dispatcher-marshal for this reason; Hide()
/// previously called <c>Window.Hide()</c> directly, so calling it off the owning thread crashed.
/// </summary>
public class SpinnerTests
{
    [StaFact]
    public void Hide_CalledFromAnotherThread_DoesNotThrow()
    {
        Spinner spinner = new Spinner();
        ISpinner asInterface = spinner;

        Exception? caught = null;
        Thread otherThread = new Thread(() =>
        {
            try
            {
                asInterface.Hide();
            }
            catch (Exception ex)
            {
                caught = ex;
            }
        });
        // The Spinner's owning thread is this STA test thread; a plain background thread (not
        // apartment-bound) mirrors the Task-continuation thread that hit the crash.
        otherThread.Start();
        otherThread.Join();

        caught.ShouldBeNull();
    }
}
