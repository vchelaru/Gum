using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.ProjectServices.Editing;
using Shouldly;

namespace Gum.ProjectServices.Tests.Editing;

public class VariableReferenceValidatorTests : BaseTestClass
{
    [Fact]
    public void GetFailures_ReportsOnlyTheInvalidLines_AndValidatesACompositeLineByItsChannels()
    {
        List<VariableSave> rectangleVariables = Project.StandardElements.Single(item => item.Name == "ColoredRectangle").DefaultState!.Variables;
        rectangleVariables.Add(new VariableSave { Name = "Width", Type = "float", Value = 10f });
        foreach (string channel in new[] { "Red", "Green", "Blue" })
        {
            rectangleVariables.Add(new VariableSave { Name = channel, Type = "int", Value = 255 });
        }
        ComponentSave hud = EditingTestProject.AddComponent(Project, "Hud");
        EditingTestProject.AddInstance(hud, "Source", "ColoredRectangle");
        InstanceSave target = EditingTestProject.AddInstance(hud, "Target", "ColoredRectangle");
        VariableReferenceValidator validator = new VariableReferenceValidator();

        List<(string Line, ToolsUtilities.GeneralResponse Response)> failures = validator.GetFailures(hud, target, new[]
        {
            "Width = Source.Width * 2",
            "Color = Source.Color",
            "// a comment",
            "Widht = 4",
            "Color = Nope.Color",
        }, liveRoot: null);

        failures.Select(item => item.Line).ShouldBe(new[] { "Widht = 4", "Color = Nope.Color" });
        failures[0].Response.Message.ShouldContain("Widht");
    }

    [Fact]
    public void NormalizeLines_ExpandsShorthandAndUnqualifiesTheLeftSide()
    {
        List<string> lines = new List<string> { "Source.Width", "Target.Height = Source.Height", "// Source.X" };

        new VariableReferenceValidator().NormalizeLines(lines);

        lines.ShouldBe(new[] { "Width = Source.Width", "Height=Source.Height", "// Source.X" });
    }
}
