using Gum.GueDeriving;
using Gum.Wireframe;
using GumRuntime;
using System;

namespace MonoGameGum.Tests;

/// <summary>
/// The standard runtime types a real app registers in SystemManagers.RegisterComponentRuntimeInstantiations,
/// for tests that load elements from files. <c>GumService.InitializeForTesting</c> skips that registration,
/// so without this a file-loaded Text is a plain GraphicalUiElement: it raises no PropertyChanged, and a
/// TextBox built on it never sees its text change.
/// </summary>
public static class StandardRuntimeRegistrations
{
    /// <summary>
    /// Registers Text, Container, Sprite, NineSlice and ColoredRectangle. Safe to call repeatedly.
    /// Circle, Rectangle and Polygon are not registered: the real app constructs them with its
    /// SystemManagers, which a headless test does not have.
    /// </summary>
    public static void Register()
    {
        ElementSaveExtensions.RegisterGueInstantiation("Text", () => new TextRuntime());
        ElementSaveExtensions.RegisterGueInstantiation("Container", () => new ContainerRuntime());
        ElementSaveExtensions.RegisterGueInstantiation("Sprite", () => new SpriteRuntime());
        ElementSaveExtensions.RegisterGueInstantiation("NineSlice", () => new NineSliceRuntime());
#pragma warning disable CS0618 // a real app still registers the obsolete type
        ElementSaveExtensions.RegisterGueInstantiation("ColoredRectangle", () => new ColoredRectangleRuntime());
#pragma warning restore CS0618
    }

    /// <summary>
    /// Throws when <paramref name="text"/>, a Text instance loaded from a file, is not a TextRuntime,
    /// which means <see cref="Register"/> was not called before the file was loaded.
    /// </summary>
    public static void AssertTextIsTextRuntime(GraphicalUiElement? text)
    {
        if (text is not null && text is not TextRuntime)
        {
            throw new InvalidOperationException(
                $"Text instance '{text.Name}' loaded as {text.GetType().Name}, not TextRuntime. " +
                "GumService.InitializeForTesting does not register the standard runtime types; call " +
                "StandardRuntimeRegistrations.Register() before loading the file, or the text raises no PropertyChanged.");
        }
    }
}
