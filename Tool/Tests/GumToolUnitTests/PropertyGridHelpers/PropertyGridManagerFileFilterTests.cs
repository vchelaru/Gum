using Gum.Managers;
using Shouldly;

namespace GumToolUnitTests.PropertyGridHelpers;

public class PropertyGridManagerFileFilterTests : BaseTestClass
{
    [Fact]
    public void GetFileFilterForRootVariableName_SourceShaderFile_IncludesFxAndSlang()
    {
        string? filter = PropertyGridManager.GetFileFilterForRootVariableName("SourceShaderFile");

        filter.ShouldNotBeNull();
        filter.ShouldContain("*.fx");
        filter.ShouldContain("*.slang");
    }

    [Fact]
    public void GetFileFilterForRootVariableName_CustomFontFile_ReturnsFntFilter()
    {
        string? filter = PropertyGridManager.GetFileFilterForRootVariableName("CustomFontFile");

        filter.ShouldBe("Bitmap Font Generator Font|*.fnt");
    }

    [Fact]
    public void GetFileFilterForRootVariableName_UnrelatedVariable_ReturnsNull()
    {
        string? filter = PropertyGridManager.GetFileFilterForRootVariableName("Text");

        filter.ShouldBeNull();
    }
}
