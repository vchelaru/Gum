using System.Runtime.CompilerServices;

// KernSmith.RaylibGum reuses ContentLoader.BuildFont (internal) to assemble a Raylib_cs.Font from a
// KernSmith-rasterized atlas, instead of duplicating the glyph-mapping logic. RaylibGum sets
// GenerateAssemblyInfo=false, so this attribute is declared in source rather than via the csproj
// <InternalsVisibleTo> item (which the SDK only emits when assembly-info generation is enabled).
[assembly: InternalsVisibleTo("KernSmith.RaylibGum")]
