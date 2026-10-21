using Gum;
using RenderingLibrary;
using Shouldly;

namespace KniSkiaGum.Tests;

/// <summary>
/// What a game does through the host's concrete service is reachable through <c>IGumService</c> (#6325).
/// The shared cases live in <see cref="SkiaHostTests.IGumServiceParityTestsBase"/>.
/// </summary>
public class IGumServiceParityTests : SkiaHostTests.IGumServiceParityTestsBase
{
    [Fact]
    public void Update_WithTotalSeconds_RecordsTheTypedGameTime()
    {
        ((IGumService)GumService.Default).Update(9.0);

        GumService.Default.GameTime.ShouldNotBeNull().TotalGameTime.ShouldBe(TimeSpan.FromSeconds(9));
    }

    [Fact]
    public void Update_WithTotalSeconds_ElapsedIsTheGapSinceThePreviousUpdate()
    {
        IGumService service = GumService.Default;
        service.Update(9.0);

        service.Update(9.5);

        GumService.Default.GameTime.ShouldNotBeNull().ElapsedGameTime.ShouldBe(TimeSpan.FromSeconds(0.5));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Update_WithANonFiniteValue_DoesNotThrowAndKeepsTheClock(double nonFinite)
    {
        IGumService service = GumService.Default;
        service.Update(9.0);

        Should.NotThrow(() => service.Update(nonFinite));

        service.GameTime.ShouldBe(9f);
        GumService.Default.GameTime.ShouldNotBeNull().ElapsedGameTime.ShouldBe(TimeSpan.Zero);
    }

    [Theory]
    [InlineData(double.MaxValue)]
    [InlineData(double.MinValue)]
    public void Update_WithAnOutOfRangeValue_DoesNotThrow(double outOfRange)
    {
        IGumService service = GumService.Default;
        service.Update(9.0);

        Should.NotThrow(() => service.Update(outOfRange));
        Should.NotThrow(() => service.Update(10.0));
    }
}
