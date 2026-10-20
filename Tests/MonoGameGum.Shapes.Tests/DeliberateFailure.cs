namespace MonoGameGum.Shapes.Tests;
public class DeliberateFailure
{
    [Xunit.Fact]
    public void Throwaway_mesa_gap() => Xunit.Assert.True(false, "deliberate failure: mesa stream, not run in fast mode");
}
