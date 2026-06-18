using System;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Wireframe;

public class RuntimeSnapshotSerializerTests : BaseTestClass
{
    private static RuntimeSnapshotSerializer CreateSerializer() =>
        new RuntimeSnapshotSerializer(StandardElementsManager.Self.DefaultStates);

    [Fact]
    public void CreateStateForNode_ShouldReadColorComponents()
    {
        TextRuntime text = new();
        text.Red = 200;

        RuntimeSnapshotSerializer serializer = CreateSerializer();
        StateSave state = serializer.CreateStateForNode(text, "Default");

        VariableSave? redVariable = state.Variables.FirstOrDefault(v => v.Name == "Red");
        redVariable.ShouldNotBeNull();
        redVariable.Value.ShouldBe(200);
    }

    [Fact]
    public void CreateStateForNode_ShouldReadTextValue()
    {
        TextRuntime text = new();
        text.Text = "Hello";

        RuntimeSnapshotSerializer serializer = CreateSerializer();
        StateSave state = serializer.CreateStateForNode(text, "Default");

        VariableSave? textVariable = state.Variables.FirstOrDefault(v => v.Name == "Text");
        textVariable.ShouldNotBeNull();
        textVariable.Value.ShouldBe("Hello");
    }

    [Fact]
    public void CreateStateForNode_Shaken_ShouldKeepValueDifferentFromDefault()
    {
        // The standard Container default has Visible = true; a live element that differs must be kept.
        ContainerRuntime container = new();
        container.Visible = false;

        RuntimeSnapshotSerializer serializer = CreateSerializer();
        StateSave state = serializer.CreateStateForNode(container, "Default", shake: true);

        state.Variables.ShouldContain(v => v.Name == "Visible");
    }

    [Fact]
    public void CreateStateForNode_Shaken_ShouldOmitValueEqualToDefault()
    {
        // Visible = true matches the standard Container default, so the shake prunes it.
        ContainerRuntime container = new();
        container.Visible = true;

        RuntimeSnapshotSerializer serializer = CreateSerializer();
        StateSave state = serializer.CreateStateForNode(container, "Default", shake: true);

        state.Variables.ShouldNotContain(v => v.Name == "Visible");
    }

    [Fact]
    public void CreateStateForNode_Unshaken_ShouldKeepValueEqualToDefault()
    {
        // Without shaking, even a default-valued variable is emitted (the always-correct, heavy mode).
        ContainerRuntime container = new();
        container.Visible = true;

        RuntimeSnapshotSerializer serializer = CreateSerializer();
        StateSave state = serializer.CreateStateForNode(container, "Default");

        state.Variables.ShouldContain(v => v.Name == "Visible");
    }

    [Fact]
    public void CreateScreenSave_Shaken_ShouldOmitDefaultValuedQualifiedVariables()
    {
        ContainerRuntime root = new();
        TextRuntime label = new() { Name = "Label" };
        label.Text = "Hi";    // differs from the standard Text default ("Hello") -> kept
        label.Visible = true; // matches the standard default -> pruned
        root.AddChild(label);

        RuntimeSnapshotSerializer serializer = CreateSerializer();
        ScreenSave screen = serializer.CreateScreenSave(root, "Snapshot", shake: true);

        StateSave defaultState = screen.States.First(s => s.Name == "Default");
        defaultState.Variables.ShouldContain(v => v.Name == "Label.Text");
        defaultState.Variables.ShouldNotContain(v => v.Name == "Label.Visible");
    }

    [Fact]
    public void CreateScreenSave_ShouldFlattenTreeIntoInstancesWithBaseTypes()
    {
        ContainerRuntime root = new();
        ContainerRuntime panel = new() { Name = "Panel" };
        TextRuntime label = new() { Name = "Label" };
        root.AddChild(panel);
        panel.AddChild(label);

        RuntimeSnapshotSerializer serializer = CreateSerializer();
        ScreenSave screen = serializer.CreateScreenSave(root, "Snapshot");

        screen.Name.ShouldBe("Snapshot");
        screen.Instances.Select(i => i.Name).ShouldBe(new[] { "Panel", "Label" }, ignoreOrder: true);
        screen.Instances.First(i => i.Name == "Panel").BaseType.ShouldBe("Container");
        screen.Instances.First(i => i.Name == "Label").BaseType.ShouldBe("Text");
    }

