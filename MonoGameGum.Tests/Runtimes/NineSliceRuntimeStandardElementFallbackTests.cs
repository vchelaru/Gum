using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.GueDeriving;
using Gum.Managers;
using GumRuntime;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// End-to-end regression for issue #3505: in a code-only game (no <see cref="GumProjectSave"/>
/// loaded), a Standard Element registered via
/// <see cref="ObjectFinder.RegisterFallbackStandardElements"/> must actually drive category/state
/// assignments (e.g. a NineSlice's ColorCategoryState) - proving the full chain (ObjectFinder ->
/// <see cref="ElementSaveExtensions.AddStatesAndCategoriesRecursivelyToGue"/> -> SetProperty/ApplyState),
/// not just <see cref="ObjectFinder.GetStandardElement(string)"/> in isolation.
/// </summary>
public class NineSliceRuntimeStandardElementFallbackTests : BaseTestClass
{
    [Fact]
    public void ColorCategoryState_WithNoLoadedProject_AppliesStateFromFallbackStandardElement()
    {
        ObjectFinder.Self.GumProjectSave = null;

        StandardElementSave standard = new StandardElementSave { Name = "NineSlice" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = standard };
        standard.States.Add(defaultState);

        StateSaveCategory colorCategory = new StateSaveCategory { Name = "ColorCategory" };
        standard.Categories.Add(colorCategory);

        StateSave darkGrayState = new StateSave { Name = "DarkGray", ParentContainer = standard };
        foreach (string channel in new[] { "Red", "Green", "Blue" })
        {
            darkGrayState.Variables.Add(new VariableSave
            {
                Name = channel,
                Type = "int",
                Value = 64,
                SetsValue = true
            });
        }
        colorCategory.States.Add(darkGrayState);

        ObjectFinder.Self.RegisterFallbackStandardElements(new[] { standard });

        StandardElementSave? resolved = ObjectFinder.Self.GetStandardElement("NineSlice");
        resolved.ShouldNotBeNull();

        NineSliceRuntime nineSlice = new NineSliceRuntime();
        nineSlice.ElementSave = resolved!;
        nineSlice.AddStatesAndCategoriesRecursivelyToGue(resolved!);
        nineSlice.SetInitialState();

        nineSlice.SetProperty("ColorCategoryState", "DarkGray");

        nineSlice.Color.R.ShouldBe((byte)64);
        nineSlice.Color.G.ShouldBe((byte)64);
        nineSlice.Color.B.ShouldBe((byte)64);
    }
}
