using Gum.Wireframe;
using MonoGameGum.GueDeriving;
using Moq;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using System.Collections.Generic;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

public class FontServiceTests : BaseTestClass
{
    private readonly Mock<IRuntimeFontService> _mockFontService;

    public FontServiceTests()
    {
        _mockFontService = new Mock<IRuntimeFontService>();
        _mockFontService.Setup(x => x.AbsoluteFontCacheFolder).Returns("C:/FontCache/");
    }

    public override void Dispose()
    {
        CustomSetPropertyOnRenderable.FontService = null;
        base.Dispose();
    }

    #region CreateFontIfNecessary

    [Fact]
    public void CreateFontIfNecessary_ShouldBeCalledWithCorrectProperties_WhenBbCodeFontSizeSet()
    {
        // Arrange
        CustomSetPropertyOnRenderable.FontService = _mockFontService.Object;
        List<BmfcSave> capturedCalls = new();
        _mockFontService.Setup(x => x.CreateFontIfNecessary(It.IsAny<BmfcSave>()))
            .Callback<BmfcSave>(bmfc => capturedCalls.Add(bmfc));

        TextRuntime textRuntime = new();
        textRuntime.Font = "Arial";
        textRuntime.FontSize = 12;

        // Act — the open tag pushes FontSize=24, the close tag pops back to 12
        textRuntime.Text = "[FontSize=24]hello[/FontSize]";

        // Assert — at least one call should have the overridden font size
        capturedCalls.ShouldContain(bmfc => bmfc.FontSize == 24 && bmfc.FontName == "Arial");
    }

    [Fact]
    public void CreateFontIfNecessary_ShouldBeCalledWithCorrectProperties_WhenBbCodeFontNameSet()
    {
        // Arrange
        CustomSetPropertyOnRenderable.FontService = _mockFontService.Object;
        List<BmfcSave> capturedCalls = new();
        _mockFontService.Setup(x => x.CreateFontIfNecessary(It.IsAny<BmfcSave>()))
            .Callback<BmfcSave>(bmfc => capturedCalls.Add(bmfc));

        TextRuntime textRuntime = new();
        textRuntime.Font = "Arial";
        textRuntime.FontSize = 18;

        // Act
        textRuntime.Text = "[Font=Courier]hello[/Font]";

        // Assert
        capturedCalls.ShouldContain(bmfc => bmfc.FontName == "Courier" && bmfc.FontSize == 18);
    }

    [Fact]
    public void CreateFontIfNecessary_ShouldBeCalledWithBoldAndItalic_WhenBbCodeSetsThose()
    {
        // Arrange
        CustomSetPropertyOnRenderable.FontService = _mockFontService.Object;
        List<BmfcSave> capturedCalls = new();
        _mockFontService.Setup(x => x.CreateFontIfNecessary(It.IsAny<BmfcSave>()))
            .Callback<BmfcSave>(bmfc => capturedCalls.Add(bmfc));

        TextRuntime textRuntime = new();
        textRuntime.Font = "Arial";
        textRuntime.FontSize = 18;

        // Act
        textRuntime.Text = "[IsBold=True][IsItalic=True]hello[/IsItalic][/IsBold]";

        // Assert
        capturedCalls.ShouldContain(bmfc => bmfc.IsBold && bmfc.IsItalic);
    }

    [Fact]
    public void CreateFontIfNecessary_ShouldNotBeCalled_WhenFontServiceIsNull()
    {
        // Arrange
        CustomSetPropertyOnRenderable.FontService = null;

        TextRuntime textRuntime = new();
        textRuntime.Font = "Arial";
        textRuntime.FontSize = 18;

        // Act — should not throw
        textRuntime.Text = "[FontSize=24]hello[/FontSize]";

        // Assert — no exception means success; verify mock was never touched
        _mockFontService.Verify(x => x.CreateFontIfNecessary(It.IsAny<BmfcSave>()), Times.Never);
    }