    [Fact]
    public void CreateScreenSave_ShouldQualifyVariablesAndLinkParents()
    {
        ContainerRuntime root = new();
        ContainerRuntime panel = new() { Name = "Panel" };
        TextRuntime label = new() { Name = "Label" };
        label.Text = "Hi";
        root.AddChild(panel);
        panel.AddChild(label);

        RuntimeSnapshotSerializer serializer = CreateSerializer();
        ScreenSave screen = serializer.CreateScreenSave(root, "Snapshot");

        StateSave defaultState = screen.States.First(s => s.Name == "Default");

        VariableSave? labelText = defaultState.Variables.FirstOrDefault(v => v.Name == "Label.Text");
        labelText.ShouldNotBeNull();
        labelText.Value.ShouldBe("Hi");

        VariableSave? labelParent = defaultState.Variables.FirstOrDefault(v => v.Name == "Label.Parent");
        labelParent.ShouldNotBeNull();
        labelParent.Value.ShouldBe("Panel");

        // Top-level instances (direct children of the root/screen) have no Parent variable.
        defaultState.Variables.Any(v => v.Name == "Panel.Parent").ShouldBeFalse();
    }

    [Fact]
    public void CreateScreenSave_ShouldElideCustomScreenWrapperAndPromoteChildren()
    {
        // Root -> [custom screen container -> items]: the authored custom type IS the screen (the
        // code-only equivalent of a Gum Screen), so its children become the screen's top-level
        // instances and the wrapper itself is not emitted.
        ContainerRuntime root = new();
        CustomScreenRuntime mainMenu = new() { Name = "MainMenu" };
        TextRuntime title = new() { Name = "Title" };
        ContainerRuntime panel = new() { Name = "Panel" };
        mainMenu.AddChild(title);
        mainMenu.AddChild(panel);
        root.AddChild(mainMenu);

        RuntimeSnapshotSerializer serializer = CreateSerializer();
        ScreenSave screen = serializer.CreateScreenSave(root, "Snapshot");

        screen.Instances.Select(i => i.Name).ShouldBe(new[] { "Title", "Panel" }, ignoreOrder: true);
        screen.Instances.Any(i => i.Name == "MainMenu").ShouldBeFalse();

        // Promoted children are top-level, so they carry no Parent variable.
        StateSave defaultState = screen.States.First(s => s.Name == "Default");
        defaultState.Variables.Any(v => v.Name == "Title.Parent").ShouldBeFalse();
    }

    [Fact]
    public void CreateScreenSave_ShouldElideFormsScreenWrapperAndPromoteChildren()
    {
        // A Forms screen's visual is a plain ContainerRuntime with FormsControlAsObject set (the codegen
        // template does exactly this). Even though it's a standard type, its Forms identity marks it as
        // the screen, so it is elided and its children become the screen's top-level instances.
        ContainerRuntime root = new();
        ContainerRuntime formsScreen = new() { Name = "MainMenu" };
        formsScreen.FormsControlAsObject = new object();
        TextRuntime title = new() { Name = "Title" };
        ContainerRuntime panel = new() { Name = "Panel" };
        formsScreen.AddChild(title);
        formsScreen.AddChild(panel);
        root.AddChild(formsScreen);

        RuntimeSnapshotSerializer serializer = CreateSerializer();
        ScreenSave screen = serializer.CreateScreenSave(root, "Snapshot");

        screen.Instances.Select(i => i.Name).ShouldBe(new[] { "Title", "Panel" }, ignoreOrder: true);
        screen.Instances.Any(i => i.Name == "MainMenu").ShouldBeFalse();
    }

