namespace Gum.ProjectServices;

/// <summary>
/// Resolves a Forms theme project's behaviors (following <c>BehaviorReference.SourcePath</c>
/// links and <c>DefaultImplementationOverride</c> values) and writes each one as a flat,
/// standalone .behx file. Used at build time to stage a theme's true effective behaviors for
/// consumers (Add Forms dialog, Import from .gumx) that expect a plain per-theme folder rather
/// than resolving links themselves. See issue #4070.
/// </summary>
public interface IFormsThemeBehaviorStagingService
{
    /// <summary>
    /// Loads <paramref name="projectGumxPath"/>, resolves every behavior it references, and
    /// writes each one to <paramref name="destinationBehaviorsDirectory"/> as <c>{Name}.behx</c>.
    /// </summary>
    /// <param name="projectGumxPath">Path to the theme's source GumProject.gumx.</param>
    /// <param name="destinationBehaviorsDirectory">Directory to write resolved .behx files into; created if missing.</param>
    /// <returns>The number of behaviors staged.</returns>
    int Stage(string projectGumxPath, string destinationBehaviorsDirectory);
}
