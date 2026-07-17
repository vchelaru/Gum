namespace Gum.Managers;

/// <summary>
/// Absolute folder paths for the loaded project's Screens/Components/Standards/Behaviors
/// subfolders. Extracted from the concrete <c>FileLocations</c> (ADR-0005 Phase 3) so dialog
/// ViewModels that only need these paths can move into the headless Gum.Presentation assembly
/// without depending on the concrete, tool-project-only type. The live implementation is
/// <c>FileLocations</c>, bridged via DI to the same singleton.
/// </summary>
public interface IFileLocations
{
    string ScreensFolder { get; }
    string ComponentsFolder { get; }
    string StandardsFolder { get; }
    string BehaviorsFolder { get; }
}
