using Gum.Input;
using Shouldly;
using SkiaHost.TestsCommon;
using GumKeys = Gum.Forms.Input.Keys;

namespace SilkNetGum.Tests;

public class KeyTableCoverageTests
{
    [Fact]
    public void SilkTable_AccountsForEveryGumKey()
    {
        KeyTableCoverage.AssertEveryKeyAccountedFor(Keyboard.GumToSilk, KeyTableCoverage.MediaImeAndLegacyKeys);
    }

    [Fact]
    public void AssertEveryKeyAccountedFor_NamesTheUnmappedKey()
    {
        Dictionary<GumKeys, int> table = Enum.GetValues<GumKeys>()
            .Except(new[] { GumKeys.Home })
            .ToDictionary(key => key, key => 0);

        Exception exception = Should.Throw<Exception>(() =>
            KeyTableCoverage.AssertEveryKeyAccountedFor(table, new GumKeys[0]));

        exception.Message.ShouldContain("Home");
    }

    [Fact]
    public void AssertEveryKeyAccountedFor_NamesAKeyListedUnmappedButPresent()
    {
        Dictionary<GumKeys, int> table = Enum.GetValues<GumKeys>().ToDictionary(key => key, key => 0);

        Exception exception = Should.Throw<Exception>(() =>
            KeyTableCoverage.AssertEveryKeyAccountedFor(table, new[] { GumKeys.Home }));

        exception.Message.ShouldContain("Home");
    }
}
