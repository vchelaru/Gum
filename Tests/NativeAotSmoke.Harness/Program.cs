using System;
using System.Linq;
using Gum;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Wireframe;
using GumRuntime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NativeAotSmoke.Harness;
using RenderingLibrary;

// End-to-end check for issue #4105 (RegisterRuntimeTypesThroughReflection scanned every loaded
// assembly via unguarded reflection, crashing GumService.Initialize under Native AOT when it
// touched trimmed framework metadata), issue #4180 (GumProjectSave.Load's JSON path, added for
// the Native AOT XmlSerializer crash in #4170/#4171, had never been proven under a real AOT
// publish - every prior check ran via `dotnet test`, which is always JIT), and issue #4706's
// Forms-under-AOT spike (RegisterFromFileFormRuntimeDefaults maps a project component to its
// DefaultFromFile*Runtime wrapper, which then applies Behavior-declared FormsProperties onto the
// live FrameworkElement via formsControl.GetType().GetProperty(name) - the same reflection shape
// #4115 fixed for PublishTrimmed via GumCommon's embedded ILLink.Descriptors.xml, but never proven
// under a real Native AOT publish). All of these failure modes only a real
// `dotnet publish -p:PublishAot=true` reproduces. This harness drives Gum's real Initialize
// pipeline - real GraphicsDevice, loading a real .gumj project and its referenced standard
// elements - end to end, so a future regression anywhere in that path is caught automatically
// instead of waiting for another user report. The CI job forces software GL (Mesa llvmpipe) since
// the runners have no GPU. Exit code is the assertion.

