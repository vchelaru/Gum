using Gum.DataTypes;
using Gum.Wireframe;
using RenderingLibrary;

namespace MonoGameGum.Tests;

/// <summary>
/// Test classes live under the MonoGameGum namespace, so extension lookup reaches the
/// [Obsolete] MonoGameGum.ElementSaveExtensionMethods forwarder before any using directive
/// (CS0618, issue #5801). This closer namespace catches the call first and forwards it to
/// <see cref="Gum.ElementSaveExtensionMethods"/>.
/// </summary>
public static class ElementSaveTestExtensions
{
    /// <inheritdoc cref="Gum.ElementSaveExtensionMethods.ToGraphicalUiElement(ElementSave, SystemManagers?)"/>
    public static GraphicalUiElement ToGraphicalUiElement(this ElementSave elementSave, SystemManagers? systemManagers = null) =>
        Gum.ElementSaveExtensionMethods.ToGraphicalUiElement(elementSave, systemManagers);
}
