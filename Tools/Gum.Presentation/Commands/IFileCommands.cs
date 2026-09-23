using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace Gum.Commands;
public interface IFileCommands
{
    FilePath? ProjectDirectory { get; }

    void DeleteDirectory(FilePath filePath);

    /// <summary>
    /// Deletes everything inside <paramref name="directory"/> but leaves the directory itself in place.
    /// </summary>
    void ClearDirectoryContents(FilePath directory);

    /// <summary>
    /// Moves a file to the OS recycle bin/trash rather than permanently deleting it.
    /// </summary>
    void MoveToRecycleBin(FilePath filePath);

    /// <summary>
    /// Moves several files to the OS recycle bin/trash in as few OS calls as the platform allows,
    /// so a large batch doesn't play the trash sound or spawn a process once per file.
    /// </summary>
    void MoveToRecycleBin(IReadOnlyList<FilePath> filePaths);

    string[] GetFiles(string path);

    string[] GetFiles(string path, string searchPattern, SearchOption searchOption);

    string ReadAllText(string path);

    void MoveDirectory(string source, string destination);

    void SaveEmbeddedResource(Assembly assembly, string resourceName, string targetFileName);

    /// <summary>
    /// Saves the current Screen, Component, Standard, or Behavior
    /// </summary>
    void TryAutoSaveCurrentObject();

    void TryAutoSaveCurrentElement();

    void TryAutoSaveElement(ElementSave elementSave);

    void TryAutoSaveBehavior(BehaviorSave behavior);

    void TryAutoSaveObject(object objectToSave);

    Task NewProjectAsync();

    bool TryAutoSaveProject(bool forceSaveContainedElements = false);

    void ForceSaveProject(bool forceSaveContainedElements = false);

    void ForceSaveElement(ElementSave element);


    Task LoadProjectAsync(string fileName);

    FilePath GetFullFileName(ElementSave element);

    /// <summary>
    /// Gets the full path to an element's XML file as though it were still named
    /// <paramref name="elementName"/> — used by rename to locate the file at its old name.
    /// </summary>
    FilePath? GetFullPathXmlFile(ElementSave element, string elementName);

    event Action? LocalizationLoaded;
    void LoadLocalizationFile();

    FilePath GetFullPathXmlFile(BehaviorSave behaviorSave);

    void SaveGeneralSettings();

    void SaveIfDiffers(FilePath filePath, string contents);
}
