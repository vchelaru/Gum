using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        #region Methods


        private ProjectManager()
        {

        }

        public void Initialize()
        {
            GeneralSettingsFile = GeneralSettingsFile.LoadOrCreateNew();

            if (!string.IsNullOrEmpty(GeneralSettingsFile.LastProject))
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

                LoadProject(fileName);

                GeneralSettingsFile.LastProject = fileName;
                GeneralSettingsFile.Save();

                return true;
            }


            return false;
        }

        private void LoadProject(string fileName)
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

            mGumProjectSave.Initialize();
            mGumProjectSave.AddNewStandardElementTypes();
            mGumProjectSave.FixStandardVariables();

            FileManager.RelativeDirectory = FileManager.GetDirectory(fileName);
            mGumProjectSave.RemoveDuplicateVariables();


            // Now that a new project is loaded, refresh the UI!
            ElementTreeViewManager.Self.RefreshUI();
            // And the guides
            WireframeObjectManager.Self.UpdateGuides();

            PluginManager.Self.ProjectLoad(mGumProjectSave);
        }

        internal void SaveProject()
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
                        bool saveContainedElements = isNewProject;

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
                    try
                    {
                        elementSave.Save(fileName);
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show("Unknown error trying to save the file\n\n" + fileName + "\n\n" + e.ToString());
                    }
                }
            }

            PluginManager.Self.Export(elementSave);
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
                string folder = FileManager.GetDirectory(fileName);

                Process.Start(folder);
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
