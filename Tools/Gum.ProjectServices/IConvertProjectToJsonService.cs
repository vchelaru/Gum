using Gum.DataTypes;

namespace Gum.ProjectServices;

/// <summary>
/// Converts an already-loaded Gum project — and every Screen/Component/StandardElement/Behavior/
/// animation file it references — from XML to its JSON counterpart (<c>.gumj</c>/<c>.gusj</c>/
/// <c>.gucj</c>/<c>.gutj</c>/<c>.behj</c>/<c>.ganj</c>). JSON is an AOT-safe alternative to XML
/// (<c>XmlSerializer</c> is not Native AOT-safe); conversion is explicit opt-in, never automatic.
/// Non-destructive: writes new JSON sibling files and never modifies or deletes the existing XML.
/// </summary>
public interface IConvertProjectToJsonService
{
    /// <summary>
    /// Converts <paramref name="project"/> to JSON. <paramref name="project"/> must already be
    /// loaded from (and saved to) a <c>.gumx</c> file — its <see cref="GumProjectSave.FullFileName"/>
    /// must be set and must not already be JSON.
    /// </summary>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown when the project has no file path, or is already in JSON format.
    /// </exception>
    ConvertProjectToJsonResult ConvertToJson(GumProjectSave project);
}

/// <summary>Result of a <see cref="IConvertProjectToJsonService.ConvertToJson"/> run.</summary>
public class ConvertProjectToJsonResult
{
    /// <summary>The <c>.gumj</c> path that was written — a sibling of the source <c>.gumx</c>.</summary>
    public string ProjectFilePath { get; set; } = string.Empty;

    /// <summary>Number of Screens converted (<c>.gusj</c> files written).</summary>
    public int ScreenCount { get; set; }

    /// <summary>Number of Components converted (<c>.gucj</c> files written).</summary>
    public int ComponentCount { get; set; }

    /// <summary>Number of StandardElements converted (<c>.gutj</c> files written).</summary>
    public int StandardElementCount { get; set; }

    /// <summary>Number of Behaviors converted (<c>.behj</c> files written).</summary>
    public int BehaviorCount { get; set; }

    /// <summary>Number of element animation files converted (<c>.ganj</c> files written).</summary>
    public int AnimationCount { get; set; }

    /// <summary>Total number of JSON files written, including the project file itself.</summary>
    public int TotalFileCount =>
        1 + ScreenCount + ComponentCount + StandardElementCount + BehaviorCount + AnimationCount;
}
