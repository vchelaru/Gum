using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Gum.Avalonia.Tests.Harness;
using Gum.Dialogs;
using Shouldly;

namespace Gum.Avalonia.Tests.NewProjectUi;

/// <summary>
/// Typing bad names and locations into the New Project dialog: the message shows next to the buttons, OK is off and
/// Enter does nothing, and fixing the text brings OK back.
/// </summary>
[Trait("Category", "NewProjectUi")]
public class NewProjectUiValidationTests
{
    private static void AssertRefused(NewProjectUiFixture fixture, string message)
    {
        fixture.Settle();
        fixture.ViewModel.ValidationMessage.ShouldBe(message);
        fixture.IsShown(message).ShouldBeTrue($"\"{message}\" is on screen");
        fixture.Dialog.OkButton.IsEffectivelyEnabled.ShouldBeFalse("OK is off");
        fixture.Window.Press(Key.Enter, PhysicalKey.Enter);
        fixture.Dialog.Result.ShouldBeNull("Enter does not create while the form is invalid");
    }

    private static void AssertAccepted(NewProjectUiFixture fixture)
    {
        fixture.Settle();
        fixture.ViewModel.ValidationMessage.ShouldBeEmpty();
        fixture.VisibleTexts().ShouldNotContain(text => text.StartsWith("Enter a project name") || text.StartsWith("The name") || text.StartsWith("Not a valid") || text.StartsWith("Choose a location") || text.StartsWith("The location"));
        fixture.Dialog.OkButton.IsEffectivelyEnabled.ShouldBeTrue("OK is on");
    }

    [AvaloniaTheory]
    [InlineData("")]
    [InlineData("   ")]
    public void AnEmptyName_ShowsEnterAName_AndTypingOneBringsOkBack(string name)
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        if (name.Length == 0)
        {
            fixture.Window.Click(fixture.Dialog.NameBox);
            fixture.Window.Press(Key.A, PhysicalKey.A, RawInputModifiers.Control);
            fixture.Window.Press(Key.Back, PhysicalKey.Backspace);
        }
        else
        {
            fixture.Dialog.TypeName(name);
        }

        AssertRefused(fixture, NewProjectUiText.EnterName);

