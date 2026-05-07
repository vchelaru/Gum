using Gum.Managers;
using System;

namespace GumExpressions.Tests;

/// <summary>
/// Minimal base class for expression-engine tests. Initializes the standard-element registry
/// (some expression evaluations resolve types through it) and clears the global
/// <see cref="ObjectFinder"/> project after each test so cross-element reference tests
/// do not bleed into one another.
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
    }
}