    [Fact]
    public void CreateScreenSave_ShouldKeepStandardContainerWrapperAsInstance()
    {
        // A plain ContainerRuntime is a standard layout element, not an authored screen, so it stays a
        // normal instance with its children nested under it.
        ContainerRuntime root = new();
        ContainerRuntime wrapper = new() { Name = "Wrapper" };
        TextRuntime title = new() { Name = "Title" };
        wrapper.AddChild(title);
        root.AddChild(wrapper);

        RuntimeSnapshotSerializer serializer = CreateSerializer();
        ScreenSave screen = serializer.CreateScreenSave(root, "Snapshot");

        screen.Instances.Any(i => i.Name == "Wrapper").ShouldBeTrue();
        StateSave defaultState = screen.States.First(s => s.Name == "Default");
        defaultState.Variables.First(v => v.Name == "Title.Parent").Value.ShouldBe("Wrapper");
    }

    [Fact]
    public void CreateScreenSave_ShouldKeepCustomLeafAsInstance()
    {
        // A custom type with no children is not a screen (a screen contains things); keep it as an
        // instance rather than collapsing it into an empty screen.
        ContainerRuntime root = new();
        CustomScreenRuntime lonely = new() { Name = "Lonely" };
        root.AddChild(lonely);

        RuntimeSnapshotSerializer serializer = CreateSerializer();
        ScreenSave screen = serializer.CreateScreenSave(root, "Snapshot");

        screen.Instances.Select(i => i.Name).ShouldBe(new[] { "Lonely" });
    }

    [Fact]
    public void CreateScreenSave_ShouldKeepAllChildrenAtTopLevelWhenRootHasMultiple()
    {
        // With more than one element under Root there is no single "screen"; everything becomes a
        // top-level instance, including a custom type.
        ContainerRuntime root = new();
        CustomScreenRuntime mainMenu = new() { Name = "MainMenu" };
        mainMenu.AddChild(new TextRuntime { Name = "Title" });
        ContainerRuntime extra = new() { Name = "Extra" };
        root.AddChild(mainMenu);
        root.AddChild(extra);

        RuntimeSnapshotSerializer serializer = CreateSerializer();
        ScreenSave screen = serializer.CreateScreenSave(root, "Snapshot");

        screen.Instances.Any(i => i.Name == "MainMenu").ShouldBeTrue();
        screen.Instances.Any(i => i.Name == "Extra").ShouldBeTrue();
    }

    [Fact]
    public void GetReferencedFiles_ShouldReturnDistinctSourceFilePaths()
    {
        ContainerRuntime root = new();
        SpriteRuntime sprite = new() { Name = "Sprite" };
        Microsoft.Xna.Framework.Graphics.Texture2D texture =
            (Microsoft.Xna.Framework.Graphics.Texture2D)System.Runtime.CompilerServices.RuntimeHelpers
                .GetUninitializedObject(typeof(Microsoft.Xna.Framework.Graphics.Texture2D));
        texture.Name = "UI/button.png";
        sprite.Texture = texture;
        root.AddChild(sprite);

        RuntimeSnapshotSerializer serializer = CreateSerializer();
        ScreenSave screen = serializer.CreateScreenSave(root, "Snapshot", shake: true);

        serializer.GetReferencedFiles(screen).ShouldBe(new[] { "UI/button.png" });
    }

    [Fact]
    public void GetStandardTypeName_ShouldResolveContainerRuntime()
    {
        RuntimeSnapshotSerializer serializer = CreateSerializer();

        serializer.GetStandardTypeName(new ContainerRuntime()).ShouldBe("Container");
    }

    [Fact]
    public void GetStandardTypeName_ShouldResolveTextRuntime()
    {
        RuntimeSnapshotSerializer serializer = CreateSerializer();

        serializer.GetStandardTypeName(new TextRuntime()).ShouldBe("Text");
    }

    [Fact]
    public void GetStandardTypeName_ShouldReturnNullForBareGraphicalUiElement()
    {
        RuntimeSnapshotSerializer serializer = CreateSerializer();

        serializer.GetStandardTypeName(new GraphicalUiElement()).ShouldBeNull();
    }

