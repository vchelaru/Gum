using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.ProjectServices.FontGeneration;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using System.Runtime.InteropServices;

namespace Gum.ProjectServices.Tests;

public class HeadlessFontGenerationServiceTests : BaseTestClass
{
    private readonly HeadlessFontGenerationService _sut;

    public HeadlessFontGenerationServiceTests()
    {
        _sut = new HeadlessFontGenerationService();
    }

    #region TryGetBmfcSaveFor

    [Fact]
    public void TryGetBmfcSaveFor_ShouldReturnNull_WhenFontNotSet()
    {
        StateSave state = new StateSave();
        state.Variables.Add(new VariableSave { Name = "FontSize", Value = 18 });

        BmfcSave? result = _sut.TryGetBmfcSaveFor(null, state, fontRanges: "", spacingHorizontal: 1, spacingVertical: 1, forcedValues: null);

        result.ShouldBeNull();
    }

    [Fact]
    public void TryGetBmfcSaveFor_ShouldReturnNull_WhenFontSizeNotSet()
    {
        StateSave state = new StateSave();
        state.Variables.Add(new VariableSave { Name = "Font", Value = "Arial" });

        BmfcSave? result = _sut.TryGetBmfcSaveFor(null, state, fontRanges: "", spacingHorizontal: 1, spacingVertical: 1, forcedValues: null);

        result.ShouldBeNull();
    }

    [Fact]
    public void TryGetBmfcSaveFor_ShouldReturnBmfcSave_WhenFontAndSizeAreSet()
    {
        StateSave state = new StateSave();
        state.Variables.Add(new VariableSave { Name = "Font", Value = "Arial" });
        state.Variables.Add(new VariableSave { Name = "FontSize", Value = 24 });

        BmfcSave? result = _sut.TryGetBmfcSaveFor(null, state, fontRanges: "32-126", spacingHorizontal: 2, spacingVertical: 3, forcedValues: null);

        result.ShouldNotBeNull();
        result.FontName.ShouldBe("Arial");
        result.FontSize.ShouldBe(24);
        result.Ranges.ShouldBe("32-126");
        result.SpacingHorizontal.ShouldBe(2);
        result.SpacingVertical.ShouldBe(3);
    }

    [Fact]
    public void TryGetBmfcSaveFor_ShouldApplyDefaults_WhenOptionalPropertiesNotSet()
    {
        StateSave state = new StateSave();
        state.Variables.Add(new VariableSave { Name = "Font", Value = "Comic Sans MS" });
        state.Variables.Add(new VariableSave { Name = "FontSize", Value = 12 });

        BmfcSave? result = _sut.TryGetBmfcSaveFor(null, state, fontRanges: "", spacingHorizontal: 1, spacingVertical: 1, forcedValues: null);

        result.ShouldNotBeNull();
        result.OutlineThickness.ShouldBe(0);
        result.UseSmoothing.ShouldBe(true);
        result.IsItalic.ShouldBe(false);
        result.IsBold.ShouldBe(false);
    }

    [Fact]
    public void TryGetBmfcSaveFor_ShouldReadOutlineItalicBold_WhenSetInState()
    {
        StateSave state = new StateSave();
        state.Variables.Add(new VariableSave { Name = "Font", Value = "Times New Roman" });
        state.Variables.Add(new VariableSave { Name = "FontSize", Value = 32 });
        state.Variables.Add(new VariableSave { Name = "OutlineThickness", Value = 2 });
        state.Variables.Add(new VariableSave { Name = "IsItalic", Value = true });
        state.Variables.Add(new VariableSave { Name = "IsBold", Value = true });
        state.Variables.Add(new VariableSave { Name = "UseFontSmoothing", Value = false });

        BmfcSave? result = _sut.TryGetBmfcSaveFor(null, state, fontRanges: "", spacingHorizontal: 1, spacingVertical: 1, forcedValues: null);

        result.ShouldNotBeNull();
        result.OutlineThickness.ShouldBe(2);
        result.IsItalic.ShouldBe(true);
        result.IsBold.ShouldBe(true);
        result.UseSmoothing.ShouldBe(false);
    }

    [Fact]
    public void TryGetBmfcSaveFor_ShouldUsePrefixedVariables_WhenInstanceProvided()
    {
        InstanceSave instance = new InstanceSave { Name = "MyLabel", BaseType = "Text" };

        StateSave state = new StateSave();
        state.Variables.Add(new VariableSave { Name = "MyLabel.Font", Value = "Verdana" });
        state.Variables.Add(new VariableSave { Name = "MyLabel.FontSize", Value = 16 });

        BmfcSave? result = _sut.TryGetBmfcSaveFor(instance, state, fontRanges: "", spacingHorizontal: 1, spacingVertical: 1, forcedValues: null);

        result.ShouldNotBeNull();
        result.FontName.ShouldBe("Verdana");
        result.FontSize.ShouldBe(16);
    }

    [Fact]
    public void TryGetBmfcSaveFor_ForcedValues_ShouldOverrideStateValues()
    {
        StateSave state = new StateSave();
        state.Variables.Add(new VariableSave { Name = "Font", Value = "Arial" });
        state.Variables.Add(new VariableSave { Name = "FontSize", Value = 12 });

        StateSave forced = new StateSave();
        forced.Variables.Add(new VariableSave { Name = "Font", Value = "Impact" });
        forced.Variables.Add(new VariableSave { Name = "FontSize", Value = 48 });

        BmfcSave? result = _sut.TryGetBmfcSaveFor(null, state, fontRanges: "", spacingHorizontal: 1, spacingVertical: 1, forcedValues: forced);

        result.ShouldNotBeNull();
        result.FontName.ShouldBe("Impact");
        result.FontSize.ShouldBe(48);
    }

    #endregion

    #region Windows gate

    [Fact]
    public async Task CreateAllMissingFontFiles_ShouldThrowPlatformNotSupportedException_WhenNotWindows()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        GumProjectSave project = new GumProjectSave();

        await Should.ThrowAsync<PlatformNotSupportedException>(
            () => _sut.CreateAllMissingFontFiles(project, projectDirectory: "/tmp/test"));
    }

    [Fact]
    public void ReactToFontValueSet_ShouldThrowPlatformNotSupportedException_WhenNotWindows()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        GumProjectSave project = new GumProjectSave();
        InstanceSave instance = new InstanceSave { Name = "Label", BaseType = "Text" };
        StateSave state = new StateSave();

        Should.Throw<PlatformNotSupportedException>(
            () => _sut.ReactToFontValueSet(instance, project, state, new StateSave(), projectDirectory: "/tmp/test"));
    }

    #endregion
}