try
{
    using SmokeGame game = new SmokeGame();
    game.RunOneFrame();

    Console.WriteLine($"[native-aot-smoke] Adapter = {game.GraphicsDevice.Adapter.Description}");

    GumProjectSave? loadedProject = game.LoadedProject;
    if (loadedProject == null)
    {
        throw new InvalidOperationException("GumService.Initialize did not return a loaded project.");
    }
    if (loadedProject.StandardElements.Count != 8)
    {
        throw new InvalidOperationException(
            $"Expected 8 standard elements from the JSON project, found {loadedProject.StandardElements.Count}.");
    }

    // Issue #4318: the component is only ever built through the ElementSave -> registered-type
    // path, so this fails if the trimmer stops preserving TrimmedComponentRuntime's (bool, bool)
    // constructor. Under the bug it threw MissingMethodException here.
    ComponentSave? component = loadedProject.Components.FirstOrDefault(item => item.Name == "TrimmedComponent");
    if (component == null)
    {
        throw new InvalidOperationException("The JSON project did not contain the TrimmedComponent component.");
    }

    GraphicalUiElement componentVisual = component.ToGraphicalUiElement(
        SystemManagers.Default, addToManagers: false);
    if (componentVisual is not TrimmedComponentRuntime)
    {
        throw new InvalidOperationException(
            $"Expected TrimmedComponent to be built as {nameof(TrimmedComponentRuntime)}, " +
            $"got {componentVisual.GetType().Name}.");
    }

    Texture2D texture = new Texture2D(game.GraphicsDevice, 4, 4);
    Color[] data = new Color[16];
    Array.Fill(data, Color.CornflowerBlue);
    texture.SetData(data);

    // Issue #4706: prove RegisterFromFileFormRuntimeDefaults' Button mapping, and
    // BehaviorFormsPropertyApplier's reflective formsControl.GetType().GetProperty(name), survive
    // a real Native AOT publish - not just the mocked-project unit tests that already cover this
    // shape under `dotnet test` (always JIT). No content files needed: this mirrors how a real
    // project's authored component + behavior look once GumProjectSave.Load has deserialized them.
    // The Button instance is nested under a Screen (not built as a bare root component) because
    // ElementSaveExtensions.NotifyFormsControlsOfInitialStateApplied only re-syncs a built
    // element's *children*, not the element passed in itself - see MonoGameGum.Tests'
    // RegisterFromFileFormRuntimeDefaultsTests for the same shape proven under JIT, and issue #4720
    // for the separate (non-AOT-specific) gap building a Forms-behavior component as the root.
    BehaviorSave buttonBehavior = new BehaviorSave { Name = StandardFormsBehaviorNames.ButtonBehaviorName };
    buttonBehavior.FormsProperties.Add(new VariableSave { Type = "string", Name = "ToolTip" });

    ComponentSave buttonComponent = new ComponentSave { Name = "AotButtonComponent" };
    StateSave buttonDefaultState = new StateSave
    {
        Name = "Default",
        ParentContainer = buttonComponent
    };
    buttonDefaultState.Variables.Add(new VariableSave
    {
        Type = "string",
        Name = "ToolTip",
        Value = "Click me",
        SetsValue = true
    });
    buttonComponent.States.Add(buttonDefaultState);
    buttonComponent.Behaviors.Add(new ElementBehaviorReference { BehaviorName = buttonBehavior.Name });

    ScreenSave buttonScreen = new ScreenSave { Name = "AotButtonScreen" };
    InstanceSave buttonInstance = new InstanceSave
    {
        Name = "ButtonInstance",
        BaseType = buttonComponent.Name,
        ParentContainer = buttonScreen
    };
    buttonScreen.Instances.Add(buttonInstance);
    buttonScreen.States.Add(new StateSave { Name = "Default", ParentContainer = buttonScreen });

    loadedProject.Components.Add(buttonComponent);
    loadedProject.Behaviors.Add(buttonBehavior);
    loadedProject.Screens.Add(buttonScreen);
    FormsUtilities.RegisterFromFileFormRuntimeDefaults();

    GraphicalUiElement buttonScreenVisual = buttonScreen.ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);
    InteractiveGue? interactiveButtonVisual = buttonScreenVisual.Children.OfType<InteractiveGue>().FirstOrDefault();
    if (interactiveButtonVisual == null)
    {
        throw new InvalidOperationException(
            "Expected the screen's ButtonInstance child to build an InteractiveGue, but no InteractiveGue " +
            $"child was found among {buttonScreenVisual.Children.Count} child/children.");
    }
    if (interactiveButtonVisual.FormsControlAsObject is not Button button)
    {
        throw new InvalidOperationException(
            "RegisterFromFileFormRuntimeDefaults did not wire a Button onto the component - " +
            $"FormsControlAsObject was {interactiveButtonVisual.FormsControlAsObject?.GetType().Name ?? "null"}.");
    }
    if (button.ToolTip is not "Click me")
    {
        throw new InvalidOperationException(
            "BehaviorFormsPropertyApplier did not apply the authored ToolTip onto the Button under " +
            $"Native AOT - expected \"Click me\", got {(button.ToolTip == null ? "null" : $"\"{button.ToolTip}\"")}. " +
            "The trimmer likely removed Button.ToolTip's property metadata that GetProperty(\"ToolTip\") needs.");
    }

    Console.WriteLine(
        "[native-aot-smoke] PASS: GumService.Initialize loaded a real .gumj project " +
        $"({loadedProject.StandardElements.Count} standard element(s), {loadedProject.Components.Count} component(s)) " +
        "under Native AOT with a real GraphicsDevice, built a registered runtime type reflectively, and " +
        "applied a Behavior-declared FormsProperty onto a live Forms control reflectively.");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[native-aot-smoke] FAIL: {ex.GetType().FullName}: {ex.Message}");
    Console.Error.WriteLine(ex.StackTrace);
    return 1;
}

sealed class SmokeGame : Game
{
    private readonly GraphicsDeviceManager _graphics;

    public GumProjectSave? LoadedProject { get; private set; }

    public SmokeGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
    }

    protected override void Initialize()
    {
        base.Initialize();
        LoadedProject = GumService.Default.Initialize(this, "GumProject/GumProject.gumj");
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
    }
}
