using Gum.Input;
using Gum.Wireframe;
using Shouldly;
using GumKeys = Gum.Forms.Input.Keys;

namespace SkiaGum.Tests.Performance;

/// <summary>
/// <c>PolledKeyboard.KeysTyped</c> is read by every focused control every frame, so reading it must not allocate
/// (#7863), while it still yields what the keyboard reports: the typed keys in <c>SupportedKeys</c> order, for
/// any number of readers, nested or one after another.
/// </summary>
public class KeysTypedAllocationTests : IDisposable
{
    private readonly FrameKeyboard _keyboard = new FrameKeyboard();
    private double _seconds;

    public void Dispose() => _keyboard.Dispose();

    private IEnumerable<GumKeys> KeysTyped => ((IInputReceiverKeyboard)_keyboard).KeysTyped;

    private void Press(params GumKeys[] keys)
    {
        foreach (GumKeys key in new[] { GumKeys.Back, GumKeys.Left, GumKeys.Right, GumKeys.LeftShift, GumKeys.RightShift })
        {
            _keyboard.SetKey(key, held: Array.IndexOf(keys, key) >= 0);
        }
        _seconds += 1.0 / 60.0;
        _keyboard.Activity(_seconds);
    }

    [Fact]
    public void KeysTyped_SeveralKeysPushed_YieldsThemInSupportedKeysOrder()
    {
        Press(GumKeys.Right, GumKeys.Back, GumKeys.LeftShift);

        KeysTyped.ToList().ShouldBe(new[] { GumKeys.Back, GumKeys.Right, GumKeys.LeftShift });
    }

    [Fact]
    public void KeysTyped_NoKeyPushed_IsEmpty()
    {
        Press();

        KeysTyped.Any().ShouldBeFalse();
    }

    [Fact]
    public void KeysTyped_ReadTwiceInARow_YieldsTheSameKeysEachTime()
    {
        Press(GumKeys.Left);

        KeysTyped.ToList().ShouldBe(new[] { GumKeys.Left });
        KeysTyped.ToList().ShouldBe(new[] { GumKeys.Left });
        KeysTyped.Contains(GumKeys.Left).ShouldBeTrue();
        KeysTyped.Contains(GumKeys.Right).ShouldBeFalse();
    }

    [Fact]
    public void KeysTyped_ReadWhileAnotherReadIsEnumerating_BothSeeEveryKey()
    {
        Press(GumKeys.Back, GumKeys.Left, GumKeys.Right);

        List<GumKeys> outer = new List<GumKeys>();
        List<GumKeys> inner = new List<GumKeys>();
        foreach (GumKeys key in KeysTyped)
        {
            outer.Add(key);
            if (inner.Count == 0)
            {
                foreach (GumKeys innerKey in KeysTyped)
                {
                    inner.Add(innerKey);
                }
            }
        }

        outer.ShouldBe(new[] { GumKeys.Back, GumKeys.Left, GumKeys.Right });
        inner.ShouldBe(new[] { GumKeys.Back, GumKeys.Left, GumKeys.Right });
    }

    [Fact]
    public void KeysTyped_AReadAbandonedMidway_DoesNotHideKeysFromTheNextRead()
    {
        Press(GumKeys.Back, GumKeys.Left);

        foreach (GumKeys key in KeysTyped)
        {
            break;
        }
        IEnumerator<GumKeys> abandoned = KeysTyped.GetEnumerator();
        abandoned.MoveNext();

        KeysTyped.ToList().ShouldBe(new[] { GumKeys.Back, GumKeys.Left });
    }

    [Fact]
    public void KeysTyped_ForeachEveryFrame_AllocatesNothing()
    {
        Press(GumKeys.Left);
        int seen = 0;
        for (int i = 0; i < 100; i++)
        {
            foreach (GumKeys key in KeysTyped)
            {
                seen++;
            }
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++)
        {
            foreach (GumKeys key in KeysTyped)
            {
                seen++;
            }
        }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        seen.ShouldBe(1100);
        allocated.ShouldBe(0L);
    }
}
