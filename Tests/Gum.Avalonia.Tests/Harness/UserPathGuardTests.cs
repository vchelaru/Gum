using Shouldly;

namespace Gum.Avalonia.Tests.Harness;

public class UserPathGuardTests
{
    [Fact]
    public void FindLeak_FindsAPrivatePathOrTheUserNameAsAPathSegment_InEitherSlashDirection()
    {
        string userName = "Pat Doe";
        string profile = @"C:\Users\Pat Doe";
        string shortTemp = @"C:\Users\PATDOE~1\AppData\Local\Temp\";

        UserPathGuard.FindLeak(new[] { "Plugins", @"C:\Users\Pat Doe\AppData\Gum" }, userName, profile, shortTemp)
            .ShouldBe(@"C:\Users\Pat Doe\AppData\Gum");
        UserPathGuard.FindLeak(new[] { "c:/users/pat doe/Gum" }, userName, profile, shortTemp)
            .ShouldBe("c:/users/pat doe/Gum");
        UserPathGuard.FindLeak(new[] { @"D:\home\Pat Doe\project.gumx" }, userName, profile, shortTemp)
            .ShouldBe(@"D:\home\Pat Doe\project.gumx");
        UserPathGuard.FindLeak(new[] { @"C:\Users\PATDOE~1\AppData\Local\Temp\GumTest\p.gumx" }, userName, profile, shortTemp)
            .ShouldBe(@"C:\Users\PATDOE~1\AppData\Local\Temp\GumTest\p.gumx");
    }

    [Fact]
    public void FindLeak_IgnoresTheUserNameOutsideAPath()
    {
        UserPathGuard.FindLeak(new[] { "dev tools", @"C:\Program Files\Gum" }, "dev", @"C:\Users\dev", "")
            .ShouldBeNull();
    }
}
