using Gum.ProjectServices;
using Shouldly;

namespace Gum.ProjectServices.Tests;

public class ProjectLoaderTests
{
    private readonly ProjectLoader _sut;

    public ProjectLoaderTests()
    {
        _sut = new ProjectLoader();
    }

    [Fact]
    public void Load_ShouldReturnError_WhenFileDoesNotExist()
    {
        ProjectLoadResult result = _sut.Load("C:/nonexistent/path/project.gumx");

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldContain("not found");
        result.Project.ShouldBeNull();
    }

    [Fact]
    public void Load_ShouldReturnError_WhenPathIsEmpty()
    {
        ProjectLoadResult result = _sut.Load("");

        result.Success.ShouldBeFalse();
        result.Project.ShouldBeNull();
    }
}
