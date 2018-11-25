using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using GluePlugin.Converters;
using GluePlugin.SaveObjects;
using Gum;
using Gum.Logic.FileWatch;
using Gum.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ToolsUtilities;

namespace GluePlugin.Logic
{
    public class GlueProjectLoadingLogic : Singleton<GlueProjectLoadingLogic>
    {
        public void ShowLoadProjectDialog()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Glue Project (*.glux)|*.glux";

            if(openFileDialog.ShowDialog() == DialogResult.OK)
            {
                var fileName = openFileDialog.FileName;

                LoadGlueProject(fileName);
            }
        }

        private void LoadGlueProject(string fileName)
        {
            GluePluginState.Self.InitializationState = InitializationState.Initializing;

            var glueProjectSave = ToolsUtilities.FileManager.XmlDeserialize<GlueProjectSave>(fileName);
            GluePluginState.Self.GlueProject = glueProjectSave;
            GluePluginState.Self.GlueProjectFilePath = fileName;

            GluePluginState.Self.CsprojFilePath = GluePluginState.Self.GlueProjectFilePath.RemoveExtension() + ".csproj";

            var vsProject = VisualStudioProjectSave.Load(GluePluginState.Self.CsprojFilePath);
            GluePluginState.Self.ProjectRootNamespace = vsProject.GetRootNamespace();


            var gumProject = GlueToGumProjectConverter.Self.ToGumProjectSave(glueProjectSave);

            FilePath glueFilePath = fileName;
            var directory = glueFilePath.GetDirectoryContainingThis();
            var saveLocation = directory + "GumGluePlugin/testGum.gumx";

            // to prevent a reload:
            FileWatchLogic.Self.IgnoreNextChangeOn(saveLocation);
            gumProject.Save(saveLocation, saveElements:true);

            GumCommands.Self.FileCommands.LoadProject(saveLocation);


            StandardElementsCustomizationLogic.Self.CustomizeStandardElements();

            GluePluginState.Self.InitializationState = InitializationState.Initialized;

        }
    }
}
