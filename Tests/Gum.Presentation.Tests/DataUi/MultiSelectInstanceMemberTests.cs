using Shouldly;
using WpfDataUi.DataTypes;

namespace Gum.Presentation.Tests.DataUi;

public class MultiSelectInstanceMemberTests
{
    [Fact]
    public void Value_ReturnsTheList_WhenEveryMemberHasAnEqualButSeparateList()
    {
        // Variable reference rows are List<string>; two instances hold different list objects with the same lines.
        List<string> firstLines = new List<string> { "X=Sin(WaveValue)", "Y=Width" };
        List<string> secondLines = new List<string> { "X=Sin(WaveValue)", "Y=Width" };
        MultiSelectInstanceMember multi = CreateMulti(firstLines, secondLines);

        multi.Value.ShouldBeSameAs(firstLines);
        multi.IsIndeterminate.ShouldBeFalse();
    }

    [Fact]
    public void Value_IsNullAndIndeterminate_WhenListsDifferInContents()
    {
        MultiSelectInstanceMember multi = CreateMulti(
            new List<string> { "X=Width" },
            new List<string> { "X=Height" });

        multi.Value.ShouldBeNull();
        multi.IsIndeterminate.ShouldBeTrue();
    }

    [Fact]
    public void Value_IsNullAndIndeterminate_WhenListsDifferInLength()
    {
        MultiSelectInstanceMember multi = CreateMulti(
            new List<string> { "X=Width" },
            new List<string> { "X=Width", "Y=Height" });

        multi.Value.ShouldBeNull();
        multi.IsIndeterminate.ShouldBeTrue();
    }

    private static MultiSelectInstanceMember CreateMulti(params object[] values)
    {
        List<InstanceMember> inner = new List<InstanceMember>();
        foreach (object value in values)
        {
            InstanceMember member = new InstanceMember { Name = "VariableReferences" };
            member.CustomGetEvent += _ => value;
            inner.Add(member);
        }
        return new MultiSelectInstanceMember { Name = "VariableReferences", InstanceMembers = inner };
    }
}
