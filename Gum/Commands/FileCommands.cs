using System;
using Gum.ToolStates;
using Gum.Managers;
using Gum.Wireframe;
using Gum.DataTypes;
using System.Windows.Forms;
using Gum.DataTypes.Behaviors;
using Gum.Undo;
using Gum.Plugins;
using ToolsUtilities;
using Gum.Logic.FileWatch;

namespace Gum.Commands
{
    public class FileCommands
    {
        MainWindow mainWindow;
        public void Initialize(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
        }

        /// <summary>
        /// Saves the current Behavior or Element
        /// </summary>
        public void TryAutoSaveCurrentObject()
        {
            if(SelectedState.Self.SelectedBehavior != null)
            {
                TryAutoSaveBehavior(SelectedState.Self.SelectedBehavior);
            }
            else
            {
                TryAutoSaveCurrentElement();
            }
        }

        public void TryAutoSaveCurrentElement()
        {
            TryAutoSaveElement(SelectedState.Self.SelectedElement);
        }


        public void TryAutoSaveElement(ElementSave elementSave)
        {
            if (ProjectManager.Self.GeneralSettingsFile.AutoSave && elementSave != null)
            {
                SaveElement(elementSave);
            }
        }

        public void TryAutoSaveBehavior(BehaviorSave behavior)
        {
            if(ProjectManager.Self.GeneralSettingsFile.AutoSave && behavior != null)
            {
                ForceSaveBehavior(behavior);
            }
        }

        public void TryAutoSaveObject(object objectToSave)
        {
            if(objectToSave is ElementSave elementSave)
            {
                TryAutoSaveElement(elementSave);
            }
            if(objectToSave is BehaviorSave behaviorSave)
            {
                TryAutoSaveBehavior(behaviorSave);
            }
        }


        internal void NewProject()
        {
            SelectedState.Self.SelectedElement = null;
            SelectedState.Self.SelectedInstance = null;
            SelectedState.Self.SelectedBehavior = null;
            SelectedState.Self.SelectedStateCategorySave = null;
            SelectedState.Self.SelectedStateSave = null;

            ProjectManager.Self.CreateNewProject();

            GumCommands.Self.GuiCommands.RefreshElementTreeView();
            GumCommands.Self.GuiCommands.RefreshStateTreeView();
            GumCommands.Self.GuiCommands.RefreshVariables();
            WireframeObjectManager.Self.RefreshAll(true);
        }

        /// <summary>
        /// Attempts to save the project (gumx) and optionally all contained elements.
        /// Does not save if auto save is turned off, or if there were errors loading the
        /// project.
        /// </summary>
        /// <param name="forceSaveContainedElements">Whether to also save all elements.</param>
        /// <returns>Whether a save occurred.</returns>
        public bool TryAutoSaveProject(bool forceSaveContainedElements = false)
        {
            if (ProjectManager.Self.GeneralSettingsFile.AutoSave && !ProjectManager.Self.HaveErrorsOccurredLoadingProject)
            {
                ForceSaveProject(forceSaveContainedElements);
                return true;
            }
            return false;
        }

        internal void ForceSaveProject(bool forceSaveContainedElements = false)
        {
            if (!ProjectManager.Self.HaveErrorsOccurredLoadingProject)
            {
                ProjectManager.Self.SaveProject(forceSaveContainedElements);
                OutputManager.Self.AddOutput("Saved Gum project to " + ProjectState.Self.GumProjectSave.FullFileName);
            }
            else
            {
                MessageBox.Show("Cannot save project because of earlier errors");
            }
        }

        internal void ForceSaveElement(ElementSave element)
        {
            SaveElement(element);
        }

        private void SaveElement(ElementSave elementSave)
        {
            if (elementSave.IsSourceFileMissing)
            {
                MessageBox.Show("Cannot save " + elementSave + " because its source file is missing");
            }
            else
            {
                bool succeeded = true;

                // April 10, 2023
                // This is potentially slow, dishonest (SaveElement should only save the element), and prevents multi-select edit
                // from properly creating a single undo state:
                //UndoManager.Self.RecordUndo();

                bool doesProjectNeedToSave = false;
                bool shouldSave = ProjectManager.Self.AskUserForProjectNameIfNecessary(out doesProjectNeedToSave);

                if (doesProjectNeedToSave)
                {
                    ProjectManager.Self.SaveProject();
                }

                if (shouldSave)
                {
                    PluginManager.Self.BeforeElementSave(elementSave);

                    var fileName = elementSave.GetFullPathXmlFile();

                    // if it's readonly, let's warn the user
                    bool isReadOnly = ProjectManager.IsFileReadOnly(fileName.FullPath);

                    if (isReadOnly)
                    {
                        ProjectManager.ShowReadOnlyDialog(fileName.FullPath);
                    }
                    else
                    {
                        FileWatchLogic.Self.IgnoreNextChangeOn(fileName.FullPath);

                        const int maxNumberOfTries = 5;
                        const int msBetweenSaves = 100;
                        int numberOfTimesTried = 0;

                        succeeded = false;
                        Exception exception = null;

                        while (numberOfTimesTried < maxNumberOfTries)
                        {
                            try
                            {
                                elementSave.Save(fileName.FullPath);
                                succeeded = true;
                                break;
                            }
                            catch (Exception e)
                            {
                                exception = e;
                                System.Threading.Thread.Sleep(msBetweenSaves);
                                numberOfTimesTried++;
                            }
                        }


                        if (succeeded == false)
                        {
                            MessageBox.Show("Unknown error trying to save the file\n\n" + fileName + "\n\n" + exception.ToString());
                            succeeded = false;
                        }
                    }
                    if (succeeded)
                    {
                        OutputManager.Self.AddOutput("Saved " + elementSave + " to " + fileName);
                        PluginManager.Self.AfterElementSave(elementSave);
                    }
                }

                PluginManager.Self.Export(elementSave);
            }
        }

