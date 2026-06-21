using Gum.Input;
using Shouldly;
using WinFormsKeys = System.Windows.Forms.Keys;

namespace GumToolUnitTests.Input;

public class GumKeyTests : BaseTestClass
{
    [Fact]
    public void GumKey_Values_MatchWinFormsVirtualKeyCodes()
    {
        // Every GumKey member's integer value must equal the Win32 virtual-key code — the same integer
        // WinForms Keys uses — so converting GumKey to a framework key type stays a pure cast. If a
        // value ever drifts from the VK code (or a member is added with a mismatched value), this fails.
        foreach (GumKey gumKey in Enum.GetValues<GumKey>())
        {
            WinFormsKeys winFormsKey = Enum.Parse<WinFormsKeys>(gumKey.ToString());
            ((int)gumKey).ShouldBe((int)winFormsKey, $"GumKey.{gumKey} must equal Keys.{gumKey}");
        }
    }
}
