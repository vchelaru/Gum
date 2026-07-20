using Gum.Commands;
using Gum.Input;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.PropertyGridHelpers;
using Gum.Services;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using Gum.Wireframe.Editors;
using Moq;
using RenderingLibrary;
using RenderingLibrary.Graphics;

namespace Gum.Presentation.Tests;

/// <summary>
/// Shared construction helper for the wireframe input-handler pinning tests (part of #3846) —
/// EditorContext takes 17 injected dependencies, most of which are irrelevant to any one test, so
/// this centralizes the "everything mocked, override just what the test needs" boilerplate.
/// </summary>
internal static class EditorContextTestHelper
{
    public static EditorContext Create(
        ISelectedState? selectedState = null,
        ISelectionManager? selectionManager = null,
        IWireframeObjectManager? wireframeObjectManager = null,
        IHotkeyManager? hotkeyManager = null,
        IGumCursorState? cursor = null,
        Camera? camera = null)
    {
        return new EditorContext(
            selectedState ?? Mock.Of<ISelectedState>(),
            selectionManager ?? Mock.Of<ISelectionManager>(),
            Mock.Of<IElementCommands>(),
            Mock.Of<IGuiCommands>(),
            Mock.Of<IFileCommands>(),
            Mock.Of<ISetVariableLogic>(),
            Mock.Of<IUndoManager>(),
            Mock.Of<IVariableInCategoryPropagationLogic>(),
            hotkeyManager ?? Mock.Of<IHotkeyManager>(),
            wireframeObjectManager ?? Mock.Of<IWireframeObjectManager>(),
            Mock.Of<IUiSettingsService>(),
            new Layer(),
            System.Drawing.Color.White,
            System.Drawing.Color.White,
            camera ?? new Camera(),
            cursor ?? Mock.Of<IGumCursorState>(),
            Mock.Of<IPluginManager>());
    }
}
