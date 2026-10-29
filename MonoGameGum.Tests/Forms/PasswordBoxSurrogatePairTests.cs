using Gum.Forms.Controls;
using Gum.Forms.Input;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// PasswordBox shares the caret and delete rules of TextBox (#7091): an emoji (a surrogate pair) is one
/// character. The mask shows one dot per UTF-16 char, so the rules read the password, not the dots.
/// </summary>
public class PasswordBoxSurrogatePairTests : BaseTestClass
{
    const string Emoji = "\U0001F600";

    [Fact]
    public void Backspace_AfterEmoji_ShouldRemoveWholePair()
    {
        PasswordBox passwordBox = CreatePasswordBox("a" + Emoji + "b");
        passwordBox.CaretIndex = 3;

        passwordBox.HandleBackspace();

        passwordBox.Password.ShouldBe("ab");
        passwordBox.CaretIndex.ShouldBe(1);
    }

    [Fact]
    public void Backspace_Ascii_ShouldRemoveOneCharacter()
    {
        PasswordBox passwordBox = CreatePasswordBox("abc");
        passwordBox.CaretIndex = 2;

        passwordBox.HandleBackspace();

        passwordBox.Password.ShouldBe("ac");
        passwordBox.CaretIndex.ShouldBe(1);
    }

    [Fact]
    public void Delete_BeforeEmoji_ShouldRemoveWholePair()
    {
        PasswordBox passwordBox = CreatePasswordBox("a" + Emoji + "b");
        passwordBox.CaretIndex = 1;

        passwordBox.HandleKeyDown(Keys.Delete, false, false, false);

        passwordBox.Password.ShouldBe("ab");
        passwordBox.CaretIndex.ShouldBe(1);
    }

    [Fact]
    public void Delete_Ascii_ShouldRemoveOneCharacter()
    {
        PasswordBox passwordBox = CreatePasswordBox("abc");
        passwordBox.CaretIndex = 1;

        passwordBox.HandleKeyDown(Keys.Delete, false, false, false);

        passwordBox.Password.ShouldBe("ac");
        passwordBox.CaretIndex.ShouldBe(1);
    }

    [Fact]
    public void Arrows_OverEmoji_ShouldStepOverWholePair()
    {
        PasswordBox passwordBox = CreatePasswordBox("a" + Emoji + "b");
        passwordBox.CaretIndex = 1;

        passwordBox.HandleKeyDown(Keys.Right, false, false, false);
        passwordBox.CaretIndex.ShouldBe(3);

        passwordBox.HandleKeyDown(Keys.Left, false, false, false);
        passwordBox.CaretIndex.ShouldBe(1);
    }

    [Fact]
    public void Arrows_Ascii_ShouldStepOneCharacter()
    {
        PasswordBox passwordBox = CreatePasswordBox("abc");
        passwordBox.CaretIndex = 1;

        passwordBox.HandleKeyDown(Keys.Right, false, false, false);
        passwordBox.CaretIndex.ShouldBe(2);

        passwordBox.HandleKeyDown(Keys.Left, false, false, false);
        passwordBox.CaretIndex.ShouldBe(1);
    }

    [Fact]
    public void ShiftRight_BeforeEmoji_ShouldSelectWholePair()
    {
        PasswordBox passwordBox = CreatePasswordBox("a" + Emoji + "b");
        passwordBox.CaretIndex = 1;

        passwordBox.HandleKeyDown(Keys.Right, true, false, false);

        passwordBox.SelectionStart.ShouldBe(1);
        passwordBox.SelectionLength.ShouldBe(2);
    }

    [Fact]
    public void DeleteSelection_EdgeInsideAPair_ShouldRemoveWholePair()
    {
        PasswordBox passwordBox = CreatePasswordBox("a" + Emoji + "b");
        passwordBox.SelectionStart = 2;
        passwordBox.SelectionLength = 1;

        passwordBox.DeleteSelection();

        passwordBox.Password.ShouldBe("ab");
        passwordBox.CaretIndex.ShouldBe(1);
    }

