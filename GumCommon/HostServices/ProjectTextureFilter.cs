using System;
using Gum.DataTypes;

// Companion to Gum.GumService / GumServiceSkiaBase (issue #5231): how a project's TextureFilter
// setting maps to point vs linear filtering, so every backend reads it the same way.
namespace Gum;

public static class ProjectTextureFilter
{
    /// <summary>
    /// Whether <paramref name="gumProject"/> asks for linear texture filtering. <c>"Linear"</c> is the
    /// string the editor stores for linear filtering; any other value (including null) means point.
    /// </summary>
    public static bool UsesLinearFiltering(GumProjectSave? gumProject) =>
        string.Equals(gumProject?.TextureFilter, "Linear", StringComparison.Ordinal);
}