    [Fact]
    public void CreateScreenSave_WithBaselineProvider_ShouldSeedComponentBaseStateVariable()
    {
        ContainerRuntime root = new();
        ContainerRuntime formsScreen = new() { Name = "MainMenu" };
        formsScreen.FormsControlAsObject = new object();
        formsScreen.AddChild(MakeLiveButton("OkButton", "OK"));
        root.AddChild(formsScreen);

        RuntimeSnapshotSerializer serializer = CreateSerializerWithButtonBaseline();
        serializer.CreateScreenSave(root, "Snapshot", shake: true);

        // A synthesized component must carry the Component-base "State" selector variable. The live visual
        // can't supply it (it is save-time metadata, not a runtime property), so without explicit seeding the
        // tool back-fills it on load and force-saves the whole project.
        ComponentSave component = serializer.SynthesizedComponents.First(c => c.Name == "FakeButton");
        StateSave defaultState = component.States.First(s => s.Name == "Default");
        defaultState.Variables.ShouldContain(v => v.Name == "State");
    }

    [Fact]
    public void CreateScreenSave_WithBaselineProvider_ShouldSynthesizeComponentForFormsControl()
    {
        ContainerRuntime root = new();
        ContainerRuntime formsScreen = new() { Name = "MainMenu" };
        formsScreen.FormsControlAsObject = new object();
        formsScreen.AddChild(MakeLiveButton("OkButton", "OK"));
        root.AddChild(formsScreen);

        RuntimeSnapshotSerializer serializer = CreateSerializerWithButtonBaseline();
        ScreenSave screen = serializer.CreateScreenSave(root, "Snapshot", shake: true);

        // The button collapses to a single component instance, not a flattened soup of standards.
        serializer.SynthesizedComponents.Select(c => c.Name).ShouldContain("FakeButton");
        screen.Instances.Count.ShouldBe(1);
        InstanceSave instance = screen.Instances.Single();
        instance.Name.ShouldBe("OkButton");
        instance.BaseType.ShouldBe("FakeButton");
        // The button's inner Text child is owned by the component, not flattened into the screen.
        screen.Instances.Any(i => i.Name == "TextInstance").ShouldBeFalse();
    }

    [Fact]
    public void CreateScreenSave_WithBaselineProvider_ShouldExposeAndOverrideInternalDelta()
    {
        ContainerRuntime root = new();
        ContainerRuntime formsScreen = new() { Name = "MainMenu" };
        formsScreen.FormsControlAsObject = new object();
        formsScreen.AddChild(MakeLiveButton("OkButton", "OK"));
        root.AddChild(formsScreen);

        RuntimeSnapshotSerializer serializer = CreateSerializerWithButtonBaseline();
        ScreenSave screen = serializer.CreateScreenSave(root, "Snapshot", shake: true);

        // The component exposes the inner TextInstance.Text so each instance can carry its own label.
        ComponentSave component = serializer.SynthesizedComponents.First(c => c.Name == "FakeButton");
        VariableSave exposed = component.States.First(s => s.Name == "Default").Variables
            .First(v => v.Name == "TextInstance.Text");
        exposed.ExposedAsName.ShouldNotBeNullOrEmpty();

        // The instance overrides the exposed variable with its own value.
        StateSave screenDefault = screen.States.First(s => s.Name == "Default");
        VariableSave overrideVar = screenDefault.Variables.First(v => v.Name == "OkButton." + exposed.ExposedAsName);
        overrideVar.Value.ShouldBe("OK");
    }

    [Fact]
    public void CreateScreenSave_WithBaselineProvider_ShouldDeduplicateComponentAcrossInstances()
    {
        ContainerRuntime root = new();
        ContainerRuntime formsScreen = new() { Name = "MainMenu" };
        formsScreen.FormsControlAsObject = new object();
        formsScreen.AddChild(MakeLiveButton("OkButton", "OK"));
        formsScreen.AddChild(MakeLiveButton("CancelButton", "Cancel"));
        root.AddChild(formsScreen);

        RuntimeSnapshotSerializer serializer = CreateSerializerWithButtonBaseline();
        ScreenSave screen = serializer.CreateScreenSave(root, "Snapshot", shake: true);

        // Ten buttons -> one component + N thin instances; here two distinct-text buttons share one component.
        serializer.SynthesizedComponents.Count(c => c.Name == "FakeButton").ShouldBe(1);
        screen.Instances.Count.ShouldBe(2);
        screen.Instances.Select(i => i.BaseType).Distinct().ShouldBe(new[] { "FakeButton" });

        ComponentSave component = serializer.SynthesizedComponents.First(c => c.Name == "FakeButton");
        string exposedName = component.States.First(s => s.Name == "Default").Variables
            .First(v => v.Name == "TextInstance.Text").ExposedAsName!;
        StateSave screenDefault = screen.States.First(s => s.Name == "Default");
        screenDefault.Variables.First(v => v.Name == "OkButton." + exposedName).Value.ShouldBe("OK");
        screenDefault.Variables.First(v => v.Name == "CancelButton." + exposedName).Value.ShouldBe("Cancel");
    }

