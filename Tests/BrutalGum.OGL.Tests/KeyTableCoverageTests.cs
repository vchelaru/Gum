using Gum.Input;
using SkiaHost.TestsCommon;

namespace BrutalGum.Tests;

public class KeyTableCoverageTests
{
    [Fact]
    public void BrutalTable_AccountsForEveryGumKey()
    {
        KeyTableCoverage.AssertEveryKeyAccountedFor(BrutalGlfwInput.GumToGlfw, KeyTableCoverage.MediaImeAndLegacyKeys);
    }
}
