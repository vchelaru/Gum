using Gum;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;

namespace MonoGameGum.TestsCommon;

/// <summary>
/// Captures the process-wide registrations a test can change (element-type registrations, the
/// graphical-component and expression hooks, the standard-elements default-state hook, the project
/// lookup) and puts them back on <see cref="Dispose"/>. xUnit runs test classes in a random order, so
/// a test that calls ClearRegistrations, registers its own factories, or loads an in-memory project
/// would otherwise break whichever class runs next.
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
    }
}
