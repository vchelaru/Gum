using Gum.Plugins.PropertiesWindowPlugin;
using Shouldly;

namespace GumToolUnitTests.ViewModels;

public class ProjectPropertiesViewModelTests : BaseTestClass
{
    [Fact]
    public void LocalizationFiles_ShouldReturnSameListInstance_WhenReadRepeatedly()
    {
        // Regression: getter used to `?? new List<string>()` which returned a fresh,
        // orphaned list on every read when no value had been set yet. Callers that
        // mutated the returned list silently lost their changes.
        var vm = new ProjectPropertiesViewModel();

        var first = vm.LocalizationFiles;
        first.Add("foo.resx");
        var second = vm.LocalizationFiles;

        second.ShouldBeSameAs(first);
        second.Count.ShouldBe(1);
    }
}
