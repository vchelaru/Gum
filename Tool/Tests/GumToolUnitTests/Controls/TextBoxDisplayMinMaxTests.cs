using Shouldly;
using WpfDataUi.Controls;

namespace GumToolUnitTests.Controls;

/// <summary>
/// Pins the reflection-safe MinValue/MaxValue passthrough on the plain numeric field. A variable such
/// as StrokeWidth has a floor of 0 but no natural maximum, so it renders as a TextBoxDisplay with only
/// MinValue set (no slider). PropertiesToSetOnDisplayer pushes a boxed double via raw reflection
/// SetValue, so the exposed property must be double? (not decimal?) or the assignment throws.
/// </summary>
public class TextBoxDisplayMinMaxTests : BaseTestClass
{
    [StaFact]
    public void MinValue_Passthrough_RoundTripsThroughReflectionFriendlyDoubleType()
    {
        TextBoxDisplay display = new TextBoxDisplay { MinValue = 0.0 };

        display.MinValue.ShouldBe(0.0);
    }
}