    [Fact]
    public void CreateFontIfNecessary_ShouldUseAbsoluteFontCacheFolder_ForPathResolution()
    {
        // Arrange
        CustomSetPropertyOnRenderable.FontService = _mockFontService.Object;
        _mockFontService.Setup(x => x.AbsoluteFontCacheFolder).Returns("C:/TestFontCache/");

        TextRuntime textRuntime = new();
        textRuntime.Font = "Arial";
        textRuntime.FontSize = 18;

        // Act
        textRuntime.Text = "[FontSize=24]hello[/FontSize]";

        // Assert — AbsoluteFontCacheFolder was accessed for path resolution
        _mockFontService.Verify(x => x.AbsoluteFontCacheFolder, Times.AtLeastOnce);
    }

    #endregion

    #region UpdateToFontValues

    [Fact]
    public void CreateFontIfNecessary_ShouldBeCalledFromUpdateToFontValues_WhenFontDoesNotExist()
    {
        // Arrange
        CustomSetPropertyOnRenderable.FontService = _mockFontService.Object;
        List<BmfcSave> capturedCalls = new();
        _mockFontService.Setup(x => x.CreateFontIfNecessary(It.IsAny<BmfcSave>()))
            .Callback<BmfcSave>(bmfc => capturedCalls.Add(bmfc));

        TextRuntime textRuntime = new();
        textRuntime.Font = "Arial";

        // Act — setting FontSize triggers UpdateToFontValues
        textRuntime.FontSize = 36;

        // Assert
        capturedCalls.ShouldContain(bmfc => bmfc.FontSize == 36 && bmfc.FontName == "Arial");
    }

    [Fact]
    public void CreateFontIfNecessary_ShouldNotBeCalledFromUpdateToFontValues_WhenFontServiceIsNull()
    {
        // Arrange
        CustomSetPropertyOnRenderable.FontService = null;

        TextRuntime textRuntime = new();
        textRuntime.Font = "Arial";

        // Act — should not throw
        textRuntime.FontSize = 36;

        // Assert — no exception means success; verify mock was never touched
        _mockFontService.Verify(x => x.CreateFontIfNecessary(It.IsAny<BmfcSave>()), Times.Never);
    }

    [Fact]
    public void CreateFontIfNecessary_ShouldPassBoldAndItalicFromUpdateToFontValues()
    {
        // Arrange — use FontSize 24 to avoid the stubbed Arial-18 embedded resources
        CustomSetPropertyOnRenderable.FontService = _mockFontService.Object;
        List<BmfcSave> capturedCalls = new();
        _mockFontService.Setup(x => x.CreateFontIfNecessary(It.IsAny<BmfcSave>()))
            .Callback<BmfcSave>(bmfc => capturedCalls.Add(bmfc));

        TextRuntime textRuntime = new();
        textRuntime.Font = "Arial";
        textRuntime.IsBold = true;
        textRuntime.IsItalic = true;

        // Act — setting FontSize triggers UpdateToFontValues with the current bold/italic values
        textRuntime.FontSize = 24;

        // Assert
        capturedCalls.ShouldContain(bmfc => bmfc.IsBold && bmfc.IsItalic && bmfc.FontSize == 24);
    }

    [Fact]
    public void CreateFontIfNecessary_ShouldPassOutlineAndSmoothing_FromUpdateToFontValues()
    {
        // Arrange — use FontSize 24 to avoid the stubbed Arial-18 embedded resources
        CustomSetPropertyOnRenderable.FontService = _mockFontService.Object;
        List<BmfcSave> capturedCalls = new();
        _mockFontService.Setup(x => x.CreateFontIfNecessary(It.IsAny<BmfcSave>()))
            .Callback<BmfcSave>(bmfc => capturedCalls.Add(bmfc));

        TextRuntime textRuntime = new();
        textRuntime.Font = "Arial";
        textRuntime.OutlineThickness = 2;
        textRuntime.UseFontSmoothing = false;

        // Act — setting FontSize triggers UpdateToFontValues with the current outline/smoothing values
        textRuntime.FontSize = 24;

        // Assert
        capturedCalls.ShouldContain(bmfc => bmfc.OutlineThickness == 2 && bmfc.UseSmoothing == false);
    }

    #endregion
}
