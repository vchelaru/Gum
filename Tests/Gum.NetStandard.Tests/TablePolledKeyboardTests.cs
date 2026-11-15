using Gum.Input;
using Shouldly;
using GumKeys = Gum.Forms.Input.Keys;

namespace Gum.NetStandard.Tests;

/// <summary>
/// The table-backed keyboard a Skia host builds from a Gum-key-to-engine-key table and one device
/// query. Built against netstandard2.1, the Unity target.
/// </summary>
public class TablePolledKeyboardTests
{
    private enum FakeKey
    {
        Left,
        Letter,
    }

    private sealed class FakeKeyboard : TablePolledKeyboard<FakeKey>
    {
        public HashSet<FakeKey> Pressed { get; } = new HashSet<FakeKey>();

        public FakeKeyboard() : base(new Dictionary<GumKeys, FakeKey>
        {
            { GumKeys.Left, FakeKey.Left },
            { GumKeys.A, FakeKey.Letter },
        })
        {
        }

        protected override bool IsDeviceKeyPressed(FakeKey deviceKey) => Pressed.Contains(deviceKey);

        public bool Lookup(GumKeys key, out FakeKey deviceKey) => TryGetDeviceKey(key, out deviceKey);

        public IEnumerable<GumKeys> Supported => SupportedKeys;

        public void Type(string text) => AppendTypedText(text);
    }

    [Fact]
    public void KeyDown_MappedKeyPressedOnDevice_IsDown()
    {
        FakeKeyboard keyboard = new FakeKeyboard();
        keyboard.Pressed.Add(FakeKey.Left);

        keyboard.Activity(0);

        keyboard.KeyDown(GumKeys.Left).ShouldBeTrue();
        keyboard.KeyPushed(GumKeys.Left).ShouldBeTrue();
    }

    [Fact]
    public void KeyDown_KeyMissingFromTable_IsFalse()
    {
        FakeKeyboard keyboard = new FakeKeyboard();
        keyboard.Pressed.Add(FakeKey.Left);
        keyboard.Pressed.Add(FakeKey.Letter);

        keyboard.Activity(0);

        keyboard.KeyDown(GumKeys.Right).ShouldBeFalse();
    }

    [Fact]
    public void SupportedKeys_EqualsTableKeys()
    {
        new FakeKeyboard().Supported.ShouldBe(new[] { GumKeys.Left, GumKeys.A }, ignoreOrder: true);
    }

    [Fact]
    public void TryGetDeviceKey_ReportsMappedAndUnmappedKeys()
    {
        FakeKeyboard keyboard = new FakeKeyboard();

        keyboard.Lookup(GumKeys.A, out FakeKey mapped).ShouldBeTrue();
        mapped.ShouldBe(FakeKey.Letter);
        keyboard.Lookup(GumKeys.F24, out _).ShouldBeFalse();
    }

    [Fact]
    public void GetStringTyped_ReturnsTextAppendedThroughTheBase()
    {
        FakeKeyboard keyboard = new FakeKeyboard();

        keyboard.Type("hi");
        keyboard.Activity(0);

        keyboard.GetStringTyped().ShouldBe("hi");
    }
}