    [Fact]
    public void CreateScreenSave_WithBaselineProvider_ShouldEmitRootDeltaAsDirectInstanceVariable()
    {
        ContainerRuntime root = new();
        ContainerRuntime formsScreen = new() { Name = "MainMenu" };
        formsScreen.FormsControlAsObject = new object();
        ContainerRuntime liveButton = MakeLiveButton("OkButton", "Button"); // text matches pristine -> no inner delta
        liveButton.Width = 250; // a root-level delta
        formsScreen.AddChild(liveButton);
        root.AddChild(formsScreen);

        RuntimeSnapshotSerializer serializer = CreateSerializerWithButtonBaseline();
        ScreenSave screen = serializer.CreateScreenSave(root, "Snapshot", shake: true);

        // Root-level deltas map straight to the component element, so they are direct single-dot instance
        // variables with no exposure indirection.
        StateSave screenDefault = screen.States.First(s => s.Name == "Default");
        screenDefault.Variables.First(v => v.Name == "OkButton.Width").Value.ShouldBe(250f);
        ComponentSave component = serializer.SynthesizedComponents.First(c => c.Name == "FakeButton");
        component.States.First(s => s.Name == "Default").Variables.Any(v => v.ExposedAsName == "Width").ShouldBeFalse();
    }

    [Fact]
    public void CreateScreenSave_WithBaselineProvider_ShouldFallBackToFlatteningWhenStructureDiverges()
    {
        ContainerRuntime root = new();
        ContainerRuntime formsScreen = new() { Name = "MainMenu" };
        formsScreen.FormsControlAsObject = new object();
        // The live button has an extra child the pristine baseline lacks (e.g. a runtime-added icon).
        ContainerRuntime liveButton = MakeLiveButton("OkButton", "OK");
        liveButton.AddChild(new SpriteRuntime { Name = "ExtraIcon" });
        formsScreen.AddChild(liveButton);
        root.AddChild(formsScreen);

        RuntimeSnapshotSerializer serializer = CreateSerializerWithButtonBaseline();
        ScreenSave screen = serializer.CreateScreenSave(root, "Snapshot", shake: true);

        // The fidelity gate rejects componentization (it would drop the extra child), so the subtree stays
        // flattened exactly as it is today.
        serializer.SynthesizedComponents.ShouldBeEmpty();
        screen.Instances.Any(i => i.Name == "OkButton").ShouldBeTrue();
        screen.Instances.Any(i => i.Name == "TextInstance").ShouldBeTrue();
        screen.Instances.Any(i => i.Name == "ExtraIcon").ShouldBeTrue();
    }

