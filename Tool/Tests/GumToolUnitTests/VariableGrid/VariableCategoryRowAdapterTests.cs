using System.Collections.Generic;
using System.Linq;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Shouldly;
using WpfDataUi.DataTypes;
using Xunit;

namespace GumToolUnitTests.VariableGrid;

public class VariableCategoryRowAdapterTests : BaseTestClass
{
    /// <summary>
    /// Builds a grid row backed by a local value rather than by reflection over an instance, so a test can
    /// read and write it without standing up a real <c>StateReferencingInstanceMember</c>.
    /// </summary>
    private static InstanceMember CreateMember(string name, object? initialValue)
    {
        object? backingValue = initialValue;
        InstanceMember member = new InstanceMember(name, null!);
        member.CustomGetEvent += (_) => backingValue;
        member.CustomGetTypeEvent += (_) => typeof(object);
        member.CustomSetPropertyEvent += (_, args) => backingValue = args.Value;
        return member;
    }

    [Fact]
    public void CreateRows_ShouldExpandACompositeRowIntoItsChannels()
    {
        InstanceMember red = CreateMember("Red", 10);
        InstanceMember green = CreateMember("Green", 20);
        CompositeInstanceMember composite = new CompositeInstanceMember(
            "Color",
            new List<InstanceMember> { red, green },
            typeof(int),
            channelValues => (int)channelValues[0]!,
            compositeValue => new object?[] { compositeValue, compositeValue });
        InstanceMember fontSize = CreateMember("FontSize", 36);

        List<IVariableCategoryRow> rows =
            VariableCategoryRowAdapter.CreateRows(new List<InstanceMember> { composite, fontSize });

        rows.Select(row => row.RootVariableName).ShouldBe(new[] { "Red", "Green", "FontSize" });
        rows[0].Value.ShouldBe(10);
    }

    [Fact]
    public void TrySetValue_ShouldWriteThroughAMultiSelectRowToEveryWrappedMember()
    {
        InstanceMember firstInstanceFontSize = CreateMember("FontSize", 12);
        InstanceMember secondInstanceFontSize = CreateMember("FontSize", 14);
        MultiSelectInstanceMember multiSelect = new MultiSelectInstanceMember
        {
            Name = "FontSize",
            InstanceMembers = new List<InstanceMember> { firstInstanceFontSize, secondInstanceFontSize }
        };

        List<IVariableCategoryRow> rows =
            VariableCategoryRowAdapter.CreateRows(new List<InstanceMember> { multiSelect });

        rows.Single().RootVariableName.ShouldBe("FontSize");
        rows.Single().TrySetValue(36).ShouldBeTrue();

        firstInstanceFontSize.Value.ShouldBe(36);
        secondInstanceFontSize.Value.ShouldBe(36);
    }
}
