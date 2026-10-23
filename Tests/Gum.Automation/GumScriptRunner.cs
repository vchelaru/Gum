using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GumKeys = Gum.Forms.Input.Keys;

namespace Gum.Automation;

/// <summary>
/// One line of an NDJSON Gum script: a JSON object whose <see cref="Cmd"/> names one
/// <see cref="GumDriver"/> call. Unknown fields are errors, so a typo cannot be silently ignored.
/// </summary>
/// <remarks>
/// Commands (positions are screen pixels; <c>target</c> is an element name, searched topmost root first):
/// <list type="bullet">
/// <item><c>{"cmd":"step","frames":3}</c> runs frames (default 1).</item>
/// <item><c>{"cmd":"move","target":"OkButton"}</c> or <c>{"cmd":"move","x":10,"y":20}</c> moves the cursor.</item>
/// <item><c>{"cmd":"click","target":"OkButton","button":"secondary"}</c> moves, pushes, releases. <c>button</c> is primary (default), secondary or middle; x/y work instead of target.</item>
/// <item><c>{"cmd":"down"}</c> / <c>{"cmd":"up"}</c> hold or release <c>button</c> where the cursor is.</item>
/// <item><c>{"cmd":"drag","target":"ThumbInstance","toX":300,"toY":40}</c> pushes on target, moves, releases.</item>
/// <item><c>{"cmd":"scroll","target":"List","detents":-2}</c> turns the wheel over target; negative scrolls down.</item>
/// <item><c>{"cmd":"type","text":"hello"}</c> types into the focused control.</item>
/// <item><c>{"cmd":"key","key":"Enter"}</c> presses a key (Gum.Forms.Input.Keys name); add <c>"down":true|false</c> to only hold or release it.</item>
/// <item><c>{"cmd":"assert","target":"AgreeBox","property":"IsChecked","equals":true}</c> reads the Forms control's property, else the visual's, and compares.</item>
/// <item><c>{"cmd":"screenshot","path":"out.png"}</c> writes the frame (needs a host screenshot writer).</item>
/// </list>
/// Blank lines and lines starting with <c>//</c> are skipped.
/// </remarks>
public sealed class GumScriptCommand
{
    /// <summary>The command name; see the remarks on <see cref="GumScriptCommand"/>.</summary>
    public string Cmd { get; set; } = "";

    /// <summary>Element name for move, click, drag, scroll and assert.</summary>
    public string? Target { get; set; }

    /// <summary>Screen x for move or click when no target is given.</summary>
    public int? X { get; set; }

    /// <summary>Screen y for move or click when no target is given.</summary>
    public int? Y { get; set; }

    /// <summary>Drag end x.</summary>
    public int? ToX { get; set; }

    /// <summary>Drag end y.</summary>
    public int? ToY { get; set; }

    /// <summary>primary (default), secondary or middle.</summary>
    public string? Button { get; set; }

    /// <summary>Frames for step (default 1).</summary>
    public int? Frames { get; set; }

    /// <summary>Wheel detents for scroll; negative scrolls down.</summary>
    public int? Detents { get; set; }

    /// <summary>Text for type.</summary>
    public string? Text { get; set; }

    /// <summary>Key name for key, from <c>Gum.Forms.Input.Keys</c>.</summary>
    public string? Key { get; set; }

    /// <summary>For key: true holds, false releases; absent presses and releases.</summary>
    public bool? Down { get; set; }

    /// <summary>Property read by assert.</summary>
    public string? Property { get; set; }

    /// <summary>Expected value for assert: a string, number, bool or null.</summary>
    [JsonPropertyName("equals")]
    public JsonElement? Expected { get; set; }

    /// <summary>Output file for screenshot.</summary>
    public string? Path { get; set; }
}

/// <summary>The outcome of one script line.</summary>
/// <param name="LineNumber">1-based line number in the script.</param>
/// <param name="Text">The line as written.</param>
/// <param name="Error">Why the line failed, or null when it passed.</param>
public sealed record LineResult(int LineNumber, string Text, string? Error)
{
    /// <summary>Whether the line ran without error.</summary>
    public bool Passed => Error == null;
}

/// <summary>The outcome of a script run: every line that ran, stopping at the first failure.</summary>
public sealed class GumScriptResult
{
    internal GumScriptResult(IReadOnlyList<LineResult> lines) => Lines = lines;

    /// <summary>The lines that ran, in order; comments and blank lines are not listed.</summary>
    public IReadOnlyList<LineResult> Lines { get; }

    /// <summary>The failed line, or null when every line passed.</summary>
    public LineResult? FirstFailure => Lines.FirstOrDefault(l => !l.Passed);

    /// <summary>Whether every line passed.</summary>
    public bool Passed => FirstFailure == null;

    /// <summary>One report line per script line: <c>PASS line 3: ...</c> or <c>FAIL line 3: ... -> error</c>.</summary>
    public override string ToString()
    {
        StringBuilder builder = new StringBuilder();
        foreach (LineResult line in Lines)
        {
            builder.Append(line.Passed ? "PASS" : "FAIL").Append(" line ").Append(line.LineNumber)
                .Append(": ").Append(line.Text);
            if (!line.Passed)
            {
                builder.Append(" -> ").Append(line.Error);
            }
            builder.AppendLine();
        }
        return builder.ToString();
    }
}

