using System;
using System.Collections.Generic;
using Gum.DataTypes;
using Gum.Managers;
using System.Windows.Forms;
using Gum.Plugins;
using Gum.Wireframe;
using Gum.Settings;
using Gum.Undo;
using ToolsUtilities;
using System.IO;
using CommonFormsAndControls.Forms;
using System.Diagnostics;
using Gum.ToolStates;

namespace Gum
{
    public class ProjectManager
    {
        #region Fields

        GumProjectSave mGumProjectSave;

        static ProjectManager mSelf;

        bool mHaveErrorsOccurred = false;

        #endregion

        #region Properties

        public static ProjectManager Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new ProjectManager();
                }
                return mSelf;
            }
        }

        public GumProjectSave GumProjectSave
        {
            get
            {
                return mGumProjectSave;
            }
        }

        public GeneralSettingsFile GeneralSettingsFile
        {
            get;
            private set;
        }

        public bool HaveErrorsOccurred
        {
            get
            {
                return mHaveErrorsOccurred;
            }
        }
        #endregion

        #region Events

        public event Action RecentFilesUpdated;

        #endregion

        #region Methods


        private ProjectManager()
        {

        }

        public void Initialize()
        {
            GeneralSettingsFile = GeneralSettingsFile.LoadOrCreateNew();


            CommandLineManager.Self.ReadCommandLine();

            if (!string.IsNullOrEmpty(CommandLineManager.Self.Glux))
            {
                GumCommands.Self.FileCommands.LoadProject(CommandLineManager.Self.Glux);

                if (!string.IsNullOrEmpty(CommandLineManager.Self.ElementName))
                {
                    SelectedState.Self.SelectedElement = ObjectFinder.Self.GetElementSave(CommandLineManager.Self.ElementName);
                }
                
            }
            else if (!string.IsNullOrEmpty(GeneralSettingsFile.LastProject))
            {
                LoadProject(GeneralSettingsFile.LastProject);
            }
            else
            {
                CreateNewProject();
            }



        }

        public void CreateNewProject()
        {
            mGumProjectSave = new GumProjectSave();
            ObjectFinder.Self.GumProjectSave = mGumProjectSave;

            StandardElementsManager.Self.PopulateProjectWithDefaultStandards(mGumProjectSave);
            // Now that a new project is created, refresh the UI!
            ElementTreeViewManager.Self.RefreshUI();
        }

        public bool LoadProject()
        {
            var openFileDialog = new OpenFileDialog();

            openFileDialog.Filter = "Gum Project (*.gumx)|*.gumx";
            openFileDialog.Title = "Select project to load";

            DialogResult result = openFileDialog.ShowDialog();

                

            if (result == DialogResult.OK)
            {
                string fileName = openFileDialog.FileName;

                SelectedState.Self.SelectedInstance = null;
                SelectedState.Self.SelectedElement = null;

                LoadProject(fileName);

                return true;
            }


            return false;
        }

        // made public so that File commands can access this function
        public void LoadProject(string fileName)
        {
            string errors;

            mGumProjectSave = GumProjectSave.Load(fileName, out errors);

            if (!string.IsNullOrEmpty(errors))
            {
                MessageBox.Show("Errors loading " + fileName + "\n\n" + errors);
                
                // If the file doesn't exist, that's okay we will let the user still work - it's not like they can overwrite a file that doesn't exist
                if (System.IO.File.Exists(fileName))
                {
                    mHaveErrorsOccurred = true;
                }

                // We used to not load the project, but maybe we still should, just disable autosaving
                //
                //mGumProjectSave = new GumProjectSave();
            }
            else
            {
                mHaveErrorsOccurred = false;
            }

            ObjectFinder.Self.GumProjectSave = mGumProjectSave;

            if (mGumProjectSave != null)
            {
                mGumProjectSave.Initialize();


                RecreateMissingStandardElements();

                mGumProjectSave.AddNewStandardElementTypes();
                mGumProjectSave.FixStandardVariables();

                FileManager.RelativeDirectory = FileManager.GetDirectory(fileName);
                mGumProjectSave.RemoveDuplicateVariables();
            }

            // Now that a new project is loaded, refresh the UI!
            ElementTreeViewManager.Self.RefreshUI();
            // And the guides
            WireframeObjectManager.Self.UpdateGuides();

            PluginManager.Self.ProjectLoad(mGumProjectSave);

            GeneralSettingsFile.AddToRecentFilesIfNew(fileName);

            if (GeneralSettingsFile.LastProject != fileName)
            {
                GeneralSettingsFile.LastProject = fileName;
                GeneralSettingsFile.Save();
            }

            if (RecentFilesUpdated != null)
            {
                RecentFilesUpdated();
            }

        }

        private void RecreateMissingStandardElements()
        {
            List<StandardElementSave> missingElements = new List<StandardElementSave>();
            foreach (var element in mGumProjectSave.StandardElements)
            {
                if (element.IsSourceFileMissing)
                {
                    missingElements.Add(element);

                }
            }



            foreach (var element in missingElements)
            {
                var result = MessageBox.Show(
                    "The following standard is missing: " + element.Name + "  Recreate it?", "Recreate " + element.Name + "?", MessageBoxButtons.OKCancel);

                if (result == DialogResult.OK)
                {
                    mGumProjectSave.StandardElements.RemoveAll(item => item.Name == element.Name);
                    mGumProjectSave.StandardElementReferences.RemoveAll(item => item.Name == element.Name);

                    var newElement = StandardElementsManager.Self.AddStandardElementSaveInstance(mGumProjectSave, element.Name);

                    string gumProjectDirectory = FileManager.GetDirectory(mGumProjectSave.FullFileName);

                    mGumProjectSave.SaveStandardElements(gumProjectDirectory);
                }
            }
        }

        internal void SaveProject(bool forceSaveContainedElements = false)
        {
            bool succeeded = false;

            if (mHaveErrorsOccurred)
            {
                MessageBox.Show("Can't save project because errors occurred when the project was last loaded");
            }
            else
            {
                bool isNewProject;
                bool shouldSave = AskUserForProjectNameIfNecessary(out isNewProject);

                if (shouldSave)
                {
                    try
                    {
                        bool saveContainedElements = isNewProject || forceSaveContainedElements;

                        GumProjectSave.Save(GumProjectSave.FullFileName, saveContainedElements);
                        succeeded = true;
                    }
                    catch(UnauthorizedAccessException exception)
                    {
                        string fileName = GetFileNameFromException(exception);

                        bool isReadOnly = GetIfFileIsReadOnly(fileName);

                        if (isReadOnly)
                        {
                            ShowReadOnlyDialog(fileName);
                        }
                        else
                        {
                            MessageBox.Show("Unknown error trying to save the file\n\n" +
                                fileName + "\n\n" + exception.ToString());
                        }
                    }
                    // This may be the first time the file is being saved.  If so, we should make it relative
                    FileManager.RelativeDirectory = FileManager.GetDirectory(GumProjectSave.FullFileName);

                    if (succeeded)
                    {
                        PluginManager.Self.ProjectSave(GumProjectSave);
                    }
                }
            }
        }

        private static string GetFileNameFromException(UnauthorizedAccessException exception)
        {
            string fileName = exception.Message;

            int start = exception.Message.IndexOf('\'') + 1;
            int end = exception.Message.IndexOf('\'', start);

            return exception.Message.Substring(start, end - start);
        }

        public string MakeAbsoluteIfNecessary(string textureAsString)
        {
            if (!string.IsNullOrEmpty(textureAsString) && FileManager.IsRelative(textureAsString))
            {
                textureAsString = FileManager.RemoveDotDotSlash(FileManager.RelativeDirectory + textureAsString);
            }
            return textureAsString;
        }

        public bool AskUserForProjectNameIfNecessary(out bool isProjectNew)
        {
            isProjectNew = false;
            bool shouldSave = true;
            // If it's null, that means the user hasn't saved this file yet
            if (string.IsNullOrEmpty(GumProjectSave.FullFileName))
            {
                shouldSave = false;

                var openFileDialog = new SaveFileDialog();

                openFileDialog.Filter = "Gum Project (*.gumx)|*.gumx";
                openFileDialog.Title = "Where would you like to save the Gum project?";

                DialogResult result = openFileDialog.ShowDialog();



                if (result == DialogResult.OK)
                {
                    GumProjectSave.FullFileName = openFileDialog.FileName;
                    shouldSave = true;
                    isProjectNew = true;
                }
            }
            return shouldSave;
        }

        internal void SaveElement(ElementSave elementSave)
        {
            if (elementSave.IsSourceFileMissing)
            {
                MessageBox.Show("Cannot save " + elementSave + " because its source file is missing");
            }
            else
            {
                bool succeeded = true;

                UndoManager.Self.RecordUndo();

                bool doesProjectNeedToSave = false;
                bool shouldSave = AskUserForProjectNameIfNecessary(out doesProjectNeedToSave);

                if (doesProjectNeedToSave)
                {
                    SaveProject();
                }

                if (shouldSave)
                {
                    string fileName = elementSave.GetFullPathXmlFile();

                    // if it's readonly, let's warn the user
                    bool isReadOnly = GetIfFileIsReadOnly(fileName);

                    if (isReadOnly)
                    {
                        ShowReadOnlyDialog(fileName);
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
                                elementSave.Save(fileName);
                                succeeded = true;
                                break;
                            }

                            catch (Exception e)
                            {
                                exception = e;
                                System.Threading.Thread.Sleep(100);
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
                    }
                }


                PluginManager.Self.Export(elementSave);
            }
        }

        public static void ShowReadOnlyDialog(string fileName)
        {
            MultiButtonMessageBox mbmb = new MultiButtonMessageBox();
            mbmb.MessageText = "Could not save the file\n\n" + fileName + "\n\nbecause it is read-only." +
                "What would you like to do?";

            mbmb.AddButton("Nothing (file will not save, Gum will continue to work normally)", DialogResult.Cancel);
            mbmb.AddButton("Open folder containing file", DialogResult.OK);

            var dialogResult = mbmb.ShowDialog();

            if (dialogResult == DialogResult.OK)
            {
                // Let's select the file instead of just opening the folder
                //string folder = FileManager.GetDirectory(fileName);
                //Process.Start(folder);
                Process.Start("explorer.exe", "/select," + fileName);
            }
        }

        public static bool GetIfFileIsReadOnly(string fileName)
        {
            bool isReadOnly = System.IO.File.Exists(fileName) && new FileInfo(fileName).IsReadOnly;
            return isReadOnly;
        }
        #endregion


    }
}
