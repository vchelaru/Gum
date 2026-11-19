using Avalonia.Threading;
using Gum.DataTypes;
using Gum.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// The tool creates its render surface after the startup project has loaded, and the canvas
/// plugins only start listening then, so they catch up with whatever happened before. The surface
/// comes up once per process, so these repeat the catch-up on the shared canvas. Same conventions
/// as <see cref="CanvasScenarioTests"/>.
/// </summary>
[Trait("Category", "EndToEnd")]
public class RenderSurfaceCatchUpTests
{
    // An edit made before the surface was ready reached no canvas; the catch-up builds from the
    // project's current values, so the canvas shows the edit.
    [Fact]
    public void EditorCatchUp_ShowsAnEditTheCanvasDidNotHear()
    {
        OnCanvas(canvas =>
        {
            ComponentSave button = canvas.Project.AddComponent("Button");
            InstanceSave box = canvas.AddInstance(button, "Box", "Rectangle", x: 20, y: 20, width: 60, height: 40);
            canvas.Tree.Click(canvas.Tree.NodeFor(button));
            button.DefaultState!.SetValue("Box.X", 150f, "float");
            canvas.Wireframe.GetRepresentation(box)!.X.ShouldBe(20, "the edit is made without telling the canvas");

            canvas.Plugin.RequestCatchUpWithProjectAndSelection();
            Dispatcher.UIThread.RunJobs();

            canvas.Wireframe.GetRepresentation(box)!.X.ShouldBe(150);
            canvas.Frame();
        });
    }

    // A project load rebuilds the canvas once. One that lands after the surface is ready but
    // before the posted catch-up runs (an open request from a second launch, say) already shows
    // the project, so the catch-up must not rebuild it again.
    [Fact]
    public void ProjectLoad_RebuildsTheCanvasOnce_EvenWhenItLandsBeforeTheCatchUp()
    {
        OnCanvas(canvas =>
        {
            ComponentSave button = canvas.Project.AddComponent("Button");
            canvas.AddInstance(button, "Box", "Rectangle", x: 20, y: 20, width: 60, height: 40);
            canvas.Tree.Click(canvas.Tree.NodeFor(button));
            Dispatcher.UIThread.RunJobs();
            PluginManager pluginManager = TestAppBuilder.Services.GetRequiredService<PluginManager>();
            // Only the editor's own project-load rebuilds: a load can also regenerate fonts, which
            // rebuilds the wireframe content separately.
            int rebuilds = 0;
            void CountRebuild()
            {
                if (Environment.StackTrace.Contains("EditorTabPluginBase.HandleProjectLoad", StringComparison.Ordinal))
                {
                    rebuilds++;
                }
            }
            canvas.Plugin.WireframeRefreshed += CountRebuild;
            try
            {
                pluginManager.ProjectLoad(canvas.Project.Project);
                Dispatcher.UIThread.RunJobs();
                rebuilds.ShouldBe(1, "a project load after startup");

                rebuilds = 0;
                canvas.Plugin.RequestCatchUpWithProjectAndSelection();
                pluginManager.ProjectLoad(canvas.Project.Project);
                Dispatcher.UIThread.RunJobs();
                rebuilds.ShouldBe(1, "a project load between the surface and its catch-up");
            }
            finally
            {
                canvas.Plugin.WireframeRefreshed -= CountRebuild;
            }
            canvas.Frame();
        });
    }

    private static void OnCanvas(Action<CanvasHarness> scenario)
    {
        Assert.SkipUnless(CanvasHarness.CanRun, CanvasHarness.SkipReason);
        CanvasHarness.OnUiThread(() =>
        {
            using CanvasHarness canvas = new CanvasHarness();
            scenario(canvas);
        });
    }
}
