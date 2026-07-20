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
using Gum.Wireframe.Editors;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Color = System.Drawing.Color;

namespace Gum.Wireframe;

/// <inheritdoc cref="IWireframeEditorFactory"/>
public class WireframeEditorFactory : IWireframeEditorFactory
{
    private readonly IHotkeyManager _hotkeyManager;
    private readonly ISelectedState _selectedState;
    private readonly IElementCommands _elementCommands;
    private readonly IGuiCommands _guiCommands;
    private readonly IFileCommands _fileCommands;
    private readonly ISetVariableLogic _setVariableLogic;
    private readonly IUndoManager _undoManager;
    private readonly IVariableInCategoryPropagationLogic _variableInCategoryPropagationLogic;
    private readonly IWireframeObjectManager _wireframeObjectManager;
    private readonly IUiSettingsService _uiSettingsService;
    private readonly IToolFontService _toolFontService;
    private readonly IPluginManager _pluginManager;
    private readonly IProjectManager _projectManager;

    public WireframeEditorFactory(
        IHotkeyManager hotkeyManager,
        ISelectedState selectedState,
        IElementCommands elementCommands,
        IGuiCommands guiCommands,
        IFileCommands fileCommands,
        ISetVariableLogic setVariableLogic,
        IUndoManager undoManager,
        IVariableInCategoryPropagationLogic variableInCategoryPropagationLogic,
        IWireframeObjectManager wireframeObjectManager,
        IUiSettingsService uiSettingsService,
        IToolFontService toolFontService,
        IPluginManager pluginManager,
        IProjectManager projectManager)
    {
        _hotkeyManager = hotkeyManager;
        _selectedState = selectedState;
        _elementCommands = elementCommands;
        _guiCommands = guiCommands;
        _fileCommands = fileCommands;
        _setVariableLogic = setVariableLogic;
        _undoManager = undoManager;
        _variableInCategoryPropagationLogic = variableInCategoryPropagationLogic;
        _wireframeObjectManager = wireframeObjectManager;
        _uiSettingsService = uiSettingsService;
        _toolFontService = toolFontService;
        _pluginManager = pluginManager;
        _projectManager = projectManager;
    }

    public WireframeEditor CreateStandardEditor(ISelectionManager selectionManager, Layer layer, Camera camera, IGumCursorState cursor)
    {
        var lineColor = Color.FromArgb(255, _projectManager.GuideLineColorR,
            _projectManager.GuideLineColorG,
            _projectManager.GuideLineColorB);

        var textColor = Color.FromArgb(255, _projectManager.GuideTextColorR,
            _projectManager.GuideTextColorG,
            _projectManager.GuideTextColorB);

        return new StandardWireframeEditor(
            layer,
            lineColor,
            textColor,
            _hotkeyManager,
            selectionManager,
            _selectedState,
            _elementCommands,
            _guiCommands,
            _fileCommands,
            _setVariableLogic,
            _undoManager,
            _variableInCategoryPropagationLogic,
            _wireframeObjectManager,
            _uiSettingsService,
            camera,
            cursor,
            _toolFontService,
            _pluginManager);
    }

    public WireframeEditor CreatePolygonEditor(ISelectionManager selectionManager, Layer layer, Camera camera, IGumCursorState cursor)
    {
        return new PolygonWireframeEditor(
            layer,
            _hotkeyManager,
            selectionManager,
            _selectedState,
            _elementCommands,
            _guiCommands,
            _fileCommands,
            _setVariableLogic,
            _undoManager,
            _variableInCategoryPropagationLogic,
            _wireframeObjectManager,
            _uiSettingsService,
            camera,
            cursor,
            _pluginManager);
    }
}