        fixture.Dialog.TypeName("Game");
        AssertAccepted(fixture);
    }

    [AvaloniaTheory]
    [InlineData("a/b")]
    [InlineData("a\\b")]
    [InlineData("a:b")]
    [InlineData("a*b")]
    [InlineData("a?b")]
    [InlineData("a<b")]
    [InlineData("a>b")]
    [InlineData("a|b")]
    [InlineData("a\"b")]
    [InlineData("name.")]
    [InlineData("..")]
    [InlineData("CON")]
    [InlineData("con")]
    [InlineData("NUL.txt")]
    [InlineData("Aux")]
    [InlineData("PRN")]
    [InlineData("COM1")]
    [InlineData("lpt9.x")]
    public void AForbiddenOrReservedName_ShowsTheMessage_AndAValidNameClearsIt(string name)
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.Dialog.TypeName(name);

        AssertRefused(fixture, NewProjectUiText.InvalidFolderName);

        fixture.Dialog.TypeName("Game");
        AssertAccepted(fixture);
    }

    [AvaloniaTheory]
    [InlineData("My Game")]
    [InlineData("a.b")]
    [InlineData("Spiel-é")]
    [InlineData("  Padded  ")]
    [InlineData("_x")]
    [InlineData("A")]
    [InlineData("CONSOLE")]
    [InlineData("COM10")]
    [InlineData("LPT0")]
    [InlineData("a b c")]
    [InlineData("CON ")]
    public void AValidName_IsAccepted_AndShowsInTheCommandLine(string name)
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        bool reserved = name.Trim().Equals("CON", StringComparison.OrdinalIgnoreCase);
        fixture.Dialog.TypeName(name);

        if (reserved)
        {
            AssertRefused(fixture, NewProjectUiText.InvalidFolderName);
            return;
        }
        AssertAccepted(fixture);
        fixture.Dialog.OpenCommandLine().ShouldContain(Path.Combine(fixture.Location, name.Trim(), name.Trim() + ".gumj"));
    }

    [AvaloniaTheory]
    [InlineData(250, false, true)]
    [InlineData(251, false, false)]
    [InlineData(248, true, true)]
    [InlineData(249, true, false)]
    public void ANameThatMakesTheLongestFileNameExceed255Bytes_IsRefused(int length, bool codeProject, bool accepted)
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.Dialog.SetChecked(NewProjectUiText.CreateCodeProject, codeProject);
        fixture.Dialog.TypeName(new string('a', length));

        if (accepted)
        {
            AssertAccepted(fixture);
        }
        else
        {
            AssertRefused(fixture, NewProjectUiText.NameTooLong);
        }
    }

    [AvaloniaTheory]
    [InlineData("my-game")]
    [InlineData("1Game")]
    [InlineData("my game")]
    [InlineData("a.b")]
    public void ACodeProjectNeedsACSharpName_ButAGumOnlyProjectDoesNot(string name)
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.Dialog.TypeName(name);
        AssertAccepted(fixture);

        fixture.Dialog.SetChecked(NewProjectUiText.CreateCodeProject, true);
        AssertRefused(fixture, NewProjectUiText.InvalidCSharpName);

        fixture.Dialog.TypeName("My_Game2");
        AssertAccepted(fixture);
    }

    [AvaloniaTheory]
    [InlineData("")]
    [InlineData("   ")]
    public void AnEmptyLocation_AsksToChooseOne_AndAFolderBringsOkBack(string location)
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        if (location.Length == 0)
        {
            fixture.Window.Click(fixture.Dialog.LocationBox);
            fixture.Window.Press(Key.A, PhysicalKey.A, RawInputModifiers.Control);
            fixture.Window.Press(Key.Back, PhysicalKey.Backspace);
        }
        else
        {
            fixture.Dialog.TypeLocation(location);
        }

        AssertRefused(fixture, NewProjectUiText.ChooseLocation);

        fixture.Dialog.TypeLocation(fixture.Location);
        AssertAccepted(fixture);
    }

    [AvaloniaTheory]
    [InlineData("Games/MyGame")]
    [InlineData("MyGames")]
    [InlineData("./here")]
    [InlineData("../up")]
    [InlineData("~/Games")]
    public void ARelativeLocation_IsRefused_AndAFullPathBringsOkBack(string location)
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.Dialog.TypeLocation(location);

        AssertRefused(fixture, NewProjectUiText.LocationNotAbsolute);

        fixture.Dialog.TypeLocation(fixture.Location);
        AssertAccepted(fixture);
    }

    [AvaloniaFact]
    public void ALocationWithAControlCharacter_IsRefused()
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.Dialog.TypeLocation(fixture.Location + "\0x");
        fixture.Dialog.LocationBox.Text!.ShouldContain("\0", Case.Sensitive, "the box took the character");

        AssertRefused(fixture, NewProjectUiText.InvalidLocation);

        fixture.Dialog.TypeLocation(fixture.Location);
        AssertAccepted(fixture);
    }

    [AvaloniaFact]
    public void ALocationThatDoesNotExistYet_IsAccepted_AndCreatingMakesTheFolders()
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.StandInRestore(NewProjectUiFixture.RestoreOk());
        string location = Path.Combine(fixture.Root, "not", "there", "yet");

        fixture.Dialog.TypeName("Game");
        fixture.Dialog.TypeLocation(location);
        fixture.Dialog.SetChecked(NewProjectUiText.CreateCodeProject, true);
        AssertAccepted(fixture);
        fixture.Dialog.ClickOk();

        File.Exists(Path.Combine(location, "Game", "Game.csproj")).ShouldBeTrue();
    }

    [AvaloniaFact]
    public void TheMessageSitsBesideTheButtons_AndFollowsEachFix()
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.Dialog.TypeName("a/b");
        fixture.Settle();
        global::Avalonia.Controls.TextBlock message = fixture.Window.Find<global::Avalonia.Controls.TextBlock>(block => block.Text == NewProjectUiText.InvalidFolderName && block.IsEffectivelyVisible);
        global::Avalonia.Rect messageBounds = fixture.BoundsOf(message);
        global::Avalonia.Rect okBounds = fixture.BoundsOf(fixture.Dialog.OkButton);

        messageBounds.Intersects(okBounds).ShouldBeFalse("the message does not cover OK");
        Math.Abs(messageBounds.Center.Y - okBounds.Center.Y).ShouldBeLessThan(okBounds.Height, "on the same row as the buttons");

        fixture.Dialog.TypeLocation("rel");
        fixture.Dialog.TypeName("Game");
        fixture.Settle();
        fixture.IsShown(NewProjectUiText.InvalidFolderName).ShouldBeFalse("the first message is gone");
        fixture.IsShown(NewProjectUiText.LocationNotAbsolute).ShouldBeTrue("and the next problem shows");
    }
}
