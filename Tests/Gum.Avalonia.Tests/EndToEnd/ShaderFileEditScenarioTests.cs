using Avalonia.Headless.XUnit;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Microsoft.Extensions.DependencyInjection;
using RenderingLibrary.Content;
using Shouldly;
using ToolsUtilities;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// Real service graph (real FileChangeReactionLogic, WireframeObjectManager, LoaderManager) for a
/// shader file edited on disk (#7017). No GL device on macOS, so a sentinel stands in for the cached Effect.
/// </summary>
[Trait("Category", "EndToEnd")]
public class ShaderFileEditScenarioTests
{
    private sealed class Sentinel : IDisposable
    {
        public bool Disposed;
        public void Dispose() => Disposed = true;
    }

    private static (ProjectTreeHarness tree, ComponentSave card, string shader) Arrange()
    {
        ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave card = tree.Project.AddComponent("Card");
        tree.Project.AddInstance(card, "Box", "Container");
        string shader = Path.Combine(tree.Project.ProjectFolder, "Shaders", "Blur.slang");
        Directory.CreateDirectory(Path.GetDirectoryName(shader)!);
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Content", "Shaders", "Blur.slang"), shader);
        card.GetDefaultStateOrThrow().SetValue("Box.IsRenderTarget", true, "bool");
        card.GetDefaultStateOrThrow().SetValue("Box.SourceShaderFile", "Shaders/Blur.slang", "string");
        tree.SaveAll();
        return (tree, card, shader);
    }

    private static Sentinel Seed(string key)
    {
        Sentinel sentinel = new Sentinel();
        LoaderManager.Self.DisposeAndClear();
        LoaderManager.Self.AddDisposable(key, sentinel);
        return sentinel;
    }

    [AvaloniaFact]
    public void EditingTheShader_WithItsElementSelected_DropsTheCachedEffect()
    {
        (ProjectTreeHarness tree, ComponentSave card, string shader) = Arrange();
        using (tree)
        {
            tree.Click(tree.NodeFor(card));
            Sentinel cached = Seed(shader);
            File.AppendAllText(shader, "\n// edit\n");

            TestAppBuilder.Services.GetRequiredService<FileChangeReactionLogic>().ReactToFileChanged(new FilePath(shader));
            tree.ThrowIfCrashed();

            cached.Disposed.ShouldBeTrue();
            LoaderManager.Self.CachedDisposables.ShouldNotContainKey(shader);
        }
    }

    [AvaloniaFact]
    public void EditingTheShader_WithNothingSelected_DropsTheCachedEffect()
    {
        (ProjectTreeHarness tree, ComponentSave card, string shader) = Arrange();
        using (tree)
        {
            tree.Click(tree.NodeFor(card));
            tree.SelectedState.SelectedElement = null;
            Sentinel cached = Seed(shader);
            File.AppendAllText(shader, "\n// edit\n");

            TestAppBuilder.Services.GetRequiredService<FileChangeReactionLogic>().ReactToFileChanged(new FilePath(shader));
            tree.ThrowIfCrashed();

            cached.Disposed.ShouldBeTrue("selecting the element afterwards would draw the stale cached effect");
        }
    }

    [AvaloniaFact]
    public void DeletingTheShader_DoesNotCrash_AndDropsTheCachedEffect()
    {
        (ProjectTreeHarness tree, ComponentSave card, string shader) = Arrange();
        using (tree)
        {
            tree.Click(tree.NodeFor(card));
            Sentinel cached = Seed(shader);
            File.Delete(shader);

            TestAppBuilder.Services.GetRequiredService<FileChangeReactionLogic>().ReactToFileDeleted(new FilePath(shader));
            tree.ThrowIfCrashed();

            cached.Disposed.ShouldBeTrue();
        }
    }

    [AvaloniaFact]
    public void EditingAShaderNoElementUses_LeavesTheCacheAlone()
    {
        (ProjectTreeHarness tree, ComponentSave card, string shader) = Arrange();
        using (tree)
        {
            tree.Click(tree.NodeFor(card));
            string other = Path.Combine(tree.Project.ProjectFolder, "Shaders", "Unused.slang");
            File.WriteAllText(other, "float X = 1.0;");
            Sentinel cached = Seed(other);

            TestAppBuilder.Services.GetRequiredService<FileChangeReactionLogic>().ReactToFileChanged(new FilePath(other));

            cached.Disposed.ShouldBeFalse();
        }
    }
}
