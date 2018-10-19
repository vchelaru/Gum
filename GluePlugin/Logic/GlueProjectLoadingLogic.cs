using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using GluePlugin.Converters;
using Gum;
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
            var glueProjectSave = ToolsUtilities.FileManager.XmlDeserialize<GlueProjectSave>(fileName);
            GluePluginState.Self.GlueProject = glueProjectSave;
            GluePluginState.Self.GlueProjectFilePath = fileName;

            var gumProject = GlueToGumProjectConverter.Self.ToGumProjectSave(glueProjectSave);

            FilePath glueFilePath = fileName;
            var directory = glueFilePath.GetDirectoryContainingThis();
            var saveLocation = directory + "GumGluePlugin/testGum.gumx";

            gumProject.Save(saveLocation, saveElements:true);

            GumCommands.Self.FileCommands.LoadProject(saveLocation);

            StandardElementsCustomizationLogic.Self.CustomizeStandardElements();
        }
    }
}
