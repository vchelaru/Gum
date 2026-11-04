using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Gum.Avalonia.Plugins.VariableGrid;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Wireframe;
using GumRuntime;
using Microsoft.Extensions.DependencyInjection;
using RenderingLibrary.Graphics;
using Shouldly;
using WpfDataUi.DataTypes;

namespace Gum.Avalonia.Tests.VariableGrid;

/// <summary>
/// A float vector shader parameter is one row in the Variables tab: the X, Y, Z and W fields side by side with their
/// letters to the left, one set marker and one right-click menu for the whole parameter, and any edit saves every
/// component as one undo step.
/// </summary>
public class ShaderVectorRowScenarioTests : IDisposable
{
    private const string Tint = "Box.Outline_Tint__";

    public void Dispose() => ShaderParameterReader.Reader = null;

    private static InstanceSave ShowAVectorParameter(VariableGridHarness grid, int componentCount = 4)
    {
        ShaderParameterReader.Reader = _ => new List<ShaderParameterInfo>
        {
            new ShaderParameterInfo("Thickness", ShaderParameterKind.Float, 1, ShaderParameterValue.FromFloat(2f)),
            new ShaderParameterInfo("Tint", ShaderParameterKind.Float, componentCount, componentCount == 2
                ? ShaderParameterValue.FromVector2(0.5f, 0.25f)
                : ShaderParameterValue.FromVector4(0.1f, 0.2f, 0.3f, 0.4f)),
        };
        ComponentSave card = grid.Project.AddComponent("Card");
        InstanceSave box = grid.Project.AddInstance(card, "Box", "Container");
        card.GetDefaultStateOrThrow().SetValue("Box.SourceShaderFile", "Shaders/Outline.slang", "string");

        AttachEffect(box);

        grid.Select(box);
        grid.Settle();
        grid.Row("IsRenderTarget").GetVisualDescendants().OfType<CheckBox>().First().IsChecked = true;
        grid.Settle();
        return box;
    }

    // The editor rebuilds its scene after a Make Default, dropping the stand-in for the compiled shader.
    private static void AttachEffect(InstanceSave box)
    {
        GraphicalUiElement representation = new GraphicalUiElement(new InvisibleRenderable { RenderTargetEffect = new object() }) { Tag = box };
        ((WireframeObjectManager)TestAppBuilder.Services.GetRequiredService<IWireframeObjectManager>()).AllIpsos.Add(representation);
    }

    private static ShaderVectorDisplay Vector(VariableGridHarness grid) => grid.Editor<ShaderVectorDisplay>("Tint");

    private static object? Saved(VariableGridHarness grid, string suffix) =>
        VariableGridHarness.StoredValue(grid.SelectedState.SelectedElement!, Tint + suffix);

    [AvaloniaFact]
    public void AVector_IsOneRow_WithItsLettersLeftOfFieldsOnTheSameLine()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ShowAVectorParameter(grid);

