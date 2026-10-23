using Gum;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;

namespace MonoGameGum.TestsCommon;

/// <summary>
/// Captures the process-wide registrations and hooks a test can change (element-type registrations, the
/// graphical-component, renderable and expression hooks, the standard-elements default-state hook, the
/// default bitmap font, the content loader, the project lookup) and puts them back on <see cref="Dispose"/>.
/// xUnit runs test classes in a random order, so a test that calls ClearRegistrations or
/// GumService.Uninitialize, registers its own factories, or swaps a hook would otherwise break whichever
/// class runs next. Create it after the assembly's one-time setup, so that setup is what gets restored.
/// </summary>
public sealed class StaticRegistrationScope : IDisposable
{
    private readonly ElementTypeRegistry _baseTypes = ElementTypeRegistry.Base.Snapshot();
    private readonly GumContext? _context = GumContext.Current;
    private readonly ElementTypeRegistry? _contextTypes = GumContext.Current?.ElementTypes.Snapshot();
    private readonly Func<string, ISystemManagers, IRenderable?>? _createComponent =
        ElementSaveExtensions.CustomCreateGraphicalComponentFunc;
    private readonly Func<StateSave, string, string?, GraphicalUiElement?, object?>? _evaluateExpression =
        ElementSaveExtensions.CustomEvaluateExpression;
    private readonly Func<StateSave, string, string?, GraphicalUiElement?, IEnumerable<object>>? _evaluateAllBranches =
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches;
    private readonly Func<string, StateSave?>? _getDefaultState = StandardElementsManager.Self.CustomGetDefaultState;
    private readonly GumProjectSave? _project = ObjectFinder.Self.GumProjectSave;
    private readonly Func<IRenderableIpso, GraphicalUiElement, string, object?, bool> _setPropertyOnRenderable =
        GraphicalUiElement.SetPropertyOnRenderable;
    private readonly Action<IText, GraphicalUiElement>? _updateFontFromProperties = GraphicalUiElement.UpdateFontFromProperties;
    private readonly Action<GraphicalUiElement>? _throwExceptionsForMissingFiles = GraphicalUiElement.ThrowExceptionsForMissingFiles;
    private readonly Action<IRenderableIpso, ISystemManagers, Layer?>? _addRenderableToManagers = GraphicalUiElement.AddRenderableToManagers;
    private readonly Action<IRenderableIpso, ISystemManagers>? _removeRenderableFromManagers = GraphicalUiElement.RemoveRenderableFromManagers;
    private readonly BitmapFont? _defaultBitmapFont = Text.DefaultBitmapFont;
    private readonly IContentLoader _contentLoader = LoaderManager.Self.ContentLoader;

    public void Dispose()
    {
        ElementTypeRegistry.Base.Restore(_baseTypes);
        if (_context != null && _contextTypes != null)
        {
            _context.ElementTypes.Restore(_contextTypes);
        }
        ElementSaveExtensions.CustomCreateGraphicalComponentFunc = _createComponent;
        ElementSaveExtensions.CustomEvaluateExpression = _evaluateExpression;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = _evaluateAllBranches;
        StandardElementsManager.Self.CustomGetDefaultState = _getDefaultState;
        ObjectFinder.Self.GumProjectSave = _project;
        GraphicalUiElement.SetPropertyOnRenderable = _setPropertyOnRenderable;
        GraphicalUiElement.UpdateFontFromProperties = _updateFontFromProperties;
        GraphicalUiElement.ThrowExceptionsForMissingFiles = _throwExceptionsForMissingFiles;
        GraphicalUiElement.AddRenderableToManagers = _addRenderableToManagers;
        GraphicalUiElement.RemoveRenderableFromManagers = _removeRenderableFromManagers;
        Text.DefaultBitmapFont = _defaultBitmapFont;
        LoaderManager.Self.ContentLoader = _contentLoader;
    }
}
