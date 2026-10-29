using Gum.Plugins.InternalPlugins.VariableGrid;
using Shouldly;
using System.Collections.Generic;
using WpfDataUi.DataTypes;
using Xunit;

namespace Gum.Presentation.Tests.Plugins.InternalPlugins.VariableGrid;

public class SyntheticInstanceMemberTests
{
    private static void Wire(InstanceMember member, object?[] stored, List<object?> setCalls, System.Type valueType)
    {
        member.CustomGetTypeEvent += _ => valueType;
        member.CustomGetEvent += _ => stored[0];
        member.CustomSetPropertyEvent += (_, args) =>
        {
            stored[0] = args.Value;
            setCalls.Add(args.Value);
        };
    }

    [Fact]
    public void IsDefaultSetTrue_WithoutResetDelegate_ClearsTheValueLikeAPlainInstanceMember()
    {
        object?[] syntheticStored = { "x" };
        List<object?> syntheticSets = new List<object?>();
        SyntheticInstanceMember synthetic = new SyntheticInstanceMember(isDefault: null, resetToDefault: null);
        Wire(synthetic, syntheticStored, syntheticSets, typeof(string));

        object?[] plainStored = { "x" };
        List<object?> plainSets = new List<object?>();
        InstanceMember plain = new InstanceMember();
        Wire(plain, plainStored, plainSets, typeof(string));

        synthetic.IsDefault = true;
        plain.IsDefault = true;

        plainStored[0].ShouldBeNull();
        syntheticStored[0].ShouldBe(plainStored[0]);
        syntheticSets.Count.ShouldBe(1);
        syntheticSets.Count.ShouldBe(plainSets.Count);
    }

    [Fact]
    public void IsDefaultSetTrue_WithoutResetDelegate_SetsAValueTypeToItsDefault()
    {
        object?[] stored = { 5 };
        List<object?> sets = new List<object?>();
        SyntheticInstanceMember member = new SyntheticInstanceMember(isDefault: null, resetToDefault: null);
        Wire(member, stored, sets, typeof(int));

        member.IsDefault = true;

        stored[0].ShouldBe(0);
        sets.Count.ShouldBe(1);
    }

    [Fact]
    public void IsDefaultSetTrue_WithResetDelegate_RunsItOnceAndDoesNotClearTheValue()
    {
        object?[] stored = { "x" };
        List<object?> sets = new List<object?>();
        int resetCount = 0;
        SyntheticInstanceMember member = new SyntheticInstanceMember(isDefault: null, resetToDefault: () => resetCount++);
        Wire(member, stored, sets, typeof(string));
        List<string?> notifications = new List<string?>();
        member.PropertyChanged += (_, args) => notifications.Add(args.PropertyName);

        member.IsDefault = true;

        resetCount.ShouldBe(1);
        sets.ShouldBeEmpty();
        stored[0].ShouldBe("x");
        notifications.ShouldContain("Value");
        notifications.ShouldContain("IsDefault");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void IsDefaultSetFalse_DoesNothing(bool hasResetDelegate)
    {
        object?[] stored = { "x" };
        List<object?> sets = new List<object?>();
        int resetCount = 0;
        SyntheticInstanceMember member = new SyntheticInstanceMember(
            isDefault: null, resetToDefault: hasResetDelegate ? () => resetCount++ : null);
        Wire(member, stored, sets, typeof(string));
        List<string?> notifications = new List<string?>();
        member.PropertyChanged += (_, args) => notifications.Add(args.PropertyName);

        member.IsDefault = false;

        resetCount.ShouldBe(0);
        sets.ShouldBeEmpty();
        stored[0].ShouldBe("x");
        notifications.ShouldBeEmpty();
    }

    [Fact]
    public void IsDefaultGet_WithoutDelegate_IsFalse()
    {
        SyntheticInstanceMember member = new SyntheticInstanceMember(isDefault: null, resetToDefault: null);

        member.IsDefault.ShouldBeFalse();
    }

    [Fact]
    public void IsDefaultGet_WithDelegate_FollowsIt()
    {
        bool isDefault = true;
        SyntheticInstanceMember member = new SyntheticInstanceMember(isDefault: () => isDefault, resetToDefault: null);

        member.IsDefault.ShouldBeTrue();

        isDefault = false;

        member.IsDefault.ShouldBeFalse();
    }
}
