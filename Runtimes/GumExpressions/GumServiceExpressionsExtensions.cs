using System;
using Gum.Expressions;
using RenderingLibrary;

namespace Gum;

/// <summary>
/// Enables Roslyn-based expression evaluation for variable references on any Gum host.
/// </summary>
public static class GumServiceExpressionsExtensions
{
    /// <summary>
    /// Enables expressions in variable references (for example <c>Width + 10</c>). Call once at startup,
    /// after <c>Initialize</c>. Without it, variable references fall back to simple dot-path lookups.
    /// Replaces <see cref="GumExpressionService.Initialize"/>. Generic over the service type so the host's own
    /// <c>GumService</c> flows through a chain.
    /// </summary>
    /// <returns><paramref name="gumService"/>, so calls chain.</returns>
    public static T UseExpressions<T>(this T gumService) where T : IGumService
    {
        if (gumService == null)
        {
            throw new ArgumentNullException(nameof(gumService));
        }

        GumExpressionService.Initialize();
        return gumService;
    }
}
