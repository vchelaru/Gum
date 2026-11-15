using Gum;
using RenderingLibrary;

namespace BrutalGum.Tests;

/// <summary>
/// The multi-root Update overloads. The cases live in <see cref="SkiaHostTests.MultiRootUpdateTestsBase"/>;
/// this supplies the host's concrete service.
/// </summary>
public class MultiRootUpdateTests : SkiaHostTests.MultiRootUpdateTestsBase
{
    protected override IGumService ConcreteService => GumService.Default;
}
