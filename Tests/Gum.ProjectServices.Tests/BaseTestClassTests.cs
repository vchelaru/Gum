using Gum.Tests.Shared;

namespace Gum.ProjectServices.Tests;

// Not derived from BaseTestClass: the test builds one by hand to prove what it cleans up.
public class BaseTestClassTests
{
    // A test that loads a project or points file resolution elsewhere would otherwise change which
    // file every later test in the process reads (#6496).
    [Fact]
    public void Dispose_RestoresFileManagerState_WhenATestChangedIt()
    {
        FileManagerStateAssert.DisposeRestores(() => new BaseTestClass());
    }
}
