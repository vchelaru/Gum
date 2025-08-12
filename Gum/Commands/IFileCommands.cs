﻿using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace Gum.Commands;
public interface IFileCommands
{
    void TryAutoSaveCurrentObject();

    void TryAutoSaveCurrentElement();

    void TryAutoSaveElement(ElementSave elementSave);

    void TryAutoSaveBehavior(BehaviorSave behavior);

    void TryAutoSaveObject(object objectToSave);

    void NewProject();

    bool TryAutoSaveProject(bool forceSaveContainedElements = false);

    void ForceSaveProject(bool forceSaveContainedElements = false);

    void ForceSaveElement(ElementSave element);


    void LoadProject(string fileName);

    FilePath GetFullFileName(ElementSave element);

    void LoadLocalizationFile();

    FilePath GetFullPathXmlFile(BehaviorSave behaviorSave);

    void SaveGeneralSettings();

    void SaveIfDiffers(FilePath filePath, string contents);
}
