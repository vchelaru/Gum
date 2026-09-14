using System.Reflection;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Shouldly;
using WpfDataUi.Controls;
using WpfDataUi.DataTypes;

namespace GumToolUnitTests.PropertyGridHelpers;

/// <summary>
/// FlatRedBall#1755: a default-valued combo box variable should get the same green
/// background <see cref="DataUiBrushes"/> gives for a default text field, not a
/// green foreground.
/// </summary>
public class ComboBoxDisplayDefaultBackgroundTests : BaseTestClass
{
    private enum SampleEnum
    {
        Alpha,
        Beta
    }

    [StaFact]
    public void ComboBoxDisplay_InstanceMemberIsDefault_SetsGreenBackground()
    {
        ComboBoxDisplay display = new ComboBoxDisplay();
        DefaultableInstanceMember member = MakeMember(isDefault: true);

        display.InstanceMember = member;
        PumpDispatcher();

        ComboBox comboBox = GetComboBox(display);
        // Reference-equality via a bool, not ShouldBe(brush): Shouldly's failure-message
        // formatter calls Brush.ToString(), which throws off the owning thread.
        ReferenceEquals(comboBox.Background, DataUiBrushes.DefaultValueBackground).ShouldBeTrue();
    }

    [StaFact]
    public void ComboBoxDisplay_InstanceMemberNotDefault_DoesNotSetGreenBackground()
    {
        ComboBoxDisplay display = new ComboBoxDisplay();
        DefaultableInstanceMember member = MakeMember(isDefault: false);

        display.InstanceMember = member;
        PumpDispatcher();

        ComboBox comboBox = GetComboBox(display);
        ReferenceEquals(comboBox.Background, DataUiBrushes.DefaultValueBackground).ShouldBeFalse();
    }

    private static DefaultableInstanceMember MakeMember(bool isDefault)
    {
        DefaultableInstanceMember member = new DefaultableInstanceMember
        {
            Name = "TestMember",
            ForcedIsDefault = isDefault
        };
        member.CustomGetTypeEvent += _ => typeof(SampleEnum);
        member.CustomGetEvent += _ => SampleEnum.Alpha;
        return member;
    }

    private static ComboBox GetComboBox(ComboBoxDisplay display)
    {
        PropertyInfo? property = typeof(ComboBoxDisplay).GetProperty(
            "ComboBox",
            BindingFlags.Instance | BindingFlags.NonPublic);
        property.ShouldNotBeNull();
        ComboBox? comboBox = (ComboBox?)property!.GetValue(display);
        comboBox.ShouldNotBeNull();
        return comboBox!;
    }

    /// <summary>
    /// Pumps the calling thread's Dispatcher once so a pending
    /// <see cref="Dispatcher.BeginInvoke(System.Delegate)"/> callback (e.g. the
    /// background-color sync in <c>ComboBoxDisplay</c>) runs before assertions.
    /// </summary>
    private static void PumpDispatcher()
    {
        DispatcherFrame frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
    }

    private sealed class DefaultableInstanceMember : InstanceMember
    {
        public bool ForcedIsDefault { get; set; }

        public override bool IsDefault => ForcedIsDefault;
    }
}
