using Gum.Automation;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Input;
using Gum.Wireframe;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Automation;

/// <summary>
/// The "test for a test": NDJSON scripts played by <see cref="GumScriptRunner"/> against real Forms
/// controls through the scripted cursor and keyboard, with no OS input read.
/// </summary>
public class GumScriptRunnerTests : BaseTestClass
{
    private readonly ICursor? _originalCursor = FormsUtilities.Cursor;
    private readonly IInputReceiverKeyboard? _originalKeyboard = FormsUtilities.Keyboard;
    private readonly GumDriver _driver;
    private readonly Button _button;
    private readonly TextBox _textBox;
    private readonly CheckBox _checkBox;
    private int _clicks;

    public GumScriptRunnerTests()
    {
        StackPanel panel = new StackPanel();
        panel.AddToRoot();

        _button = new Button { Name = "OkButton" };
        _button.Click += (_, _) => _clicks++;
        panel.AddChild(_button);

        _textBox = new TextBox { Name = "NameBox" };
        panel.AddChild(_textBox);

        _checkBox = new CheckBox { Name = "AgreeBox" };
        panel.AddChild(_checkBox);

        _driver = GumDriver.ForGumService(Gum.GumService.Default);
        _driver.Install();
    }

    public override void Dispose()
    {
        if (_originalCursor != null)
        {
            FormsUtilities.SetCursor(_originalCursor);
        }
        if (_originalKeyboard != null)
        {
            FormsUtilities.SetKeyboard(_originalKeyboard);
        }
        base.Dispose();
    }

    [Fact]
    public void Run_ClickTypeAndCheckScript_DrivesRealControlsAndPassesEveryLine()
    {
        string script = """
            // comments and blank lines are skipped

            {"cmd":"click","target":"OkButton"}
            {"cmd":"click","target":"NameBox"}
            {"cmd":"type","text":"Vic"}
            {"cmd":"key","key":"Back"}
            {"cmd":"click","target":"AgreeBox"}
            {"cmd":"assert","target":"NameBox","property":"Text","equals":"Vi"}
            {"cmd":"assert","target":"AgreeBox","property":"IsChecked","equals":true}
            {"cmd":"step","frames":3}
            """;

        GumScriptResult result = GumScriptRunner.Run(_driver, script);

        result.Passed.ShouldBeTrue(result.ToString());
        result.Lines.Count.ShouldBe(8);
        result.Lines[0].LineNumber.ShouldBe(3);
        _clicks.ShouldBe(1);
        _textBox.Text.ShouldBe("Vi");
        _checkBox.IsChecked.ShouldBe(true);
    }

    [Fact]
    public void Run_FailingAssert_ReportsItsLineNumberAndStops()
    {
        string script = """
            {"cmd":"click","target":"AgreeBox"}
            {"cmd":"step"}
            {"cmd":"assert","target":"AgreeBox","property":"IsChecked","equals":false}
            {"cmd":"click","target":"OkButton"}
            """;

        GumScriptResult result = GumScriptRunner.Run(_driver, script);

        result.Passed.ShouldBeFalse();
        LineResult failure = result.FirstFailure.ShouldNotBeNull();
        failure.LineNumber.ShouldBe(3);
        failure.Error.ShouldNotBeNull().ShouldContain("IsChecked");
        failure.Error.ShouldContain("True");
        result.ToString().ShouldContain("line 3");
        // The line after the failure never ran.
        _clicks.ShouldBe(0);
    }

    [Fact]
    public void Run_UnknownTarget_ReportsLineNumberAndName()
    {
        GumScriptResult result = GumScriptRunner.Run(_driver, """
            {"cmd":"step"}
            {"cmd":"click","target":"Missing"}
            """);

        LineResult failure = result.FirstFailure.ShouldNotBeNull();
        failure.LineNumber.ShouldBe(2);
        failure.Error.ShouldNotBeNull().ShouldContain("Missing");
    }

    [Fact]
    public void Run_MalformedOrUnknownCommand_ReportsLineNumber()
    {
        GumScriptRunner.Run(_driver, "{\"cmd\":\"click\",\"target\":")
            .FirstFailure.ShouldNotBeNull().LineNumber.ShouldBe(1);
        GumScriptRunner.Run(_driver, "{\"cmd\":\"step\"}\n{\"cmd\":\"teleport\"}")
            .FirstFailure.ShouldNotBeNull().Error.ShouldNotBeNull().ShouldContain("teleport");
        // A misspelled field is an error, not silently ignored.
        GumScriptRunner.Run(_driver, "{\"cmd\":\"click\",\"targt\":\"OkButton\"}")
            .FirstFailure.ShouldNotBeNull().Error.ShouldNotBeNull().ShouldContain("targt");
    }
}
