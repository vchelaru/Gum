#pragma warning disable CS0618
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Gum.Graphics.Animation;
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

    [Fact]
    public void ChangedSourceFile_FromAChainFileToEmpty_KeepsTheChainNameAskedFor()
    {
        PreviewAccuracy2World world = BootWithChain();
        GraphicalUiElement pic = world.Find("Pic")!;
        Sprite sprite = pic.RenderableComponent.ShouldBeOfType<Sprite>();

        pic.SetProperty("SourceFile", "");
        sprite.CurrentChainName.ShouldBeNull();
        pic.SetProperty("SourceFile", "Content/run.achx");

        sprite.CurrentChainName.ShouldBe("Run");
    }

    [Fact]
    public void ChangedSourceFile_FromAChainFileToEmpty_KeepsTextureValuesASelectedStateSets()
    {
        PreviewAccuracy2World world = new PreviewAccuracy2World();
        _worlds.Add(world);
        world.Boot(
            editBaseline: project =>
            {
                Std(MainScreen(project), "Pic", "SourceFile", "Content/run.achx");
                Std(MainScreen(project), "Pic", "CurrentChainName", "Run");
                StateSave big = new StateSave { Name = "Big", ParentContainer = MainScreen(project) };
                Set(big, "Pic.TextureAddress", "TextureAddress", Gum.Managers.TextureAddress.Custom);
                Set(big, "Pic.TextureWidth", "int", 5);
                Set(big, "Pic.TextureHeight", "int", 6);
                StateSaveCategory look = new StateSaveCategory { Name = "Look" };
                look.States.Add(big);
                MainScreen(project).Categories.Add(look);
            },
            beforeBoot: dir => WriteChain(dir, "run.achx", "red.png", 0.1f));
        world.Screen.ApplyState("Look", "Big");
        GraphicalUiElement pic = world.Find("Pic")!;

        pic.SetProperty("SourceFile", "");

        pic.TextureAddress.ShouldBe(Gum.Managers.TextureAddress.Custom);
        pic.TextureWidth.ShouldBe(5);
        pic.TextureHeight.ShouldBe(6);
    }
}
