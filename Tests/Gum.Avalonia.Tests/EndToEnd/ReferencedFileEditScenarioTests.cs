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
/// Real service graph (real FileChangeReactionLogic, WireframeObjectManager, LoaderManager) for a picture, svg
/// or lottie file an element references, edited or deleted on disk (#7027). No GL device on macOS, so a
/// sentinel stands in for the cached texture or document.
/// </summary>
[Trait("Category", "EndToEnd")]
public class ReferencedFileEditScenarioTests
{
    private sealed class Sentinel : IDisposable
    {
        public bool Disposed;
        public void Dispose() => Disposed = true;
    }

    // "Card" uses the file; "Other" is empty and is what is shown, so no texture has to load.
    private static (ProjectTreeHarness tree, ComponentSave other, string file) Arrange(string baseType, string variable, string relativeFile)
    {
        ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave card = tree.Project.AddComponent("Card");
        ComponentSave other = tree.Project.AddComponent("Other");
        tree.Project.AddInstance(card, "Item", baseType);
        string file = Path.Combine(tree.Project.ProjectFolder, relativeFile);
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, "first");
        card.GetDefaultStateOrThrow().SetValue("Item." + variable, relativeFile, "string");
        tree.SaveAll();
        return (tree, other, file);
    }

    private static Sentinel Seed(string key)
    {
        Sentinel sentinel = new Sentinel();
        LoaderManager.Self.DisposeAndClear();
        LoaderManager.Self.AddDisposable(key, sentinel);
        return sentinel;
    }

    [AvaloniaTheory]
    [InlineData("Sprite", "SourceFile", "Textures/Photo.png")]
    [InlineData("NineSlice", "SourceFile", "Textures/Frame.png")]
    [InlineData("Sprite", "AnimationChains", "Animations/Idle.achx")]
    [InlineData("Svg", "SourceFile", "Art/Icon.svg")]
    [InlineData("LottieAnimation", "SourceFile", "Art/Spinner.json")]
    [InlineData("Text", "CustomFontFile", "Fonts/Custom.fnt")]
    public void EditingAFileAnotherElementUses_DropsTheCachedCopy_SoSelectingThatElementLoadsFresh(string baseType, string variable, string relativeFile)
    {
        (ProjectTreeHarness tree, ComponentSave other, string file) = Arrange(baseType, variable, relativeFile);
        using (tree)
        {
            tree.Click(tree.NodeFor(other));
            Sentinel cached = Seed(file);
            File.AppendAllText(file, "second");

            TestAppBuilder.Services.GetRequiredService<FileChangeReactionLogic>().ReactToFileChanged(new FilePath(file));
            tree.ThrowIfCrashed();

            cached.Disposed.ShouldBeTrue("the next element to show it would draw the stale cached copy");
            LoaderManager.Self.CachedDisposables.ShouldNotContainKey(file);
        }
    }

    [AvaloniaFact]
    public void EditingAReferencedPicture_WithNothingSelected_DropsTheCachedCopy()
    {
        (ProjectTreeHarness tree, ComponentSave other, string file) = Arrange("Sprite", "SourceFile", "Textures/Photo.png");
        using (tree)
        {
            tree.Click(tree.NodeFor(other));
            tree.SelectedState.SelectedElement = null;
            Sentinel cached = Seed(file);
            File.AppendAllText(file, "second");

            TestAppBuilder.Services.GetRequiredService<FileChangeReactionLogic>().ReactToFileChanged(new FilePath(file));
            tree.ThrowIfCrashed();

            cached.Disposed.ShouldBeTrue();
        }
    }

    [AvaloniaFact]
    public void DeletingAReferencedPicture_DoesNotCrash_AndDropsTheCachedCopy()
    {
        (ProjectTreeHarness tree, ComponentSave other, string file) = Arrange("Sprite", "SourceFile", "Textures/Photo.png");
        using (tree)
        {
            tree.Click(tree.NodeFor(other));
            Sentinel cached = Seed(file);
            File.Delete(file);

            TestAppBuilder.Services.GetRequiredService<FileChangeReactionLogic>().ReactToFileDeleted(new FilePath(file));
            tree.ThrowIfCrashed();

            cached.Disposed.ShouldBeTrue();
        }
    }

    [AvaloniaFact]
    public void EditingAFileNoElementReferences_LeavesTheCacheAlone()
    {
        (ProjectTreeHarness tree, ComponentSave other, string file) = Arrange("Sprite", "SourceFile", "Textures/Photo.png");
        using (tree)
        {
            tree.Click(tree.NodeFor(other));
            string unrelated = Path.Combine(tree.Project.ProjectFolder, "notes.txt");
            File.WriteAllText(unrelated, "x");
            Sentinel cached = Seed(file);

            TestAppBuilder.Services.GetRequiredService<FileChangeReactionLogic>().ReactToFileChanged(new FilePath(unrelated));

            cached.Disposed.ShouldBeFalse();
        }
    }
}
