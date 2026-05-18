using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using ToolsUtilities;

namespace Gum.Plugins.ImportPlugin.Manager;

public interface IImportLogic
{
    ScreenSave? ImportScreen(FilePath filePath, string? desiredDirectory = null, bool saveProject = true);
    ComponentSave? ImportComponent(FilePath filePath, string? desiredDirectory = null, bool saveProject = true);
    BehaviorSave ImportBehavior(FilePath filePath, string? desiredDirectory = null, bool saveProject = false);

    /// <summary>
    /// Registers an already-loaded, fully-mutated <see cref="ComponentSave"/> with the project
    /// and writes it once to its canonical destination path. Bypasses the file → deserialize
    /// round-trip required by the FilePath overload. The destination path is derived from the
    /// component's <see cref="ElementSave.Name"/>, so callers must set Name to the final
    /// (possibly subfolder-qualified) target before calling.
    /// </summary>
    ComponentSave? ImportComponent(ComponentSave component, bool saveProject = true);

    /// <summary>
    /// Registers an already-loaded, fully-mutated <see cref="ScreenSave"/> with the project
    /// and writes it once to its canonical destination path. See
    /// <see cref="ImportComponent(ComponentSave, bool)"/> for semantics.
    /// </summary>
    ScreenSave? ImportScreen(ScreenSave screen, bool saveProject = true);
}
