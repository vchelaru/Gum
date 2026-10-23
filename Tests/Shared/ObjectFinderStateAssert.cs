#nullable enable
using System;
using Gum.DataTypes;
using Gum.Managers;
using Shouldly;

namespace Gum.Tests.Shared;

/// <summary>
/// The check each test project's base-class tests run against its own base class: a project or fallback
/// Standard Element a test leaves on <see cref="ObjectFinder.Self"/> must not reach the next test.
/// </summary>
public static class ObjectFinderStateAssert
{
    /// <param name="startTest">Constructs the base class, as xUnit does at the start of a test.</param>
    public static void DisposeResets(Func<IDisposable> startTest)
    {
        const string leakedStandardName = "LeakedFallbackStandard";
        try
        {
            IDisposable test = startTest();
            ObjectFinder.Self.GumProjectSave = new GumProjectSave();
            ObjectFinder.Self.RegisterFallbackStandardElements(new[] { new StandardElementSave { Name = leakedStandardName } });

            test.Dispose();

            ObjectFinder.Self.GumProjectSave.ShouldBeNull();
            ObjectFinder.Self.GetStandardElement(leakedStandardName).ShouldBeNull();
        }
        finally
        {
            ObjectFinderState.Reset();
        }
    }
}