    [Fact]
    public void MaxLength_ThatCutsAPair_ShouldDropTheWholePair()
    {
        PasswordBox passwordBox = CreatePasswordBox("a" + Emoji + "b");

        passwordBox.MaxLength = 2;

        passwordBox.Password.ShouldBe("a");
    }

    [Fact]
    public void Backspace_AfterLoneHighSurrogate_ShouldRemoveOneUnit()
    {
        PasswordBox passwordBox = CreatePasswordBox("a\uD83D");
        passwordBox.CaretIndex = 2;

        passwordBox.HandleBackspace();

        passwordBox.Password.ShouldBe("a");
        passwordBox.CaretIndex.ShouldBe(1);
    }


    // ----- A caret between the halves of a character (#7100) -----

    [Fact]
    public void CtrlBackspace_CaretBetweenTheHalves_ShouldRemoveWholePair()
    {
        PasswordBox passwordBox = CreatePasswordBox("x a" + Emoji + "b");
        passwordBox.CaretIndex = 4;

        passwordBox.HandleBackspace(isCtrlDown: true);

        passwordBox.Password.ShouldBe("b");
        passwordBox.CaretIndex.ShouldBe(0);
    }

    [Fact]
    public void CtrlBackspace_Ascii_ShouldRemoveEverythingBeforeTheCaret()
    {
        PasswordBox passwordBox = CreatePasswordBox("abcd");
        passwordBox.CaretIndex = 3;

        passwordBox.HandleBackspace(isCtrlDown: true);

        passwordBox.Password.ShouldBe("d");
        passwordBox.CaretIndex.ShouldBe(0);
    }

    [Fact]
    public void Typing_CaretBetweenTheHalves_ShouldInsertBeforeThePair()
    {
        PasswordBox passwordBox = CreatePasswordBox("a" + Emoji + "b");
        passwordBox.CaretIndex = 2;

        passwordBox.HandleCharEntered('X');

        passwordBox.Password.ShouldBe("aX" + Emoji + "b");
        passwordBox.CaretIndex.ShouldBe(2);
    }

    [Fact]
    public void Paste_CaretBetweenTheHalves_ShouldInsertBeforeThePair()
    {
        Gum.Clipboard.ClipboardImplementation.PushStringToClipboard("X");
        PasswordBox passwordBox = CreatePasswordBox("a" + Emoji + "b");
        passwordBox.CaretIndex = 2;

        passwordBox.HandleKeyDown(Keys.V, false, false, true);

        passwordBox.Password.ShouldBe("aX" + Emoji + "b");
    }

    // ----- MaxLength while typing (#7100) -----

    [Fact]
    public void MaxLength_TypedEmojiThatDoesNotFit_ShouldBeRejectedWhole()
    {
        PasswordBox passwordBox = CreatePasswordBox("ab");
        passwordBox.MaxLength = 3;
        passwordBox.CaretIndex = 2;

        passwordBox.HandleCharEntered(Emoji[0]);
        passwordBox.HandleCharEntered(Emoji[1]);

        passwordBox.Password.ShouldBe("ab");
        passwordBox.CaretIndex.ShouldBe(2);
    }

    [Fact]
    public void MaxLength_TypedEmojiThatFits_ShouldBeInserted()
    {
        PasswordBox passwordBox = CreatePasswordBox("a");
        passwordBox.MaxLength = 3;
        passwordBox.CaretIndex = 1;

        passwordBox.HandleCharEntered(Emoji[0]);
        passwordBox.HandleCharEntered(Emoji[1]);

        passwordBox.Password.ShouldBe("a" + Emoji);
        passwordBox.CaretIndex.ShouldBe(3);
    }

    [Fact]
    public void MaxLength_TypedAsciiAtTheLimit_ShouldBeRejected()
    {
        PasswordBox passwordBox = CreatePasswordBox("abc");
        passwordBox.MaxLength = 3;
        passwordBox.CaretIndex = 3;

        passwordBox.HandleCharEntered('X');

        passwordBox.Password.ShouldBe("abc");
    }

    static PasswordBox CreatePasswordBox(string password)
    {
        PasswordBox passwordBox = new();
        passwordBox.Password = password;
        passwordBox.IsFocused = true;
        return passwordBox;
    }
}
