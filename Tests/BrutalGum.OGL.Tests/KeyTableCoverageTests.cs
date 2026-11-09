using Gum.Input;
using SkiaHost.TestsCommon;

namespace BrutalGum.Tests;

public class KeyTableCoverageTests
{
    [Fact]
    public void BrutalTable_AccountsForEveryGumKey()
    {
        KeyTableCoverage.AssertEveryKeyAccountedFor(Keyboard.GumToGlfw, KeyTableCoverage.MediaImeAndLegacyKeys);
    }
}
