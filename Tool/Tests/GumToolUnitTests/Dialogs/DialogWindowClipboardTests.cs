using System;
using System.Runtime.InteropServices;
using Gum.Services.Dialogs;
using Shouldly;
using Xunit;

namespace GumToolUnitTests.Dialogs;

public class DialogWindowClipboardTests
{
    // CLIPBRD_E_CANT_OPEN — the transient HRESULT the real clipboard raises when another
    // process is holding it open. Issue #3368: this must never escape and crash the tool.
    private const int CLIPBRD_E_CANT_OPEN = unchecked((int)0x800401D0);

    private static COMException ClipboardBusy() => new("OpenClipboard Failed", CLIPBRD_E_CANT_OPEN);

    [Fact]
    public void TrySetClipboard_DoesNotThrowAndReturnsFalse_WhenEveryAttemptThrowsComException()
    {
        int attempts = 0;
        bool result = true;

        Should.NotThrow(() =>
            result = DialogWindow.TrySetClipboard(
                () => { attempts++; throw ClipboardBusy(); },
                retries: 4,
                delayMilliseconds: 0));

        result.ShouldBeFalse();
        attempts.ShouldBe(4);
    }

    [Fact]
    public void TrySetClipboard_ReturnsTrue_WhenOperationSucceedsImmediately()
    {
        int attempts = 0;

        bool result = DialogWindow.TrySetClipboard(() => attempts++, retries: 4, delayMilliseconds: 0);

        result.ShouldBeTrue();
        attempts.ShouldBe(1);
    }

    [Fact]
    public void TrySetClipboard_StopsAfterSuccess_WhenOperationSucceedsOnLaterAttempt()
    {
        int attempts = 0;

        bool result = DialogWindow.TrySetClipboard(
            () =>
            {
                attempts++;
                if (attempts < 3)
                {
                    throw ClipboardBusy();
                }
            },
            retries: 10,
            delayMilliseconds: 0);

        result.ShouldBeTrue();
        attempts.ShouldBe(3);
    }
}
