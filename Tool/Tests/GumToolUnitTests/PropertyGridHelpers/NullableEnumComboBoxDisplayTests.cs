using Shouldly;
using WpfDataUi;
using WpfDataUi.Controls;
using WpfDataUi.DataTypes;

namespace GumToolUnitTests.PropertyGridHelpers;

/// <summary>
/// Verifies that <see cref="WpfDataUi.Controls.ComboBoxDisplay"/> handles a nullable
/// enum (e.g., <c>ResizeBehavior?</c>) the same way it handles a non-nullable enum,
/// with an additional null entry, and that <see cref="SingleDataUiContainer"/> routes
/// nullable-enum types to the combo box displayer rather than the textbox fallback.
/// </summary>
public class NullableEnumComboBoxDisplayTests : BaseTestClass
{
    public enum SampleEnum
    {
        Alpha,
        Beta,
        Gamma
    }

    [Fact]
    public void SingleDataUiContainer_RoutingPredicate_NullableEnum_RoutesToComboBoxDisplay()
    {
        // The TypeDisplayerAssociation is evaluated in order in
        // SingleDataUiContainer.CreateInternalControl. Find the first predicate that
        // matches typeof(SampleEnum?). It must be the ComboBoxDisplay association,
        // not the textbox fallback (which is only reached when no predicate matches).
        Type? matchedDisplayer = null;
        foreach (KeyValuePair<Func<Type, bool>, Type> kvp in SingleDataUiContainer.TypeDisplayerAssociation)
        {
            if (kvp.Key(typeof(SampleEnum?)))
            {
                matchedDisplayer = kvp.Value;
                break;
            }
        }

        matchedDisplayer.ShouldBe(typeof(ComboBoxDisplay));
    }

    [Fact]
    public void SingleDataUiContainer_RoutingPredicate_NonNullableEnum_StillRoutesToComboBoxDisplay()
    {
        // Regression guard: do not break the existing non-nullable enum routing
        // when adding the nullable-enum branch.
        Type? matchedDisplayer = null;
        foreach (KeyValuePair<Func<Type, bool>, Type> kvp in SingleDataUiContainer.TypeDisplayerAssociation)
        {
            if (kvp.Key(typeof(SampleEnum)))
            {
                matchedDisplayer = kvp.Value;
                break;
            }
        }

        matchedDisplayer.ShouldBe(typeof(ComboBoxDisplay));
    }

    [Fact]
    public void SingleDataUiContainer_RoutingPredicate_NullablePrimitive_NotRoutedToComboBoxDisplay()
    {
        // Make sure the new "nullable enum" predicate doesn't accidentally match
        // typeof(int?) etc. — only Nullable<TEnum> should reach ComboBoxDisplay.
        Type? matchedDisplayer = null;
        foreach (KeyValuePair<Func<Type, bool>, Type> kvp in SingleDataUiContainer.TypeDisplayerAssociation)
        {
            if (kvp.Key(typeof(int?)))
            {
                matchedDisplayer = kvp.Value;
                break;
            }
        }

        matchedDisplayer.ShouldBeNull();
    }

    [StaFact]
    public void ComboBoxDisplay_NullableEnumPropertyType_CustomOptionsIncludesNullEntryAndAllEnumValues()
    {
        TestComboBoxDisplay display = new TestComboBoxDisplay();
        InstanceMember member = MakeMemberOfType(typeof(SampleEnum?));
        display.InstanceMember = member;

        List<object?> options = display.GetCustomOptions().ToList();

        // Convention: the no-value sentinel is the "<None>" string, matching
        // StateReferencingInstanceMember.HandleCustomSet which translates that
        // string back to null on commit. Using a string sentinel rather than
        // literal null is required because WPF ComboBox does not handle a null
        // SelectedItem cleanly — selecting the null item deselects instead.
        options.Count(o => Equals(o, ComboBoxDisplay.NullSentinel)).ShouldBe(1, "exactly one <None> entry should be present for the no-value option");
        options.ShouldContain(SampleEnum.Alpha);
        options.ShouldContain(SampleEnum.Beta);
        options.ShouldContain(SampleEnum.Gamma);
        options.Count.ShouldBe(4);
    }

