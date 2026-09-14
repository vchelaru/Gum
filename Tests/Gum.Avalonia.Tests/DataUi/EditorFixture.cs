using WpfDataUi.DataTypes;

namespace Gum.Avalonia.Tests.DataUi;

/// <summary>Options for the fixture's enum members.</summary>
public enum FixtureChoice
{
    First,
    Second,
    Third,
}

/// <summary>
/// One member of each type the grid's editors handle. Every editor test binds an editor to one of
/// these, sets a value through the editor, and reads it back here (and the reverse).
/// </summary>
public class EditorFixture
{
    public string Text { get; set; } = "start";
    public float Number { get; set; } = 1;
    public int Count { get; set; } = 3;
    public float? MaybeNumber { get; set; } = 2;
    public bool Flag { get; set; }
    public bool? Maybe { get; set; } = true;
    public FixtureChoice Choice { get; set; }
    public List<string> Lines { get; set; } = new List<string> { "alpha" };
    public List<int> Numbers { get; set; } = new List<int> { 1, 2 };
    public List<string> Files { get; set; } = new List<string>();
    public string File { get; set; } = "";
    public float Angle { get; set; }
    public float Red { get; set; } = 1;
    public float Green { get; set; } = 2;

    /// <summary>A reflection member over <paramref name="propertyName"/> on this fixture.</summary>
    public InstanceMember Member(string propertyName) => new InstanceMember(propertyName, this);
}