        grid.ShownMemberNames().ShouldContain("Tint");
        grid.ShownMemberNames().ShouldNotContain("Tint X");
        ShaderVectorDisplay row = Vector(grid);
        row.Cells.Count.ShouldBe(4);
        double[] tops = row.Cells.Select(cell => cell.TranslatePoint(new global::Avalonia.Point(0, 0), grid.Input.Window)!.Value.Y).ToArray();
        tops.ShouldAllBe(top => Math.Abs(top - tops[0]) < 0.5);
        double[] lefts = row.Cells.Select(cell => cell.TranslatePoint(new global::Avalonia.Point(0, 0), grid.Input.Window)!.Value.X).ToArray();
        lefts.ShouldBe(lefts.OrderBy(left => left).ToArray());
        row.Cells.Select(cell => cell.InstanceMember!.DisplayName).ToArray().ShouldBe(new[] { "X", "Y", "Z", "W" });
        row.Cells.Select(cell => cell.TextBox.Text).ToArray().ShouldBe(new[] { "0.1", "0.2", "0.3", "0.4" });
    }

    [AvaloniaFact]
    public void TypingInOneField_SavesEveryComponent_AndOneUndoRemovesThemAll()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ShowAVectorParameter(grid);

        grid.Input.TypeAndEnter(Vector(grid).Cells[2].TextBox, "0.9");

        Saved(grid, "X").ShouldBe(0.1f);
        Saved(grid, "Y").ShouldBe(0.2f);
        Saved(grid, "Z").ShouldBe(0.9f);
        Saved(grid, "W").ShouldBe(0.4f);
        grid.ShowsSetMarker("Tint").ShouldBeTrue();

        grid.Undo();

        Saved(grid, "X").ShouldBeNull();
        Saved(grid, "W").ShouldBeNull();
    }

    [AvaloniaFact]
    public void RightClickOnAField_OffersTheWholeParametersMenu_AndMakeDefaultClearsEveryComponent()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        InstanceSave box = ShowAVectorParameter(grid);
        grid.Input.TypeAndEnter(Vector(grid).Cells[1].TextBox, "0.7");

        grid.Input.RightClick(Vector(grid).Cells[3]);
        List<string> headers = grid.Input.ContextMenuHeaders();
        grid.Input.PickContextMenuItem("Make Default");
        grid.Settle();
        AttachEffect(box);
        grid.GridManager.RefreshEntireGrid(force: true);
        grid.Settle();

        headers.ShouldContain("Make Default");
        headers.ShouldContain("Expose Variable");
        Saved(grid, "X").ShouldBeNull();
        Saved(grid, "Y").ShouldBeNull();
        Saved(grid, "W").ShouldBeNull();
        grid.ShowsSetMarker("Tint").ShouldBeFalse();
        Vector(grid).Cells[1].TextBox.Text.ShouldBe("0.2");
    }

    private static string?[] ExposedNames(VariableGridHarness grid) =>
        new[] { "X", "Y", "Z", "W" }
            .Select(suffix => grid.SelectedState.SelectedElement!.DefaultState.GetVariableSave(Tint + suffix)?.ExposedAsName)
            .ToArray();

    [AvaloniaFact]
    public void ExposingTheVector_ExposesEveryComponentAfterOnePrompt_AsOneUndoStep()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ShowAVectorParameter(grid);
        grid.Project.Dialogs.AnswerNext<Gum.Dialogs.ExposeColorDialogViewModel>(_ => true);

        grid.Input.RightClick(Vector(grid).Cells[1]);
        grid.Input.PickContextMenuItem("Expose Variable");
        grid.Settle();

        string?[] exposed = ExposedNames(grid);
        exposed.ShouldAllBe(name => !string.IsNullOrEmpty(name));
        exposed.Select(name => name![^3..]).ToArray().ShouldBe(new[] { "__X", "__Y", "__Z", "__W" });
        exposed.Select(name => name![..^3]).Distinct().Count().ShouldBe(1, "one base name for the whole vector");
        Saved(grid, "Z").ShouldBe(0.3f, "the components carry their values");

        grid.Undo();

        ExposedNames(grid).ShouldAllBe(name => string.IsNullOrEmpty(name));
    }

    [AvaloniaFact]
    public void CancellingTheExposePrompt_ChangesNothing()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ShowAVectorParameter(grid);
        grid.Project.Dialogs.AnswerNext<Gum.Dialogs.ExposeColorDialogViewModel>(_ => false);

        grid.Input.RightClick(Vector(grid).Cells[0]);
        grid.Input.PickContextMenuItem("Expose Variable");
        grid.Settle();

        ExposedNames(grid).ShouldAllBe(name => string.IsNullOrEmpty(name));
        Saved(grid, "X").ShouldBeNull("nothing was pinned either");
    }

    [AvaloniaFact]
    public void AnExposedVector_OffersUnexpose_AndUnexposingClearsEveryComponent()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ShowAVectorParameter(grid);
        grid.Project.Dialogs.AnswerNext<Gum.Dialogs.ExposeColorDialogViewModel>(_ => true);
        grid.Input.RightClick(Vector(grid).Cells[0]);
        grid.Input.PickContextMenuItem("Expose Variable");
        grid.Settle();

        grid.Input.RightClick(Vector(grid).Cells[2]);
        List<string> headers = grid.Input.ContextMenuHeaders();
        string unexpose = headers.First(header => header.StartsWith("Un-expose"));
        grid.Input.PickContextMenuItem(unexpose);
        grid.Settle();

        headers.ShouldNotContain("Expose Variable");
        ExposedNames(grid).ShouldAllBe(name => string.IsNullOrEmpty(name));
    }

    [AvaloniaFact]
    public void TheRowsSetMarker_ShowsOnceForTheWholeParameter()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ShowAVectorParameter(grid, componentCount: 2);

        grid.ShowsSetMarker("Tint").ShouldBeFalse();

        grid.Input.TypeAndEnter(Vector(grid).Cells[0].TextBox, "0.75");

        grid.ShowsSetMarker("Tint").ShouldBeTrue();
    }
}
