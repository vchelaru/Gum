using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Gum.Avalonia.Canvas;
using Gum.Avalonia.Shell;
using Gum.Services;
using Gum.Avalonia.Tests.EndToEnd;
using Gum.Avalonia.Tests.Harness;
using Gum.Avalonia.Tests.VariableGrid;
using Gum.DataTypes;
using Gum.Wireframe;
using GumRuntime;
using Microsoft.Extensions.DependencyInjection;
using RenderingLibrary.Graphics;
using AvaloniaDataUi.Controls;
using Gum.Avalonia.Plugins.VariableGrid;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The Variables tab's shader parameter rows (a float, then a float2, float3 and float4 each in one row) in the real main
/// window, both themes, at every zoom level
/// (<c>GUM_SCREENSHOT_DIR=&lt;folder&gt; dotnet test --filter ShaderVectorRowScreenshotTests</c>).
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class ShaderVectorRowScreenshotTests : IDisposable
{
    public void Dispose() => ShaderParameterReader.Reader = null;

    [SkippableFact]
    public void VectorRows_NothingSaved() => PrScreenshot.Run(() => Capture("shader-vectors-default", edit: false));

    [SkippableFact]
    public void VectorRows_MenuOnAnUnexposedRow() => PrScreenshot.Run(() => Capture("shader-vectors-menu", edit: true, menu: true));

    [SkippableFact]
    public void VectorRows_ExposedWithItsMenu() => PrScreenshot.Run(() => Capture("shader-vectors-exposed", edit: true, expose: true, menu: true));

    [SkippableFact]
    public void VectorRows_Exposed() => PrScreenshot.Run(() => Capture("shader-vectors-exposed-row", edit: true, expose: true));

    [SkippableFact]
    public void VectorRows_SomeEdited() => PrScreenshot.Run(() => Capture("shader-vectors-edited", edit: true));

    private static void Capture(string name, bool edit, bool expose = false, bool menu = false)
    {
        ShaderParameterReader.Reader = _ => new List<ShaderParameterInfo>
        {
            new ShaderParameterInfo("Thickness", ShaderParameterKind.Float, 1, ShaderParameterValue.FromFloat(2f)),
            new ShaderParameterInfo("Offset", ShaderParameterKind.Float, 2, ShaderParameterValue.FromVector2(0f, 0f)),
            new ShaderParameterInfo("Size", ShaderParameterKind.Float, 3, ShaderParameterValue.FromVector3(1f, 1f, 1f)),
            new ShaderParameterInfo("Tint", ShaderParameterKind.Float, 4, ShaderParameterValue.FromVector4(1f, 1f, 1f, 1f)),
        };

        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave card = grid.Project.AddComponent("Card");
        InstanceSave box = grid.Project.AddInstance(card, "Icon", "Container");
        card.GetDefaultStateOrThrow().SetValue("Icon.SourceShaderFile", "Shaders/Outline.slang", "string");
        GraphicalUiElement representation = new GraphicalUiElement(new InvisibleRenderable { RenderTargetEffect = new object() }) { Tag = box };
        ((WireframeObjectManager)TestAppBuilder.Services.GetRequiredService<IWireframeObjectManager>()).AllIpsos.Add(representation);
        grid.Select(box);
        grid.Settle();
        grid.Row("IsRenderTarget").GetVisualDescendants().OfType<CheckBox>().First().IsChecked = true;
        grid.Settle();
        if (edit)
        {
            grid.Input.TypeAndEnter(grid.Editor<ShaderVectorDisplay>("Tint").Cells[1].TextBox, "0.5");
            grid.Input.TypeAndEnter(grid.Editor<ShaderVectorDisplay>("Offset").Cells[0].TextBox, "12");
            grid.Settle();
        }

        if (expose)
        {
            grid.Project.Dialogs.AnswerNext<Gum.Dialogs.ExposeColorDialogViewModel>(_ => true);
            grid.PickRowMenuItem("Tint", "Expose Variable");
            ((WireframeObjectManager)TestAppBuilder.Services.GetRequiredService<IWireframeObjectManager>()).AllIpsos.Add(
                new GraphicalUiElement(new InvisibleRenderable { RenderTargetEffect = new object() }) { Tag = box });
            grid.GridManager.RefreshEntireGrid(force: true);
            grid.Settle();
        }

        foreach (WpfDataUi.DataTypes.MemberCategory category in grid.Grid.Categories)
        {
            category.IsExpanded = category.Name == "Rendering";
        }
        grid.Settle();

        // The real main window hosts the Variables tab; the harness window gives it up.
        grid.Input.Window.Content = null;
        Dispatcher.UIThread.RunJobs();
        (grid.View.Parent as Panel)?.Children.Remove(grid.View);
        Dispatcher.UIThread.RunJobs();
        MainWindow window = TestAppBuilder.Services.GetRequiredService<MainWindow>();
        window.MinHeight = 0;
        window.Width = 1000;
        window.Height = 900;
        window.Show();
        foreach (AvaloniaGraphicsDeviceControl canvas in window.GetVisualDescendants().OfType<AvaloniaGraphicsDeviceControl>())
        {
            canvas.IsVisible = false;
        }
        ToolStartup.VariablesTab.IsSelected = true;
        Dispatcher.UIThread.RunJobs();

        // Saved per zoom level by hand (as PrScreenshot.SaveWindowAtZoomLevels does) so the Tint row can be scrolled into view at each.
        IAppScaleProvider scale = TestAppBuilder.Services.GetRequiredService<IAppScaleProvider>();
        double originalSize = scale.BaseFontSize;
        using IDisposable follower = AppWideWindowInput.FollowBaseFontSize(window, scale);
        try
        {
            foreach (ThemeVariant theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
            {
                window.RequestedThemeVariant = theme;
                foreach (double size in PrScreenshot.ZoomLevels)
                {
                    scale.BaseFontSize = size;
                    Dispatcher.UIThread.RunJobs();
                    window.GetVisualDescendants().OfType<ShaderVectorDisplay>().First(row => row.InstanceMember!.Name == "Tint").BringIntoView();
                    Dispatcher.UIThread.RunJobs();
                    if (menu)
                    {
                        TextBoxDisplay field = window.GetVisualDescendants().OfType<ShaderVectorDisplay>().First(row => row.InstanceMember!.Name == "Tint").Cells[1];
                        field.ContextMenu!.Open(field);
                        Dispatcher.UIThread.RunJobs();
                    }
                    PrScreenshot.SaveWindow(window, $"{name}-{theme.Key.ToString()!.ToLowerInvariant()}-zoom-{size}");
                    if (menu)
                    {
                        window.GetVisualDescendants().OfType<ShaderVectorDisplay>().First(row => row.InstanceMember!.Name == "Tint").Cells[1].ContextMenu!.Close();
                    }
                }
            }
        }
        finally
        {
            scale.BaseFontSize = originalSize;
        }
    }
}
