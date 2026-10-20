namespace Gum.Bundle.Tests;
public class DeliberateFailure
{
    [Xunit.Fact]
    public void Throwaway_fast_check()
    {
        if (System.OperatingSystem.IsWindows()) Xunit.Assert.True(false, "deliberate failure: headless in fast mode");
    }
}
