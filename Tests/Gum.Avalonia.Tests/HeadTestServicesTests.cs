using Gum.ProjectServices.FontGeneration;
using Microsoft.Extensions.DependencyInjection;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using ToolsUtilities;

namespace Gum.Avalonia.Tests;

public class HeadTestServicesTests
{
    [Fact]
    public void FontFileGenerator_ShouldCompleteWithoutGenerating()
    {
        IFontFileGenerator generator = TestAppBuilder.Services.GetRequiredService<IFontFileGenerator>();
        string outputFntPath = Path.Combine(Path.GetTempPath(), "GumAvaloniaTests", Guid.NewGuid().ToString("N"), "Font18Arial.fnt");
        BmfcSave bmfcSave = new BmfcSave { FontName = "Arial", FontSize = 18 };

        Task<GeneralResponse> generation = generator.GenerateFont(bmfcSave, outputFntPath, createTask: true);

        generation.IsCompletedSuccessfully.ShouldBeTrue();
        File.Exists(outputFntPath).ShouldBeFalse();
    }
}
