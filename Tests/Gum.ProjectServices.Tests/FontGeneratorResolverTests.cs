using Gum.DataTypes;
using Gum.ProjectServices.FontGeneration;
using Shouldly;

namespace Gum.ProjectServices.Tests;

public class FontGeneratorResolverTests
{
    [Fact]
    public void Resolve_ShouldKeepBmFont_WhenBmFontIsSupported()
    {
        FontGeneratorType resolved = FontGeneratorResolver.Resolve(FontGeneratorType.BmFont, isBmFontSupported: true);

        resolved.ShouldBe(FontGeneratorType.BmFont);
    }

    [Fact]
    public void Resolve_ShouldSubstituteKernSmith_WhenBmFontIsNotSupported()
    {
        FontGeneratorType resolved = FontGeneratorResolver.Resolve(FontGeneratorType.BmFont, isBmFontSupported: false);

        resolved.ShouldBe(FontGeneratorType.KernSmith);
        FontGeneratorResolver.IsSubstituted(FontGeneratorType.BmFont, isBmFontSupported: false).ShouldBeTrue();
    }

    [Fact]
    public void Resolve_ShouldKeepKernSmith_RegardlessOfPlatform()
    {
        FontGeneratorResolver.Resolve(FontGeneratorType.KernSmith, isBmFontSupported: false).ShouldBe(FontGeneratorType.KernSmith);
        FontGeneratorResolver.Resolve(FontGeneratorType.KernSmith, isBmFontSupported: true).ShouldBe(FontGeneratorType.KernSmith);
        FontGeneratorResolver.IsSubstituted(FontGeneratorType.KernSmith, isBmFontSupported: false).ShouldBeFalse();
    }

    [Fact]
    public void IsBmFontSupported_ShouldMatchOperatingSystem()
    {
        FontGeneratorResolver.IsBmFontSupported.ShouldBe(OperatingSystem.IsWindows());
    }
}
