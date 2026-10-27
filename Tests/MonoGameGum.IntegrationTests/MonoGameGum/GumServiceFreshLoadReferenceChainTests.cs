using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using Shouldly;
using System;
using System.IO;

namespace MonoGameGum.IntegrationTests.MonoGameGum;

/// <summary>
/// #6881: a game started on files edited by hand (only the last swatch of a chain changed, so the files that
/// read it still hold the value they were saved with) shows the end of the chain, like a hot reload does.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class GumServiceFreshLoadReferenceChainTests : BaseTestClass, IDisposable
{
    private const int Old = 10;
    private const int Edited = 110;

    private string? _tempDirectory;

    public override void Dispose()
    {
        base.Dispose();
        if (_tempDirectory != null && Directory.Exists(_tempDirectory))
        {
            try
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
            catch
            {
                // Best effort - file handles may still be held during test teardown.
            }
        }
    }

    [Fact]
    public void Initialize_OnlyTheLastSwatchOfAChainEdited_ReaderShowsTheEndOfTheChain()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumFreshLoad6881_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
        string gumxPath = Path.Combine(_tempDirectory, "Project.gumx");
        WriteEditedChainProject(gumxPath);

        using GameForFreshLoad game = new GameForFreshLoad(gumxPath);
        game.RunOneFrame();

        ScreenSave screen = game.Project!.Screens[0];
        GraphicalUiElement visual = screen.ToGraphicalUiElement(game.GumService.SystemManagers!, addToManagers: false);
        ((ColoredRectangleRuntime)visual.GetGraphicalUiElementByName("Reader")!).Red.ShouldBe(Edited);
    }

    private static void SetVariable(StateSave state, string name, string type, object value)
    {
        state.Variables.Add(new VariableSave { Name = name, Type = type, Value = value, SetsValue = true });
    }

    private static void AddSwatch(ElementSave owner, string instance, int red, string? row)
    {
        owner.Instances.Add(new InstanceSave { Name = instance, BaseType = "ColoredRectangle", ParentContainer = owner });
        SetVariable(owner.DefaultState!, instance + ".Red", "int", red);
        SetVariable(owner.DefaultState!, instance + ".Green", "int", 0);
        SetVariable(owner.DefaultState!, instance + ".Blue", "int", 0);
        if (row != null)
        {
            VariableListSave<string> list = new VariableListSave<string> { Type = "string", Name = instance + ".VariableReferences" };
            list.Value.Add(row);
            owner.DefaultState!.VariableLists.Add(list);
        }
    }

    private static ComponentSave NewComponent(string name)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        return component;
    }

    /// <summary>Chain1 reads Chain2 reads Chain3, and the reader reads Chain1; only Chain3 holds the edit.</summary>
    private static void WriteEditedChainProject(string gumxPath)
    {
        StandardElementsManager.Self.Initialize();
        GumProjectSave project = new GumProjectSave();
        foreach (string standard in new[] { "Container", "ColoredRectangle" })
        {
            StandardElementSave standardElement = new StandardElementSave { Name = standard };
            standardElement.States.Add(StandardElementsManager.Self.GetDefaultStateFor(standard)!.Clone());
            standardElement.DefaultState!.ParentContainer = standardElement;
            project.StandardElements.Add(standardElement);
            project.StandardElementReferences.Add(new ElementReference { Name = standard, ElementType = ElementType.Standard });
        }

        for (int i = 1; i <= 3; i++)
        {
            ComponentSave chain = NewComponent("Chain" + i);
            AddSwatch(chain, "Swatch", i == 3 ? Edited : Old, i == 3 ? null : "Color = Components/Chain" + (i + 1) + ".Swatch.Color");
            project.Components.Add(chain);
            project.ComponentReferences.Add(new ElementReference { Name = chain.Name, ElementType = ElementType.Component });
        }

        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        AddSwatch(screen, "Reader", Old, "Color = Components/Chain1.Swatch.Color");
        project.Screens.Add(screen);
        project.ScreenReferences.Add(new ElementReference { Name = screen.Name, ElementType = ElementType.Screen });

        project.Save(gumxPath, saveElements: true);
    }

    private sealed class GameForFreshLoad : SharedGameLease
    {
        private readonly string _gumProjectFile;
        public Gum.GumService GumService { get; } = new Gum.GumService();
        public GumProjectSave? Project { get; private set; }

        public GameForFreshLoad(string gumProjectFile)
        {
            _gumProjectFile = gumProjectFile;
        }

        protected override void OnInitialize() => Project = GumService.Initialize(Game, _gumProjectFile);

        protected override void OnDispose()
        {
            if (GumService.IsInitialized)
            {
                GumService.Uninitialize();
            }
        }
    }
}
