namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>User-visible warnings shown when the project's font generator is bmfont.exe.</summary>
public static class FontGeneratorWarningText
{
    /// <summary>Where the user switches the project to KernSmith.</summary>
    public const string SwitchToKernSmith = "Switch to KernSmith in Edit > Settings... > Fonts.";

    /// <summary>Detail text under a Font that is a file.</summary>
    public const string CannotGenerateFromFontFiles = "bmfont cannot generate from font files. " + SwitchToKernSmith;

    /// <summary>Detail text under Text's HasDropshadow.</summary>
    public const string CannotBakeDropshadows = "bmfont cannot bake dropshadows into the font atlas. " + SwitchToKernSmith;
}
