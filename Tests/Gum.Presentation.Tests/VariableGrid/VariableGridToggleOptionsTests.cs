using System.Linq;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.ToolStates;
using Moq;
using Shouldly;
using WpfDataUi.Controls;

namespace Gum.Presentation.Tests.VariableGrid;

public class VariableGridToggleOptionsTests : BaseTestClass
{
    [Fact]
    public void Exclude_DropsTheValuesAStandardElementExcludes()
    {
        VariableGridToggleOptions sut = new VariableGridToggleOptions(Mock.Of<ISelectedState>());
        (string elementName, VariableSave? widthUnits) = StandardElementsManager.Self.DefaultStates
            .Select(kvp => (kvp.Key, kvp.Value.Variables.FirstOrDefault(v => v.Name == "WidthUnits")))
            .First(pair => pair.Item2?.ExcludedValuesForEnum.Count > 0);
        widthUnits.ShouldNotBeNull();

        ToggleButtonOption[] allowed = sut.Exclude(sut.AllWidthUnits, "WidthUnits", elementName);

        allowed.Length.ShouldBe(sut.AllWidthUnits.Length - widthUnits.ExcludedValuesForEnum.Count);
        foreach (object excluded in widthUnits.ExcludedValuesForEnum)
        {
            allowed.ShouldNotContain(option => option.Value.Equals(excluded));
        }
    }

    [Fact]
    public void Exclude_ReturnsTheSameSet_WhenNothingIsExcluded()
    {
        VariableGridToggleOptions sut = new VariableGridToggleOptions(Mock.Of<ISelectedState>());

        sut.Exclude(sut.AllWidthUnits, "WidthUnits", rootElementName: null).ShouldBeSameAs(sut.AllWidthUnits);
        sut.GetYOrigins().ShouldBeSameAs(sut.AllYOrigins);
    }

    [Fact]
    public void OptionSets_AreStableAndCarryIcons()
    {
        VariableGridToggleOptions sut = new VariableGridToggleOptions(Mock.Of<ISelectedState>());

        sut.XUnits.ShouldBeSameAs(sut.XUnits);
        sut.XUnits.ShouldAllBe(option => option.GumIconName != null);
        sut.TextHorizontalAlignment.ShouldAllBe(option => option.ImagePath != null);
    }

    [Fact]
    public void CornerRadiusDisplayLogic_ComposesLinkedAndUnlinkedValues()
    {
        CornerRadiusDisplayLogic logic = new CornerRadiusDisplayLogic();
        CornerRadiusComposite current = new CornerRadiusComposite(4, null, null, null, null);

        logic.Compose(isLinked: true, "6", "1", "2", "3", "4", current).ShouldBe(new CornerRadiusComposite(6, null, null, null, null));
        logic.Compose(isLinked: false, "6", "1", "", "3", "x", current).ShouldBe(new CornerRadiusComposite(6, 1, null, 3, null));
        logic.Compose(isLinked: true, "bad", "", "", "", "", current).Uniform.ShouldBe(4f);

        logic.FormatFloat(2.5f).ShouldBe("2.5");
        logic.FormatNullableFloat(null).ShouldBe("");
        logic.ParseFloat("1.25").ShouldBe(1.25f);
    }
}
