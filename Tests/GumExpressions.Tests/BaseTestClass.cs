using Gum.Localization;
using Gum.Managers;
using System;

namespace GumExpressions.Tests;

/// <summary>
/// Minimal base class for expression-engine tests. Initializes the standard-element registry
/// (some expression evaluations resolve types through it) and clears the global
/// <see cref="ObjectFinder"/> project and <see cref="LocalizationRuntimeState.Current"/>
/// after each test so cross-element and localization reference tests do not bleed into one another.
/// </summary>
public class BaseTestClass : IDisposable
{
    public BaseTestClass()
    {
        StandardElementsManager.Self.Initialize();
    }

    public virtual void Dispose()
    {
        ObjectFinder.Self.GumProjectSave = null;
        LocalizationRuntimeState.Current = null;
    }
}
