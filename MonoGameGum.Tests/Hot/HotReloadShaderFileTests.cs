using Gum;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Content;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace MonoGameGum.Tests.Hot;

/// <summary>
/// A container's SourceShaderFile is re-assigned when the file it points at changes (#7034). The
/// rule is "a file an element references changed", not a list of extensions, so any extension works
/// and no project reload or tree rebuild happens.
/// </summary>
public class HotReloadShaderFileTests : BaseTestClass
{
    private sealed class FakeEffect : IDisposable
    {
        public bool IsDisposed;
        public void Dispose() => IsDisposed = true;
    }

    private sealed class Harness : IDisposable
    {
        public string Directory = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), "HotReloadShaderFileTests_" + System.IO.Path.GetRandomFileName());
        public DateTime Now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public int ProjectReloadCount;
        public List<string> Sequence = new List<string>();
        public int ResolveCount;
        public List<FakeEffect> Effects = new List<FakeEffect>();
        public GumHotReloadManager Manager;
        public string GumxPath = "";
        private readonly bool _previousCacheTextures = LoaderManager.Self.CacheTextures;

        public Harness()
        {
            System.IO.Directory.CreateDirectory(Directory);
            string gumxPath = System.IO.Path.Combine(Directory, "Proj.gumx");
            new Gum.DataTypes.GumProjectSave().Save(gumxPath, saveElements: false);

            LoaderManager.Self.CacheTextures = true;
            Gum.Wireframe.CustomSetPropertyOnRenderable.RenderTargetEffectResolver = path =>
            {
                ResolveCount++;
                Sequence.Add("resolve");
                FakeEffect effect = new FakeEffect();
                Effects.Add(effect);
                return effect;
            };

            Manager = new GumHotReloadManager(
                _ => { ProjectReloadCount++; Sequence.Add("reload"); },
                GumAnimationLoader.LoadAnimationsFromProvider,
                path => LoaderManager.Self.Dispose(path),
                () => Now);
            GumxPath = gumxPath;
            Manager.Start(gumxPath);
            Manager.Stop();
        }

        public string WriteFile(string name)
        {
            string path = System.IO.Path.Combine(Directory, name);
            File.WriteAllText(path, "shader");
            return path;
        }

        public void ChangeAndUpdate(string path, params GraphicalUiElement[] roots)
        {
            Manager.NotifyFileChanged(path);
            Now = Now.AddMilliseconds(300);
            Manager.Update(roots);
        }

        public void Dispose()
        {
            Gum.Wireframe.CustomSetPropertyOnRenderable.RenderTargetEffectResolver = null;
            LoaderManager.Self.DisposeAndClear();
            LoaderManager.Self.CacheTextures = _previousCacheTextures;
            try { System.IO.Directory.Delete(Directory, recursive: true); } catch { /* best-effort */ }
        }
    }

    [Theory]
    [InlineData("effect.fx")]
    [InlineData("effect.slang")]
    [InlineData("effect.customshader")]
    public void Update_ReassignsSourceShaderFile_WhenTheReferencedFileChanges(string fileName)
    {
        using Harness harness = new Harness();
        string shaderPath = harness.WriteFile(fileName);

        ContainerRuntime container = new ContainerRuntime();
        container.IsRenderTarget = true;
        container.SourceShaderFile = shaderPath;
        harness.ResolveCount.ShouldBe(1);

        harness.ChangeAndUpdate(shaderPath, container);

        harness.ResolveCount.ShouldBe(2);
    }

    [Fact]
    public void Update_GivesTheContainerTheFreshlyResolvedEffect_AndDisposesTheStaleOne()
    {
        using Harness harness = new Harness();
        string shaderPath = harness.WriteFile("effect.fx");

        ContainerRuntime child = new ContainerRuntime();
        child.IsRenderTarget = true;
        child.SourceShaderFile = shaderPath;
        ContainerRuntime root = new ContainerRuntime();
        root.AddChild(child);

        harness.ChangeAndUpdate(shaderPath, root);

        FakeEffect assigned = (FakeEffect)((RenderingLibrary.Graphics.InvisibleRenderable)child.RenderableComponent)
            .RenderTargetEffect!;
        assigned.ShouldBeSameAs(harness.Effects[1]);
        harness.Effects[0].IsDisposed.ShouldBeTrue();
        harness.ProjectReloadCount.ShouldBe(0, "no project reload is needed for a shader edit");
    }

    [Fact]
    public void Update_LeavesContainersAlone_WhenAnUnreferencedFileChanges()
    {
        using Harness harness = new Harness();
        string shaderPath = harness.WriteFile("effect.fx");
        string otherPath = harness.WriteFile("other.fx");

        ContainerRuntime container = new ContainerRuntime();
        container.IsRenderTarget = true;
        container.SourceShaderFile = shaderPath;

        harness.ChangeAndUpdate(otherPath, container);

        harness.ResolveCount.ShouldBe(1);
        harness.Effects[0].IsDisposed.ShouldBeFalse();
        harness.ProjectReloadCount.ShouldBe(0);
    }

    [Fact]
    public void Update_ReassignsOnlyContainersUsingTheChangedFile()
    {
        using Harness harness = new Harness();
        string firstPath = harness.WriteFile("first.fx");
        string secondPath = harness.WriteFile("second.fx");

        ContainerRuntime first = new ContainerRuntime();
        first.IsRenderTarget = true;
        first.SourceShaderFile = firstPath;
        ContainerRuntime second = new ContainerRuntime();
        second.IsRenderTarget = true;
        second.SourceShaderFile = secondPath;

        harness.ChangeAndUpdate(firstPath, first, second);

        harness.ResolveCount.ShouldBe(3);
        harness.Effects[1].IsDisposed.ShouldBeFalse();
    }

    [Fact]
    public void Update_StillReloadsAfterTheDebounce_WhenAnUnrelatedFileChangesInTheMeantime()
    {
        using Harness harness = new Harness();

        harness.Manager.NotifyFileChanged(harness.GumxPath);
        harness.Now = harness.Now.AddMilliseconds(150);
        harness.Manager.NotifyFileChanged(Path.Combine(harness.Directory, "editor-swap.tmp"));
        harness.Now = harness.Now.AddMilliseconds(60);
        harness.Manager.Update(Array.Empty<GraphicalUiElement>());

        harness.ProjectReloadCount.ShouldBe(1, "the project change is 210 ms old; the temp file must not restart its debounce");
    }

    [Fact]
    public void Update_ReassignsShadersThenReloads_WhenAProjectFileAndAShaderChangeInOneBurst()
    {
        using Harness harness = new Harness();
        string shaderPath = harness.WriteFile("effect.fx");

        ContainerRuntime container = new ContainerRuntime();
        container.IsRenderTarget = true;
        container.SourceShaderFile = shaderPath;
        harness.Sequence.Clear();

        harness.Manager.NotifyFileChanged(Path.Combine(harness.Directory, "Screen.gusx"));
        harness.Manager.NotifyFileChanged(shaderPath);
        harness.Now = harness.Now.AddMilliseconds(300);
        harness.Manager.Update(new GraphicalUiElement[] { container });

        harness.Sequence.ShouldBe(new[] { "resolve", "reload" });
    }
}
