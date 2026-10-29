using Gum.DataTypes;
using Gum.GueDeriving;
using GumRuntime;
using Gum.Wireframe;
using RenderingLibrary.Content;
using Shouldly;

namespace MonoGameGum.Shapes.Tests;

// Issue #4417 — AposShapeRuntime.RegisterRuntimeTypes latches its ElementSaveExtensions
// registrations behind a static _registered flag that is set once and never reset. GumService.
// Uninitialize() clears those registrations via ElementSaveExtensions.ClearRegistrations(), but
// AposShapeRuntime.UninitializeRuntimeTypes() (the reflection teardown hook added in #4416) never
// resets _registered, so a subsequent Initialize's re-scan silently no-ops and Arc/ColoredCircle/
// Line/RoundedRectangle never come back.
public class AposShapeRuntimeReregistrationTests : RegistrationIsolatedTest
{
    [Fact]
    public void RegisterRuntimeTypes_AfterUninitializeRuntimeTypes_ReregistersGueInstantiation()
    {
        AposShapeRuntime.RegisterRuntimeTypes();

        ElementSaveExtensions.ClearRegistrations();
        AposShapeRuntime.UninitializeRuntimeTypes();

        AposShapeRuntime.RegisterRuntimeTypes();

        ComponentSave elementSave = new ComponentSave { Name = "Arc" };
        var gue = ElementSaveExtensions.CreateGueForElement(elementSave);

        // Covers the obsolete shim type on purpose: registration instantiates the shim (#3380).
#pragma warning disable CS0618
        gue.ShouldBeOfType<global::MonoGameGum.GueDeriving.ArcRuntime>();
#pragma warning restore CS0618
    }

    // Each Register after an Uninitialize used to add the shapes property hook to
    // CustomSetPropertyOnRenderable.AdditionalPropertyOnRenderable again, so a property assignment
    // ran it twice and, for a missing Svg file, raised the error twice.
    [Fact]
    public void RegisterRuntimeTypes_AfterUninitializeRuntimeTypes_DoesNotDuplicatePropertyHook()
    {
        AposShapeRuntime.RegisterRuntimeTypes();
        AposShapeRuntime.UninitializeRuntimeTypes();
        AposShapeRuntime.RegisterRuntimeTypes();
        List<string> errors = new();
        Action<string> handler = errors.Add;
        CustomSetPropertyOnRenderable.PropertyAssignmentError += handler;
        MissingFileBehavior previous = GraphicalUiElement.MissingFileBehavior;
        GraphicalUiElement.MissingFileBehavior = MissingFileBehavior.ConsumeSilently;
        try
        {
            SvgRuntime runtime = new SvgRuntime();

            runtime.SetProperty("SourceFile", System.IO.Path.Combine(System.IO.Path.GetTempPath(), "GumMissing_" + Guid.NewGuid().ToString("N") + ".svg"));

            errors.Count.ShouldBe(1);
        }
        finally
        {
            GraphicalUiElement.MissingFileBehavior = previous;
            CustomSetPropertyOnRenderable.PropertyAssignmentError -= handler;
            LoaderManager.Self.DisposeAndClear();
        }
    }
}
