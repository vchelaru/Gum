using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.ProjectServices;
using GumRuntime;
using Shouldly;
using SkiaSharp;

namespace Gum.Cli.Tests;

/// <summary>
/// Render commands evaluate variable references the way the tool does (#8240): arithmetic on the
/// right side goes through Gum.Expressions, and a reference that still cannot apply prints a warning.
/// </summary>
public class ScreenshotVariableReferenceTests : BaseTestClass
{
    [Fact]
    public void Screenshot_ArithmeticReferenceReadingParentOverride_RendersEvaluatedWidth()
    {
        // Other commands in this process wire the evaluator, so start unwired: screenshot must not
        // depend on anything having initialized it.
        ElementSaveExtensions.CustomEvaluateExpression = null;
        string projectPath = CreateSegmentBarProject("TotalHealth * SegmentWidth");

        CliTestHelper result = RenderScreen(projectPath, out SKBitmap bitmap);

        using (bitmap)
        {
            result.ExitCode.ShouldBe(0, result.StandardError);
            // 23 * 4 = 92 wide. The stored width is 32.
            bitmap.GetPixel(90, 8).ShouldBe(SKColors.White);
            bitmap.GetPixel(94, 8).ShouldBe(SKColors.Black);
        }
    }

    [Fact]
    public void Screenshot_ExpressionThatCannotApply_PrintsWarningAndKeepsStoredWidth()
    {
        ElementSaveExtensions.CustomEvaluateExpression = null;
        string projectPath = CreateSegmentBarProject("TotalHealth > 3");

        CliTestHelper result = RenderScreen(projectPath, out SKBitmap bitmap);

        using (bitmap)
        {
            result.ExitCode.ShouldBe(0, result.StandardError);
            // Once, although both the project's data pass and the built Bar apply the row.
            string warning = "Variable reference 'Width = TotalHealth > 3' was not applied in SegmentBar (Container): "
                + "TotalHealth > 3 is a bool and Total.Width is a float, which cannot be converted.";
            result.StandardError.Split('\n').Count(line => line.Contains(warning)).ShouldBe(1, result.StandardError);
            bitmap.GetPixel(30, 8).ShouldBe(SKColors.White);
            bitmap.GetPixel(34, 8).ShouldBe(SKColors.Black);
        }
    }

    [Theory]
    [InlineData("TotalHealth *", "is not a valid expression")]
    [InlineData("TotalHealth / 0", "divides by zero")]
    public void Screenshot_ExpressionThatCannotEvaluate_PrintsWarningOnce(string rightSide, string problem)
    {
        string projectPath = CreateSegmentBarProject(rightSide);

        CliTestHelper result = RenderScreen(projectPath, out SKBitmap bitmap);

        using (bitmap)
        {
            result.ExitCode.ShouldBe(0, result.StandardError);
            string warning = $"Variable reference 'Width = {rightSide}' was not applied in SegmentBar (Container): "
                + $"{rightSide} {problem}. Total.Width keeps its current value.";
            result.StandardError.Split('\n').Count(line => line.Contains(warning)).ShouldBe(1, result.StandardError);
            bitmap.GetPixel(30, 8).ShouldBe(SKColors.White);
            bitmap.GetPixel(34, 8).ShouldBe(SKColors.Black);
        }
    }

    [Theory]
    [InlineData("TotalHealth *", "is not a valid expression")]
    [InlineData("TotalHealth / 0", "divides by zero")]
    public void Check_ExpressionThatCannotEvaluate_ReportsGum0047(string rightSide, string problem)
    {
        string projectPath = CreateSegmentBarProject(rightSide);

        CliTestHelper result = CliTestHelper.Run("check", projectPath);

        result.StandardOutput.ShouldContain(
            $"warning: SegmentBar: Default: the Total.VariableReferences line \"Width = {rightSide}\" {problem}, so it never applies.");
    }

    private CliTestHelper RenderScreen(string projectPath, out SKBitmap bitmap)
    {
        string outputPath = Path.Combine(TempDirectory, "Screen.png");

        CliTestHelper result = CliTestHelper.Run(
            "screenshot", projectPath, "Screen", "--output", outputPath,
            "--backend", "skia", "--background", "000000", "--width", "128", "--height", "16");

        bitmap = SKBitmap.Decode(outputPath);
        return result;
    }

    /// <summary>
    /// Builds component SegmentBar (int custom variables TotalHealth = 4 and SegmentWidth = 4, a filled
    /// white Rectangle Total with stored Width 32 and the reference <c>Width = rightSide</c>) and a
    /// Screen whose instance Bar sets TotalHealth to 23.
    /// </summary>
    private string CreateSegmentBarProject(string rightSide)
    {
        string projectPath = Path.Combine(TempDirectory, "Project.gumx");
        GumProjectSave project = new ProjectCreator().Create(projectPath);

        ComponentSave bar = new ComponentSave { Name = "SegmentBar", BaseType = "Container" };
        StateSave barState = new StateSave { Name = "Default", ParentContainer = bar };
        bar.States.Add(barState);
        bar.Instances.Add(new InstanceSave { Name = "Total", BaseType = "Rectangle", ParentContainer = bar });
        barState.Variables.Add(new VariableSave { Name = "TotalHealth", Type = "int", Value = 4, IsCustomVariable = true, SetsValue = true });
        barState.Variables.Add(new VariableSave { Name = "SegmentWidth", Type = "int", Value = 4, IsCustomVariable = true, SetsValue = true });
        barState.Variables.Add(new VariableSave { Name = "Total.Width", Type = "float", Value = 32f, SetsValue = true });
        barState.Variables.Add(new VariableSave { Name = "Total.Height", Type = "float", Value = 16f, SetsValue = true });
        barState.Variables.Add(new VariableSave { Name = "Total.IsFilled", Type = "bool", Value = true, SetsValue = true });
        barState.VariableLists.Add(new VariableListSave<string>
        {
            Name = "Total.VariableReferences",
            Type = "string",
            Value = new List<string> { $"Width = {rightSide}" },
        });

        ScreenSave screen = new ScreenSave { Name = "Screen" };
        StateSave screenState = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(screenState);
        screen.Instances.Add(new InstanceSave { Name = "Bar", BaseType = "SegmentBar", ParentContainer = screen });
        screenState.Variables.Add(new VariableSave { Name = "Bar.TotalHealth", Type = "int", Value = 23, SetsValue = true });

        project.Components.Add(bar);
        project.ComponentReferences.Add(new ElementReference { Name = bar.Name, ElementType = ElementType.Component });
        project.Screens.Add(screen);
        project.ScreenReferences.Add(new ElementReference { Name = screen.Name, ElementType = ElementType.Screen });
        project.Save(projectPath, saveElements: true);

        return projectPath;
    }
}
