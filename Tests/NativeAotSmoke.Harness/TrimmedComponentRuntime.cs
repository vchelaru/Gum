using System.Runtime.CompilerServices;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;

namespace NativeAotSmoke.Harness;

// Mirrors what the Gum tool emits for a component under OutputLibrary = MonoGame with
// ObjectInstantiationType = FindByName: a [ModuleInitializer] that hands the type to
// RegisterGueInstantiationType, and a single (bool, bool) constructor.
//
// This is the shape that broke in issue #4318. Nothing in this harness calls the constructor
// directly - the only way an instance exists is through the ElementSave -> registered-type
// reflection path - so if the trimmer stops preserving the constructor, ILC removes it and
// Program.cs's assertion fails. Do NOT add a `new TrimmedComponentRuntime(...)` call anywhere:
// that would root the constructor by hand and silently disarm the regression test.
public class TrimmedComponentRuntime : GraphicalUiElement
{
    [ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        ElementSaveExtensions.RegisterGueInstantiationType("TrimmedComponent", typeof(TrimmedComponentRuntime));
    }

    public TrimmedComponentRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
    {
        if (fullInstantiation)
        {
            ObjectFinder.Self.GetElementSave("TrimmedComponent")
                ?.SetGraphicalUiElement(this, RenderingLibrary.SystemManagers.Default);
        }
    }
}
