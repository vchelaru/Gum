using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using Microsoft.Xna.Framework;
using RenderingLibrary;
using RenderingLibrary.Content;
using Shouldly;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// #5709: #5707 left every descendant of an inflated component suspended (blank UI) while all unit
/// tests stayed green. This inflates every Screen and Component of the GameUiSamples project and
/// asserts the invariant that would have caught it: once inflation returns and
/// <see cref="GraphicalUiElement.IsAllLayoutSuspended"/> is false, no node is still suspended.
/// </summary>
public class SampleInflationLayoutSuspensionTests : BaseTestClass
{
    [Fact]
    public void InflatingEveryScreenAndComponent_ShouldLeaveNoNodeSuspended()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        SystemManagers managers = SystemManagers.Default;
        var project = ObjectFinder.Self.GumProjectSave;
        project.ShouldNotBeNull();

        List<ElementSave> elements = new();
        elements.AddRange(project!.Screens);
        elements.AddRange(project.Components);
        elements.Count.ShouldBeGreaterThan(10);

        RegisterComponentFactoriesLikeGeneratedCode(project, managers);

        List<string> offenders = new();
        foreach (ElementSave element in elements)
        {
            GraphicalUiElement root;
            try
            {
                root = element.ToGraphicalUiElement(managers, addToManagers: true);
            }
            catch (Exception e)
            {
                offenders.Add($"{element.Name}: threw {e.GetType().Name}: {e.Message}");
                continue;
            }

            GraphicalUiElement.IsAllLayoutSuspended.ShouldBeFalse(element.Name);
            CollectSuspended(root, "", offenders);
            root.RemoveFromManagers();
        }

        offenders.Count.ShouldBe(0, offenders.Count + " suspended nodes, first: " + string.Join(", ", offenders.Take(5)));
    }

    // The sample's generated code registers a factory per Container-based component that inflates it
    // through SetGraphicalUiElement (nested inside the parent screen's inflation, so under
    // IsAllLayoutSuspended) and then constructs the Forms wrapper, whose constructor often applies a
    // state (ManaOrb.CustomInitialize does InterpolateBetween). The sample project is not referenced
    // here, so mirror that: after inflating, apply the first two states of the first multi-state
    // category, interpolated and directly.
    private static void RegisterComponentFactoriesLikeGeneratedCode(GumProjectSave project, SystemManagers managers)
    {
        foreach (ComponentSave component in project.Components.Where(item => item.BaseType == "Container"))
        {
            ComponentSave captured = component;
            ElementSaveExtensions.RegisterGueInstantiation(captured.Name, () =>
            {
                var visual = new Gum.GueDeriving.ContainerRuntime();
                captured.SetGraphicalUiElement(visual, managers);

                var category = captured.Categories.FirstOrDefault(item => item.States.Count >= 2);
                if (category != null)
                {
                    visual.InterpolateBetween(category.States[0], category.States[1], 0.5f);
                    visual.ApplyState(category.States[1]);
                }
                return visual;
            });
        }
    }

    private static void CollectSuspended(GraphicalUiElement node, string path, List<string> offenders)
    {
        string here = path + "/" + node.Name; // root name is the element name
        if (node.IsLayoutSuspended)
        {
            offenders.Add(here);
        }

        if (node.Children != null)
        {
            foreach (GraphicalUiElement child in node.Children.ToArray())
            {
                CollectSuspended(child, here, offenders);
            }
        }
    }

    private class MinimalGame : Game
    {
        private readonly GraphicsDeviceManager _graphics;
        public Gum.GumService GumService { get; }

        public MinimalGame()
        {
            LoaderManager.Self?.DisposeAndClear();
            _graphics = new GraphicsDeviceManager(this);
            GumService = new Gum.GumService();
        }

        protected override void Initialize()
        {
            base.Initialize();
            GumService.Initialize(this, FindProjectFile());
        }

        private static string FindProjectFile()
        {
            string current = AppContext.BaseDirectory;
            for (int i = 0; i < 10; i++)
            {
                string candidate = Path.Combine(
                    current, "Samples", "GameUiSamples", "Content", "GumProject", "GameUiSamplesGumProject.gumx");
                if (File.Exists(candidate))
                {
                    return candidate;
                }
                string? parent = Path.GetDirectoryName(current);
                if (string.IsNullOrEmpty(parent) || parent == current)
                {
                    break;
                }
                current = parent;
            }
            throw new InvalidOperationException("could not locate GameUiSamples project from " + AppContext.BaseDirectory);
        }

        protected override void Update(GameTime gameTime) { }
        protected override void Draw(GameTime gameTime) => GraphicsDevice.Clear(Color.CornflowerBlue);

        protected override void Dispose(bool disposing)
        {
            if (GumService.IsInitialized)
            {
                GumService.Uninitialize();
            }
            LoaderManager.Self?.DisposeAndClear();
            base.Dispose(disposing);
        }
    }
}
