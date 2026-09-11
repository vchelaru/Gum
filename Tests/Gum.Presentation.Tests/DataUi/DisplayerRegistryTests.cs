using System.Collections.Generic;
using Shouldly;
using WpfDataUi;
using WpfDataUi.DataTypes;

namespace Gum.Presentation.Tests.DataUi;

public class DisplayerRegistryTests
{
    private enum SampleEnum
    {
        First,
        Second
    }

    private sealed class FakeTextControl { }

    private sealed class FakeSliderControl { }

    private static InstanceMember MemberOfType(Type type)
    {
        InstanceMember member = new InstanceMember { Name = "Member" };
        member.CustomGetTypeEvent += _ => type;
        member.CustomGetEvent += _ => null;
        return member;
    }

    [Fact]
    public void ResolveControlType_ReturnsRegisteredControl_OrTheTypeItselfWhenUnregistered()
    {
        DisplayerRegistry registry = new DisplayerRegistry();
        registry.Register(typeof(StandardDisplayers.Slider), typeof(FakeSliderControl));

        registry.ResolveControlType(typeof(StandardDisplayers.Slider)).ShouldBe(typeof(FakeSliderControl));
        // A concrete control assigned as the preferred displayer passes straight through.
        registry.ResolveControlType(typeof(FakeTextControl)).ShouldBe(typeof(FakeTextControl));
    }

    [Fact]
    public void SelectDisplayerKey_PrefersExplicitDisplayer_ThenCustomOptions_ThenType_ThenText()
    {
        DisplayerRegistry registry = new DisplayerRegistry();

        InstanceMember preferred = MemberOfType(typeof(bool));
        preferred.PreferredDisplayer = typeof(StandardDisplayers.Slider);
        registry.SelectDisplayerKey(preferred).ShouldBe(typeof(StandardDisplayers.Slider));

        InstanceMember withOptions = MemberOfType(typeof(string));
        withOptions.CustomOptions = new List<object> { "A", "B" };
        registry.SelectDisplayerKey(withOptions).ShouldBe(typeof(StandardDisplayers.ComboBox));

        registry.SelectDisplayerKey(MemberOfType(typeof(bool))).ShouldBe(typeof(StandardDisplayers.CheckBox));
        registry.SelectDisplayerKey(MemberOfType(typeof(bool?))).ShouldBe(typeof(StandardDisplayers.NullableBool));
        registry.SelectDisplayerKey(MemberOfType(typeof(SampleEnum))).ShouldBe(typeof(StandardDisplayers.ComboBox));
        registry.SelectDisplayerKey(MemberOfType(typeof(SampleEnum?))).ShouldBe(typeof(StandardDisplayers.ComboBox));
        registry.SelectDisplayerKey(MemberOfType(typeof(List<string>))).ShouldBe(typeof(StandardDisplayers.ListBox));
        registry.SelectDisplayerKey(MemberOfType(typeof(string))).ShouldBe(typeof(StandardDisplayers.TextBox));
        registry.SelectDisplayerKey(MemberOfType(typeof(int?))).ShouldBe(typeof(StandardDisplayers.TextBox));
    }

    [Fact]
    public void SelectDisplayerKey_EmptyCustomOptions_FallsBackToTheType()
    {
        DisplayerRegistry registry = new DisplayerRegistry();
        InstanceMember member = MemberOfType(typeof(bool));
        member.CustomOptions = new List<object>();

        registry.SelectDisplayerKey(member).ShouldBe(typeof(StandardDisplayers.CheckBox));
    }
}