        public void LoadProject(string fileName)
        {
            ProjectManager.Self.LoadProject(fileName);
        }

        public FilePath GetFullFileName(ElementSave element)
        {
            return element.GetFullPathXmlFile();
        }


        internal void LoadLocalizationFile()
        {
            LocalizationManager.Clear();

            if (!string.IsNullOrEmpty(GumState.Self.ProjectState.GumProjectSave.LocalizationFile))
            {
                FilePath file = GumState.Self.ProjectState.ProjectDirectory + GumState.Self.ProjectState.GumProjectSave.LocalizationFile;

                if (file.Exists())
                {
                    try
                    {
                        LocalizationManager.AddDatabase(file.FullPath, ',');
                        LocalizationManager.CurrentLanguage = GumState.Self.ProjectState.GumProjectSave.CurrentLanguageIndex;
                    }
                    catch (Exception e)
                    {
                        // This can happen if the CSV has duplicate entries
                        GumCommands.Self.GuiCommands.ShowMessage($"Error loading CSV {file.FullPath}\n\n{e}");
                    }
                }
            }
        }

        private void ForceSaveBehavior(BehaviorSave behavior)
        {
            if (behavior.IsSourceFileMissing)
            {
                MessageBox.Show("Cannot save " + behavior + " because its source file is missing");
            }
            else
            {
                bool succeeded = true;

                UndoManager.Self.RecordUndo();

                bool doesProjectNeedToSave = false;
                bool shouldSave = ProjectManager.Self.AskUserForProjectNameIfNecessary(out doesProjectNeedToSave);

                if (doesProjectNeedToSave)
                {
                    ProjectManager.Self.SaveProject();
                }

                if (shouldSave)
                {
                    //PluginManager.Self.BeforeBehaviorSave(behavior);

                    string fileName = GetFullPathXmlFile( behavior).FullPath;
                    FileWatchLogic.Self.IgnoreNextChangeOn(fileName);
                    // if it's readonly, let's warn the user
                    bool isReadOnly = ProjectManager.IsFileReadOnly(fileName);

                    if (isReadOnly)
                    {
                        ProjectManager.ShowReadOnlyDialog(fileName);
                    }
                    else
                    {
                        const int maxNumberOfTries = 5;
                        const int msBetweenSaves = 100;
                        int numberOfTimesTried = 0;

                        succeeded = false;
                        Exception exception = null;

                        while (numberOfTimesTried < maxNumberOfTries)
                        {
                            try
                            {
                                FileManager.XmlSerialize(behavior.GetType(), behavior, fileName);
                                
                                succeeded = true;
                                break;
                            }
                            catch (Exception e)
                            {
                                exception = e;
                                System.Threading.Thread.Sleep(msBetweenSaves);
                                numberOfTimesTried++;
                            }
                        }


                        if (succeeded == false)
                        {
                            MessageBox.Show("Unknown error trying to save the file\n\n" + fileName + "\n\n" + exception.ToString());
                            succeeded = false;
                        }
                    }
                    if (succeeded)
                    {
                        OutputManager.Self.AddOutput("Saved " + behavior + " to " + fileName);
                        //PluginManager.Self.AfterBehaviorSave(behavior);
                    }
                }

                //PluginManager.Self.Export(elementSave);
            }

        }

        public FilePath GetFullPathXmlFile(BehaviorSave behaviorSave)
        {
            return GetFullPathXmlFile(behaviorSave, behaviorSave.Name);
        }

        static string GetFullPathXmlFile(BehaviorSave behaviorSave, string behaviorName)
        {
            if (string.IsNullOrEmpty(ProjectManager.Self.GumProjectSave.FullFileName))
            {
                return null;
            }

            string directory = FileManager.GetDirectory(ProjectManager.Self.GumProjectSave.FullFileName);

            return directory + BehaviorReference.Subfolder + "\\" + behaviorName + "." + BehaviorReference.Extension;
        }

        public void Exit()
        {
            mainWindow.Close();
        }

        internal void SaveGeneralSettings()
        {
            var settings = ProjectManager.Self.GeneralSettingsFile;
            settings.Save();
        }

        public void SaveIfDiffers(FilePath filePath, string contents)
        {
            if (filePath.Exists() == false)
            {
                FileManager.SaveText(contents, filePath.FullPath);
            }
            else
            {
                string existingContents = FileManager.FromFileText(filePath.FullPath);
                if (existingContents != contents)
                {
                    FileManager.SaveText(contents, filePath.FullPath);
                }
            }
        }
    }
}