    [StaFact]
    public void ComboBoxDisplay_NonNullableEnumPropertyType_CustomOptionsHasNoNullEntry()
    {
        TestComboBoxDisplay display = new TestComboBoxDisplay();
        InstanceMember member = MakeMemberOfType(typeof(SampleEnum));
        display.InstanceMember = member;

        List<object?> options = display.GetCustomOptions().ToList();

        options.ShouldNotContain(ComboBoxDisplay.NullSentinel);
        options.Count.ShouldBe(3);
    }

    [StaFact]
    public void ComboBoxDisplay_NullableEnum_SelectingNoneSentinelWritesSentinelToInstanceMember()
    {
        // The WpfDataUi layer hands the "<None>" sentinel string straight to
        // InstanceMember.SetValue. The null translation happens one level up,
        // in StateReferencingInstanceMember.HandleCustomSet, which is exercised
        // by the variable-grid integration tests rather than here. This test
        // pins the contract this layer owns: the sentinel reaches SetValue
        // intact when the user selects the no-value entry.
        object? backingValue = SampleEnum.Beta;

        TestComboBoxDisplay display = new TestComboBoxDisplay();
        InstanceMember member = MakeMemberOfType(typeof(SampleEnum?));
        member.CustomGetEvent += _ => backingValue;
        member.CustomSetPropertyEvent += (_, args) => backingValue = args.Value;
        display.InstanceMember = member;

        display.SimulateUserSelects(ComboBoxDisplay.NullSentinel);

        backingValue.ShouldBe(ComboBoxDisplay.NullSentinel);
    }

    [StaFact]
    public void ComboBoxDisplay_NullableEnum_RoundTripsRealEnumValue()
    {
        SampleEnum? backingValue = null;

        TestComboBoxDisplay display = new TestComboBoxDisplay();
        InstanceMember member = MakeMemberOfType(typeof(SampleEnum?));
        member.CustomGetEvent += _ => backingValue;
        member.CustomSetPropertyEvent += (_, args) => backingValue = (SampleEnum?)args.Value;
        display.InstanceMember = member;

        display.SimulateUserSelects(SampleEnum.Gamma);

        backingValue.ShouldBe(SampleEnum.Gamma);
    }

    private static InstanceMember MakeMemberOfType(Type type)
    {
        InstanceMember member = new InstanceMember { Name = "TestMember" };
        member.CustomGetTypeEvent += _ => type;
        // CustomGetEvent must be set for IsDefined to be true — give a default no-op
        // that callers can override with += to add side-effects.
        member.CustomGetEvent += _ => null;
        return member;
    }

    /// <summary>
    /// Test harness that exposes the protected <c>CustomOptions</c> for assertion
    /// and provides a way to simulate user selection without a WPF dispatcher.
    /// </summary>
    private sealed class TestComboBoxDisplay : ComboBoxDisplay
    {
        public IEnumerable<object?> GetCustomOptions()
        {
            // Reflection over the protected property — keeps the test independent of
            // whether the displayer exposes it directly.
            System.Reflection.PropertyInfo? prop = typeof(ComboBoxDisplay).GetProperty(
                "CustomOptions",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            prop.ShouldNotBeNull();
            System.Collections.IEnumerable? raw = (System.Collections.IEnumerable?)prop!.GetValue(this);
            raw.ShouldNotBeNull();
            List<object?> result = new List<object?>();
            foreach (object? item in raw!)
            {
                result.Add(item);
            }
            return result;
        }

        public void SimulateUserSelects(object? value)
        {
            // Bypass WPF SelectionChanged plumbing — drive the same code path the
            // selection change uses to commit a value.
            // The extension method declares object (not object?), but passing null is
            // exactly what we want to verify here — null-forgive to silence the warning.
            this.TrySetValueOnInstance(value!);
        }

    }
}
