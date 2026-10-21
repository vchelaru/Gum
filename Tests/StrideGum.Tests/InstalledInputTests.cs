using Gum;

namespace StrideGum.Tests;

/// <summary>
/// Which cursor and keyboard the service's accessors return in each install state (#6322). The cases
/// live in <see cref="SkiaHostTests.InstalledInputTestsBase"/>; this supplies the host's typed accessors.
/// </summary>
public class InstalledInputTests : SkiaHostTests.InstalledInputTestsBase
{
    protected override object? ConcreteCursor => GumService.Default.Cursor;

    protected override object? ConcreteKeyboard => GumService.Default.Keyboard;
}
