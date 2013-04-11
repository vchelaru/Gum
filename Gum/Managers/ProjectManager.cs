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
                mGumProjectSave = new GumProjectSave();
                ObjectFinder.Self.GumProjectSave = mGumProjectSave;

                StandardElementsManager.Self.PopulateProjectWithDefaultStandards(mGumProjectSave);
                // Now that a new project is created, refresh the UI!
                ElementTreeViewManager.Self.RefreshUI();
            }



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
                mGumProjectSave = new GumProjectSave();
            }
            else
            {
                mHaveErrorsOccurred = false;
            }

            ObjectFinder.Self.GumProjectSave = mGumProjectSave;

            mGumProjectSave.Initialize();
            mGumProjectSave.AddNewStandardElementTypes();

            FileManager.RelativeDirectory = FileManager.GetDirectory(fileName);


            // Now that a new project is loaded, refresh the UI!
            ElementTreeViewManager.Self.RefreshUI();
            // And the guides
            WireframeObjectManager.Self.UpdateGuides();

            PluginManager.Self.ProjectLoad(mGumProjectSave);
        }

        public void SaveProject()
        {
            if (mHaveErrorsOccurred)
            {
                MessageBox.Show("Can't save project because errors occurred when the project was last loaded");
            }
            else
            {
                bool throwaway;
                bool shouldSave = AskUserForProjectNameIfNecessary(out throwaway);

                if (shouldSave)
                {
                    GumProjectSave.Save(GumProjectSave.FullFileName);
                    // This may be the first time the file is being saved.  If so, we should make it relative
                    FileManager.RelativeDirectory = FileManager.GetDirectory(GumProjectSave.FullFileName);
                }
            }
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

                elementSave.Save(fileName);
            }

            PluginManager.Self.Export(elementSave);
        }
        #endregion


    }
}
