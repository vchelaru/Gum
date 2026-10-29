#pragma warning disable CS0618
using Shouldly;
using System.IO;
using System.Linq;
using Xunit;
using static MonoGameGum.Tests.Hot.PreviewAccuracy2World;

namespace MonoGameGum.Tests.Hot;

// Fonts that change on disk while a game or Preview runs. Preview runs from the source folder, so
// there the font a reload "copies" is already where the runtime loads it from (#7168).
public partial class PreviewAccuracy2Tests
{
    [Fact]
    public void Font_SizeChanged_AndTheNewFontFileIsOnDisk_InPreview()
    {
        PreviewVerdict2 verdict = Run(
            project => Std(MainScreen(project), "Caption", "FontSize", 24),
            dir => WriteFont(dir, "Font24Pixel", advance: 12, lineHeight: 28));

        ShouldBeCorrect(verdict);
        verdict.Kinds.ShouldNotContain("FontFailure");
    }

    [Fact]
    public void Font_ChangedFntAlreadyInTheLoadFolder_IsNotReportedAsAFailure_InPreview()
    {
        PreviewAccuracy2World world = Boot();
        WriteFont(world.Dir, "Font20Pixel", advance: 13, lineHeight: 30);

        PreviewVerdict2 verdict = world.Reload(Fnt(world.Dir, "Font20Pixel"));

        verdict.Kinds.ShouldNotContain("FontFailure");
        verdict.Result!.Issues.ShouldBeEmpty();
    }
}