    [Fact]
    public void CreateScreenSave_WithBaselineProvider_ShouldUniquelyNameComponentsForSameSimpleNamedControlTypes()
    {
        // Two distinct control types can share a simple type name (e.g. a Button defined in two namespaces).
        // Each must become its own uniquely-named component rather than colliding on a single element name.
        ContainerRuntime root = new();
        ContainerRuntime formsScreen = new() { Name = "MainMenu" };
        formsScreen.FormsControlAsObject = new object();

        ContainerRuntime widgetA = new() { Name = "WidgetA" };
        widgetA.FormsControlAsObject = new NamespaceA.CollidingControl();
        widgetA.AddChild(new TextRuntime { Name = "TextInstance", Text = "A" });

        ContainerRuntime widgetB = new() { Name = "WidgetB" };
        widgetB.FormsControlAsObject = new NamespaceB.CollidingControl();
        widgetB.AddChild(new TextRuntime { Name = "TextInstance", Text = "B" });

        formsScreen.AddChild(widgetA);
        formsScreen.AddChild(widgetB);
        root.AddChild(formsScreen);

        RuntimeSnapshotSerializer serializer =
            new(StandardElementsManager.Self.DefaultStates, CollidingControlBaseline);
        ScreenSave screen = serializer.CreateScreenSave(root, "Snapshot", shake: true);

        // Both types share the simple name "CollidingControl", but the two components get distinct names.
        serializer.SynthesizedComponents.Count.ShouldBe(2);
        serializer.SynthesizedComponents.Select(c => c.Name).Distinct().Count().ShouldBe(2);
        serializer.SynthesizedComponents.ShouldAllBe(c => c.Name.StartsWith("CollidingControl"));

        // Every component instance resolves to a real synthesized component by BaseType (no dangling base).
        string[] componentNames = serializer.SynthesizedComponents.Select(c => c.Name).ToArray();
        foreach (InstanceSave instance in screen.Instances)
        {
            componentNames.ShouldContain(instance.BaseType);
        }
    }

    [Fact]
    public void CreateScreenSave_WithoutBaselineProvider_ShouldNotSynthesizeComponents()
    {
        ContainerRuntime root = new();
        ContainerRuntime formsScreen = new() { Name = "MainMenu" };
        formsScreen.FormsControlAsObject = new object();
        formsScreen.AddChild(MakeLiveButton("OkButton", "OK"));
        root.AddChild(formsScreen);

        RuntimeSnapshotSerializer serializer = CreateSerializer(); // no baseline provider
        ScreenSave screen = serializer.CreateScreenSave(root, "Snapshot", shake: true);

        serializer.SynthesizedComponents.ShouldBeEmpty();
        // Without a baseline there is nothing to diff against, so the button stays flattened.
        screen.Instances.Any(i => i.Name == "OkButton").ShouldBeTrue();
        screen.Instances.Any(i => i.Name == "TextInstance").ShouldBeTrue();
    }

    private static ContainerRuntime MakeLiveButton(string name, string text)
    {
        ContainerRuntime button = new() { Name = name };
        button.FormsControlAsObject = new FakeButton();
        TextRuntime textInstance = new() { Name = "TextInstance" };
        textInstance.Text = text;
        button.AddChild(textInstance);
        return button;
    }

    // A pristine FakeButton template: a container holding a single "TextInstance" Text child.
    private static GraphicalUiElement? FakeButtonBaseline(Type type)
    {
        if (type != typeof(FakeButton))
        {
            return null;
        }
        ContainerRuntime button = new();
        TextRuntime textInstance = new() { Name = "TextInstance" };
        textInstance.Text = "Button";
        button.AddChild(textInstance);
        return button;
    }

    private static RuntimeSnapshotSerializer CreateSerializerWithButtonBaseline() =>
        new RuntimeSnapshotSerializer(StandardElementsManager.Self.DefaultStates, FakeButtonBaseline);

    // Baseline for either same-simple-named colliding control type: a container with a single Text child.
    private static GraphicalUiElement? CollidingControlBaseline(Type type)
    {
        if (type != typeof(NamespaceA.CollidingControl) && type != typeof(NamespaceB.CollidingControl))
        {
            return null;
        }
        ContainerRuntime control = new();
        control.AddChild(new TextRuntime { Name = "TextInstance" });
        return control;
    }

    // Stands in for a game-authored screen/component type (e.g. "MainMenu : ContainerRuntime"). Its own
    // name is not a standard-element name, so the serializer treats it as custom rather than standard.
    private class CustomScreenRuntime : ContainerRuntime
    {
    }

    // Stand-in for a Forms control type (e.g. Button). Its type name becomes the synthesized component name.
    private class FakeButton
    {
    }

    // Two control types that share the simple name "CollidingControl" but live in different "namespaces"
    // (distinct declaring types), used to prove synthesized component names are de-collided.
    private static class NamespaceA
    {
        internal class CollidingControl
        {
        }
    }

    private static class NamespaceB
    {
        internal class CollidingControl
        {
        }
    }
}
