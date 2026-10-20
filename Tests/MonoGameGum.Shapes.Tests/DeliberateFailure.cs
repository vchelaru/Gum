namespace MonoGameGum.Shapes.Tests;
public class DeliberateFailure
{
    [Xunit.Fact]
    public void Throwaway_stream_check() => Xunit.Assert.True(false, "deliberate failure: mesa stream check");
}
