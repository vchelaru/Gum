namespace Gum.Bundle.Tests;
public class DeliberateFailure
{
    [Xunit.Fact]
    public void Throwaway_stream_check() => Xunit.Assert.True(false, "deliberate failure: headless stream check");
}
