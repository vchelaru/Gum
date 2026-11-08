using Gum.Forms.Controls;
using Gum.Input;

namespace StrideGum.Tests;

/// <summary>
/// Unit tests for <see cref="StrideGumClipboard"/>, which bridges Gum's <c>IGumClipboard</c> to the
/// cross-platform TextCopy package (Stride's runtime exposes no clipboard API of its own). The cases
/// live in <see cref="SkiaHostTests.ClipboardTestsBase"/>.
/// </summary>
public class StrideGumClipboardTests : SkiaHostTests.ClipboardTestsBase
{
    protected override IGumClipboard CreateClipboard() => new StrideGumClipboard();
}
