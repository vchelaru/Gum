using Gum.Managers;
using Shouldly;
using WpfDataUi.Controls;
using WpfDataUi.DataTypes;

namespace GumToolUnitTests.PropertyGridHelpers;

public class PropertyGridManagerStringDisplayerTests : BaseTestClass
{
    [Fact]
    public void ApplyLocalizedOrMultilineStringDisplayer_LocalizationDisabled_AssignsMultiLineTextBoxDisplay()
    {
        InstanceMember member = MakeStringMember("ButtonInstance.ToolTip");

        PropertyGridManager.ApplyLocalizedOrMultilineStringDisplayer(
            member,
            hasLocalizationDatabase: false,
            sortedKeys: Array.Empty<string>());

        member.PreferredDisplayer.ShouldBe(typeof(MultiLineTextBoxDisplay));
        member.CustomOptions.ShouldBeNull();
    }

    [Fact]
    public void ApplyLocalizedOrMultilineStringDisplayer_LocalizationEnabled_AssignsEditableComboBoxWithKeys()
    {
        InstanceMember member = MakeStringMember("ButtonInstance.ToolTip");
        string[] keys = new[] { "ZKey", "AKey", "MKey" };

        PropertyGridManager.ApplyLocalizedOrMultilineStringDisplayer(
            member,
            hasLocalizationDatabase: true,
            sortedKeys: keys);

        member.PreferredDisplayer.ShouldBe(typeof(ComboBoxDisplay));
        member.PropertiesToSetOnDisplayer[nameof(ComboBoxDisplay.IsEditable)].ShouldBe(true);
        // Method itself does not sort — caller hands sorted keys; verify pass-through.
        member.CustomOptions.ShouldBe(keys);
    }

    [Fact]
    public void IsEligibleStringDisplayerName_TextOrToolTipOnly_ReturnsTrue()
    {
        // Behavior-declared FormsProperties have no base variable; eligibility falls back
        // to the trailing identifier of member.Name (FormsProperty.Name). Standard Text
        // members resolve through ObjectFinder and pass via baseVariableName.
        PropertyGridManager.IsEligibleStringDisplayerRootName("Text").ShouldBeTrue();
        PropertyGridManager.IsEligibleStringDisplayerRootName("ToolTip").ShouldBeTrue();
        PropertyGridManager.IsEligibleStringDisplayerRootName("Foo").ShouldBeFalse();
        PropertyGridManager.IsEligibleStringDisplayerRootName(null).ShouldBeFalse();
    }

    private static InstanceMember MakeStringMember(string name)
    {
        InstanceMember member = new InstanceMember { Name = name };
        member.CustomGetTypeEvent += _ => typeof(string);
        return member;
    }
}
