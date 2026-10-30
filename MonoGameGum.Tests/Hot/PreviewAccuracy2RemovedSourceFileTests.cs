#pragma warning disable CS0618
using Gum.DataTypes;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using RenderingLibrary.Graphics.Animation;
using Shouldly;
using System.Linq;
using Xunit;
using static MonoGameGum.Tests.Hot.PreviewAccuracy2World;

namespace MonoGameGum.Tests.Hot;

// A SourceFile that goes from an animation chain file to nothing (variable removed or set empty) or to an image:
// the chains the file produced go with it, as a restart shows.
public partial class PreviewAccuracy2Tests
{
    private static void RemoveVariable(GumProjectSave project, string name) =>
        MainScreen(project).DefaultState!.Variables.RemoveAll(variable => variable.Name == name);

    [Fact]
    public void RemovedSourceFile_SpriteThatPlayedAChainFile_ShowsNoChainsAndNoTexture()
    {
        PreviewAccuracy2World world = BootWithChain();
        PreviewVerdict2 verdict = world.Save(project => RemoveVariable(project, "Pic.SourceFile"));

        ShouldBeCorrect(verdict);
        verdict.Hot.ShouldNotContain("r.chains=");
    }

    [Fact]
    public void EmptySourceFile_SpriteThatPlayedAChainFile_ShowsNoChainsAndNoTexture()
    {
        PreviewAccuracy2World world = BootWithChain();
        PreviewVerdict2 verdict = world.Save(project => Std(MainScreen(project), "Pic", "SourceFile", ""));

        ShouldBeCorrect(verdict);
        verdict.Hot.ShouldNotContain("r.chains=");
    }

    [Fact]
    public void RemovedSourceFile_NineSliceThatPlayedAChainFile_ShowsNoChainsAndNoTexture()
    {
        PreviewAccuracy2World world = BootWithChain("Frame");
        world.Find("Frame")!.RenderableComponent.ShouldBeOfType<NineSlice>().AnimationChains.ShouldNotBeNull();

        PreviewVerdict2 verdict = world.Save(project => RemoveVariable(project, "Frame.SourceFile"));

        ShouldBeCorrect(verdict);
        world.Find("Frame")!.RenderableComponent.ShouldBeOfType<NineSlice>().AnimationChains.ShouldBeNull();
    }

    [Fact]
    public void ChangedSourceFile_FromAChainFileToAnImage_ShowsNoChains()
    {
        PreviewAccuracy2World world = BootWithChain();
        PreviewVerdict2 verdict = world.Save(project => Std(MainScreen(project), "Pic", "SourceFile", "Content/other.png"));

        ShouldBeCorrect(verdict);
        verdict.Hot.ShouldNotContain("r.chains=");
        verdict.Hot.ShouldContain("r.tex=40x20");
    }

    [Fact]
    public void RemovedSourceFile_AfterAnImage_ShowsNoTexture()
    {
        PreviewAccuracy2World world = Boot();
        PreviewVerdict2 verdict = world.Save(project => RemoveVariable(project, "Pic.SourceFile"));

        ShouldBeCorrect(verdict);
        verdict.Hot.ShouldContain("r.tex=<null>");
    }

    [Fact]
    public void RemovedSourceFile_ChainsAssignedByCodeAfterTheFile_AreLeftAlone()
    {
        PreviewAccuracy2World world = BootWithChain();
        Sprite sprite = world.Find("Pic")!.RenderableComponent.ShouldBeOfType<Sprite>();
        AnimationChainList fromCode = new AnimationChainList();
        fromCode.Add(new AnimationChain { Name = "Code" });
        sprite.AnimationChains = fromCode;

        world.Save(project => RemoveVariable(project, "Pic.SourceFile"));

        world.Find("Pic")!.RenderableComponent.ShouldBeOfType<Sprite>().AnimationChains.ShouldBeSameAs(fromCode);
    }

    [Fact]
    public void RemovedSourceFile_ChainsAssignedByCodeToASpriteThatNeverHadAFile_AreLeftAlone()
    {
        PreviewAccuracy2World world = Boot();
        Sprite sprite = world.Find("Pic")!.RenderableComponent.ShouldBeOfType<Sprite>();
        AnimationChainList fromCode = new AnimationChainList();
        fromCode.Add(new AnimationChain { Name = "Code" });
        sprite.AnimationChains = fromCode;

        world.Save(project => RemoveVariable(project, "Pic.SourceFile"));

        world.Find("Pic")!.RenderableComponent.ShouldBeOfType<Sprite>().AnimationChains.ShouldBeSameAs(fromCode);
    }
}
