using System;
using System.Collections.Generic;
using Shouldly;
using WpfDataUi.DataTypes;
using Xunit;

namespace GumToolUnitTests.DataTypes;

public class CompositeInstanceMemberTests
{
    // Domain-agnostic compose/decompose: pack three 0-255 channels into one int and back.
    private static readonly Func<IReadOnlyList<object?>, object> Compose =
        channels => ((int)channels[0]! << 16) | ((int)channels[1]! << 8) | (int)channels[2]!;

    private static readonly Func<object, object?[]> Decompose =
        packed => new object?[]
        {
            ((int)packed >> 16) & 0xFF,
            ((int)packed >> 8) & 0xFF,
            (int)packed & 0xFF,
        };

    private static CompositeInstanceMember MakeComposite(params FakeChannelMember[] channels) =>
        new CompositeInstanceMember("Composite", channels, typeof(int), Compose, Decompose);

    [Fact]
    public void IsDefault_ShouldBeFalse_WhenAnyChannelIsNotDefault()
    {
        FakeChannelMember red = new() { IsDefault = true };
        FakeChannelMember green = new() { IsDefault = false };
        FakeChannelMember blue = new() { IsDefault = true };

        CompositeInstanceMember composite = MakeComposite(red, green, blue);

        composite.IsDefault.ShouldBeFalse();
    }

    [Fact]
    public void IsDefault_ShouldBeTrue_WhenAllChannelsAreDefault()
    {
        FakeChannelMember red = new() { IsDefault = true };
        FakeChannelMember green = new() { IsDefault = true };
        FakeChannelMember blue = new() { IsDefault = true };

        CompositeInstanceMember composite = MakeComposite(red, green, blue);

        composite.IsDefault.ShouldBeTrue();
    }

    [Fact]
    public void IsReadOnly_ShouldBeTrue_WhenAnyChannelIsReadOnly()
    {
        FakeChannelMember red = new() { IsReadOnly = false };
        FakeChannelMember green = new() { IsReadOnly = true };
        FakeChannelMember blue = new() { IsReadOnly = false };

        CompositeInstanceMember composite = MakeComposite(red, green, blue);

        composite.IsReadOnly.ShouldBeTrue();
    }

    [Fact]
    public void PropertyType_ShouldBeCompositeType()
    {
        CompositeInstanceMember composite = MakeComposite(new(), new(), new());

        composite.PropertyType.ShouldBe(typeof(int));
    }

    [Fact]
    public void SetValue_ShouldDecomposeIntoEachChannel()
    {
        FakeChannelMember red = new();
        FakeChannelMember green = new();
        FakeChannelMember blue = new();

        CompositeInstanceMember composite = MakeComposite(red, green, blue);

        composite.SetValue((1 << 16) | (2 << 8) | 3, SetPropertyCommitType.Full);

        red.BackingValue.ShouldBe(1);
        green.BackingValue.ShouldBe(2);
        blue.BackingValue.ShouldBe(3);
    }

    [Fact]
    public void SetValue_ShouldSkipChannelWrite_WhenDecomposedValueAlreadyMatchesChannel()
    {
        // Issue #3617 corner-radius follow-up: a composite whose channels carry inherit/explicit
        // (nullable) semantics must not force-write a channel whose value isn't actually changing,
        // or an edit to one channel silently converts every other unchanged channel from inherited
        // to explicit (e.g. writing an explicit null onto per-corner overrides just because the
        // uniform CornerRadius field was edited).
        FakeChannelMember red = new() { BackingValue = 1 };
        FakeChannelMember green = new() { BackingValue = 9 };
        FakeChannelMember blue = new() { BackingValue = 3 };

        CompositeInstanceMember composite = MakeComposite(red, green, blue);

        // Decomposes to (1, 2, 3) - only green's target (2) differs from its current value (9).
        composite.SetValue((1 << 16) | (2 << 8) | 3, SetPropertyCommitType.Full);

        red.SetCallCount.ShouldBe(0);
        green.SetCallCount.ShouldBe(1);
        blue.SetCallCount.ShouldBe(0);
    }

    [Fact]
    public void SetValue_ShouldRaiseAfterComposite_EvenWhenAChannelThrows()
    {
        // AfterComposite is where the consumer disposes the undo lock taken in BeforeComposite. If a
        // channel write throws and AfterComposite is skipped, the lock leaks and suppresses all further
        // undo recording for the session. The write must be wrapped so AfterComposite always runs.
        FakeChannelMember red = new();
        FakeChannelMember green = new() { ThrowOnSet = true };
        FakeChannelMember blue = new();

        CompositeInstanceMember composite = MakeComposite(red, green, blue);

        int afterCount = 0;
        composite.AfterComposite += _ => afterCount++;

        Should.Throw<InvalidOperationException>(() =>
            composite.SetValue((1 << 16) | (2 << 8) | 3, SetPropertyCommitType.Full));

        afterCount.ShouldBe(1);
    }

    [Fact]
    public void SetValue_ShouldRaiseBeforeAndAfterCompositeExactlyOnce()
    {
        CompositeInstanceMember composite = MakeComposite(new(), new(), new());

        int beforeCount = 0;
        int afterCount = 0;
        composite.BeforeComposite += _ => beforeCount++;
        composite.AfterComposite += _ => afterCount++;

        composite.SetValue((1 << 16) | (2 << 8) | 3, SetPropertyCommitType.Full);

        beforeCount.ShouldBe(1);
        afterCount.ShouldBe(1);
    }

    [Fact]
    public void SetValue_ShouldThrowInvalidOperation_AndStillRaiseAfterComposite_WhenDecomposeArityMismatches()
    {
        // A descriptor whose Decompose returns the wrong number of values would otherwise throw
        // IndexOutOfRange deep in the loop; the explicit guard gives a diagnosable message and the
        // try/finally still runs AfterComposite so the undo lock is released.
        CompositeInstanceMember composite = new(
            "Composite",
            new FakeChannelMember[] { new(), new(), new() },
            typeof(int),
            Compose,
            _ => new object?[] { 1, 2 });

        int afterCount = 0;
        composite.AfterComposite += _ => afterCount++;

        Should.Throw<InvalidOperationException>(() =>
            composite.SetValue(123, SetPropertyCommitType.Full));

        afterCount.ShouldBe(1);
    }

    [Fact]
    public void Value_ShouldComposeFromChannels()
    {
        FakeChannelMember red = new() { BackingValue = 10 };
        FakeChannelMember green = new() { BackingValue = 20 };
        FakeChannelMember blue = new() { BackingValue = 30 };

        CompositeInstanceMember composite = MakeComposite(red, green, blue);

        composite.Value.ShouldBe((10 << 16) | (20 << 8) | 30);
    }

    private class FakeChannelMember : InstanceMember
    {
        private bool _isDefault;
        private bool _isReadOnly;

        public int BackingValue { get; set; }

        public bool ThrowOnSet { get; set; }

        public int SetCallCount { get; private set; }

        public override bool IsDefault
        {
            get => _isDefault;
            set => _isDefault = value;
        }

        public override bool IsReadOnly
        {
            get => _isReadOnly;
            set => _isReadOnly = value;
        }

        public FakeChannelMember()
        {
            CustomGetEvent += _ => BackingValue;
            CustomGetTypeEvent += _ => typeof(int);
            CustomSetPropertyEvent += (_, args) =>
            {
                SetCallCount++;
                if (ThrowOnSet)
                {
                    throw new InvalidOperationException("Simulated channel write failure.");
                }
                BackingValue = (int)args.Value!;
            };
        }
    }
}
