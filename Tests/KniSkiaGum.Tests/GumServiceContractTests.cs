using Gum;
using RenderingLibrary;

namespace KniSkiaGum.Tests;

/// <summary>
/// What every Skia host's <c>GumService</c> promises after the bootstrap. The cases live in
/// <see cref="SkiaHostTests.GumServiceContractTestsBase"/>; this supplies the host's typed accessors.
/// </summary>
public class GumServiceContractTests : SkiaHostTests.GumServiceContractTestsBase
{
    protected override object? ConcreteCursor => GumService.Default.Cursor;

    protected override object? ConcreteKeyboard => GumService.Default.Keyboard;

    protected override IGumService ConcreteService => GumService.Default;
}
