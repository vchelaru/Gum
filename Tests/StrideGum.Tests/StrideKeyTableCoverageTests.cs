using Gum.Input;
using SkiaHost.TestsCommon;

namespace StrideGum.Tests;

public class StrideKeyTableCoverageTests
{
    [Fact]
    public void StrideTable_AccountsForEveryGumKey()
    {
        KeyTableCoverage.AssertEveryKeyAccountedFor(Keyboard.GumToStride, KeyTableCoverage.MediaImeAndLegacyKeys);
    }
}
