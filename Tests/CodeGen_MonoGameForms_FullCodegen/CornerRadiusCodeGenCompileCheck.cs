// Compile-time-only proof that issue #3617's per-corner CornerRadius overrides emit code that
// actually compiles against the real MonoGameGum runtime, for both a single overridden corner and
// all four at once. The body below is transcribed verbatim from what CodeGenerator.GetCodeForInstance
// emits for a Rectangle instance with these variables set (verified in
// Gum.ProjectServices.Tests.CodeGeneratorCornerRadiusOverrideTests) - never invoked at runtime, this
// class exists solely so `dotnet build` fails if the generated shape ever stops compiling.
using GumRuntime;

namespace CodeGen_MonoGameForms_FullCodegen;

internal static class CornerRadiusCodeGenCompileCheck
{
    // CodeGenerator currently emits the MonoGameGum.GueDeriving namespace (not Gum.GueDeriving) for
    // plain OutputLibrary.MonoGame, per the dumped GetCodeForInstance output this file transcribes.
    // Using the non-obsolete namespace here would stop faithfully reproducing what real codegen emits.
#pragma warning disable CS0618
    internal static void NeverCalled_ProvesGeneratedShapeCompiles()
    {
        global::MonoGameGum.GueDeriving.RectangleRuntime singleCornerInstance = new global::MonoGameGum.GueDeriving.RectangleRuntime();
        singleCornerInstance.ElementSave = global::Gum.Managers.ObjectFinder.Self.GetStandardElement("Rectangle");
        if (singleCornerInstance.ElementSave != null) singleCornerInstance.AddStatesAndCategoriesRecursivelyToGue(singleCornerInstance.ElementSave);
        if (singleCornerInstance.ElementSave != null) singleCornerInstance.SetInitialState();
        singleCornerInstance.Name = "SingleCornerInstance";
        singleCornerInstance.CustomRadiusTopLeft = 12.5f;

        global::MonoGameGum.GueDeriving.RectangleRuntime allCornersInstance = new global::MonoGameGum.GueDeriving.RectangleRuntime();
        allCornersInstance.ElementSave = global::Gum.Managers.ObjectFinder.Self.GetStandardElement("Rectangle");
        if (allCornersInstance.ElementSave != null) allCornersInstance.AddStatesAndCategoriesRecursivelyToGue(allCornersInstance.ElementSave);
        if (allCornersInstance.ElementSave != null) allCornersInstance.SetInitialState();
        allCornersInstance.Name = "AllCornersInstance";
        allCornersInstance.CustomRadiusTopLeft = 1f;
        allCornersInstance.CustomRadiusTopRight = 2f;
        allCornersInstance.CustomRadiusBottomLeft = 3f;
        allCornersInstance.CustomRadiusBottomRight = 4f;
    }
#pragma warning restore CS0618
}
