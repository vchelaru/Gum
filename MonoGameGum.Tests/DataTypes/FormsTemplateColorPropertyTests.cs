using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Wireframe;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.DataTypes;

/// <summary>
/// ExposedAsName forwards to a named child instance's property, so the underlying Name must
/// be instance-qualified (e.g. "LinesSprite.ColorCategoryState"). Issue #4079's migration
/// briefly promoted CautionLines/VerticalLines/Icon/PercentBar/PercentBarIcon's exposed color
/// property to live unqualified on the component itself while keeping ExposedAsName - a shape
/// gumcli check/check-references/diff-standards all pass (structurally valid XML), but that
/// GraphicalUiElement.SetProperty crashes at runtime (ArgumentOutOfRangeException at
/// Substring(0, -1)). Pin that GraphicalUiElement no longer crashes on the malformed shape -
/// it should degrade gracefully, not tear down the whole load.
/// </summary>
public class FormsTemplateColorPropertyTests : BaseTestClass
{
    [Fact]
    public void SetProperty_ExposedVariableWithUnqualifiedUnderlyingName_ShouldNotThrow()
    {
        ComponentSave component = new ComponentSave { Name = "Broken" };
        component.States.Add(new StateSave { Name = "Default" });
        component.DefaultState.Variables.Add(new VariableSave
        {
            Type = "string",
            Name = "SomeProperty", // unqualified - no instance dot
            ExposedAsName = "Friendly",
            SetsValue = false,
        });

        GraphicalUiElement gue = component.ToGraphicalUiElement();

        Should.NotThrow(() => gue.SetProperty("Friendly", "value"));
    }
}