/// <summary>Plays an NDJSON script (see <see cref="GumScriptCommand"/>) through a <see cref="GumDriver"/>.</summary>
public static class GumScriptRunner
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    /// <summary>Runs the script file at <paramref name="path"/>.</summary>
    public static GumScriptResult RunFile(GumDriver driver, string path) => Run(driver, File.ReadAllText(path));

    /// <summary>Runs <paramref name="script"/> line by line and stops at the first failing line.</summary>
    public static GumScriptResult Run(GumDriver driver, string script)
    {
        List<LineResult> results = new();
        string[] lines = script.Replace("\r\n", "\n").Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            string text = lines[i].Trim();
            if (text.Length == 0 || text.StartsWith("//", StringComparison.Ordinal))
            {
                continue;
            }

            string? error = null;
            try
            {
                GumScriptCommand command = JsonSerializer.Deserialize<GumScriptCommand>(text, Options)
                    ?? throw new GumAutomationException("Line is JSON null.");
                Execute(driver, command);
            }
            catch (JsonException exception)
            {
                error = "Invalid JSON: " + exception.Message;
            }
            catch (GumAutomationException exception)
            {
                error = exception.Message;
            }
            catch (Exception exception)
            {
                // An app exception thrown while a line runs is that line's failure, not a runner crash.
                error = $"{exception.GetType().Name}: {exception.Message}";
            }

            results.Add(new LineResult(i + 1, text, error));
            if (error != null)
            {
                break;
            }
        }
        return new GumScriptResult(results);
    }

    private static void Execute(GumDriver driver, GumScriptCommand command)
    {
        switch (command.Cmd.ToLowerInvariant())
        {
            case "step":
                driver.Step(command.Frames ?? 1);
                break;
            case "move":
                if (command.Target != null) { driver.MoveTo(command.Target); }
                else { driver.MoveTo(Required(command.X, "x"), Required(command.Y, "y")); }
                break;
            case "click":
                if (command.Target != null) { driver.Click(command.Target, ParseButton(command.Button)); }
                else { driver.Click(Required(command.X, "x"), Required(command.Y, "y"), ParseButton(command.Button)); }
                break;
            case "down":
            case "up":
                driver.SetButton(ParseButton(command.Button), held: command.Cmd.Equals("down", StringComparison.OrdinalIgnoreCase));
                break;
            case "drag":
                driver.Drag(Required(command.Target, "target"), Required(command.ToX, "toX"), Required(command.ToY, "toY"));
                break;
            case "scroll":
                driver.Scroll(Required(command.Target, "target"), Required(command.Detents, "detents"));
                break;
            case "type":
                driver.Type(Required(command.Text, "text"));
                break;
            case "key":
                GumKeys key = Enum.TryParse(Required(command.Key, "key"), ignoreCase: true, out GumKeys parsed)
                    ? parsed
                    : throw new GumAutomationException($"Unknown key '{command.Key}'.");
                if (command.Down is bool down) { driver.SetKey(key, down); }
                else { driver.PressKey(key); }
                break;
            case "assert":
                Assert(driver, command);
                break;
            case "screenshot":
                driver.Screenshot(Required(command.Path, "path"));
                break;
            default:
                throw new GumAutomationException(
                    $"Unknown cmd '{command.Cmd}'. Known: step, move, click, down, up, drag, scroll, type, key, assert, screenshot.");
        }
    }

    private static void Assert(GumDriver driver, GumScriptCommand command)
    {
        string target = Required(command.Target, "target");
        string property = Required(command.Property, "property");
        JsonElement expected = command.Expected
            ?? throw new GumAutomationException("assert needs 'equals'.");
        object? actual = driver.Read(target, property);

        bool matches = expected.ValueKind switch
        {
            JsonValueKind.Null => actual == null,
            JsonValueKind.True => actual is true,
            JsonValueKind.False => actual is false,
            JsonValueKind.Number => actual is IConvertible && actual is not bool && actual is not string
                && Math.Abs(Convert.ToDouble(actual, CultureInfo.InvariantCulture) - expected.GetDouble()) < 0.0001,
            JsonValueKind.String => Convert.ToString(actual, CultureInfo.InvariantCulture) == expected.GetString(),
            _ => throw new GumAutomationException("'equals' must be a string, number, bool or null."),
        };

        if (!matches)
        {
            string actualText = actual == null ? "null" : Convert.ToString(actual, CultureInfo.InvariantCulture) ?? "null";
            throw new GumAutomationException($"{target}.{property} is {actualText}, expected {expected.GetRawText()}.");
        }
    }

    private static MouseButton ParseButton(string? button) => button?.ToLowerInvariant() switch
    {
        null or "primary" or "left" => MouseButton.Primary,
        "secondary" or "right" => MouseButton.Secondary,
        "middle" => MouseButton.Middle,
        _ => throw new GumAutomationException($"Unknown button '{button}'. Use primary, secondary or middle."),
    };

    private static T Required<T>(T? value, string field) where T : struct =>
        value ?? throw new GumAutomationException($"Missing '{field}'.");

    private static string Required(string? value, string field) =>
        value ?? throw new GumAutomationException($"Missing '{field}'.");
}
