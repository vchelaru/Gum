using Gum.DataTypes;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// An instance of a component whose file is missing, on the tool's real canvas. The tool consumes missing
/// files silently, so the instance builds as an empty container (#7963) and a click on it selects it.
/// Same conventions as <see cref="CanvasScenarioTests"/>.
/// </summary>
[Trait("Category", "EndToEnd")]
public class MissingComponentCanvasTests
{
    [SkippableFact]
    public void InstanceOfMissingComponent_IsAnEmptyContainer_AndSelectsOnClick()
    {
        Skip.IfNot(CanvasHarness.CanRun, CanvasHarness.SkipReason);
        CanvasHarness.OnUiThread(() =>
        {
            using CanvasHarness canvas = new CanvasHarness();
            ComponentSave missing = canvas.Project.AddComponent("Missing");
            ComponentSave host = canvas.Project.AddComponent("Host");
            InstanceSave instance = canvas.AddInstance(host, "MissingInstance", "Missing", x: 100, y: 100, width: 80, height: 40);
            // What the project loader leaves for a component whose file is gone: a placeholder with no base type.
            missing.BaseType = null;
            missing.IsSourceFileMissing = true;
            canvas.Wireframe.RefreshAll(forceLayout: true);
            canvas.Frame();
            canvas.Tree.Click(canvas.Tree.NodeFor(host));

            GraphicalUiElement representation = canvas.Wireframe.GetRepresentation(instance).ShouldNotBeNull(canvas.Describe());
            representation.RenderableComponent.ShouldBeOfType<InvisibleRenderable>(canvas.Describe());

            canvas.Click(canvas.WindowPointOf(140, 120));
            canvas.Project.SelectedState.SelectedInstance.ShouldNotBeNull(canvas.Describe()).Name.ShouldBe("MissingInstance", canvas.Describe());
        });
    }
}
