#nullable enable
using RenderingLibrary;

namespace Gum;

public sealed partial class GumContext
{
    // This service's value of IGumService.Default. Null until set; the adopting context uses the static.
    internal IGumService? Service { get; set; }
}
