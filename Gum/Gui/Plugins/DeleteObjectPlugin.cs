using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum.Plugins.BaseClasses;
using System.ComponentModel.Composition;
using System.Windows.Forms;
using Gum.DataTypes;
using System.Drawing;

namespace Gum.Gui.Plugins
{
    [Export(typeof(Gum.Plugins.BaseClasses.PluginBase))]
    public class DeleteObjectPlugin : InternalPlugin
    {
        public CheckBox DeleteXmlCheckBox { get; private set; }

        public override void StartUp()
        {
            CreateDeleteXmlFileComboBox();

            this.DeleteOptionsWindowShow += new Action<Forms.DeleteOptionsWindow, object>(HandleDeleteOptionsShow);
            this.DeleteConfirm += new Action<Forms.DeleteOptionsWindow, object>(HandleDeleteConfirm);
        }

        void HandleDeleteConfirm(Forms.DeleteOptionsWindow deleteOptionsWindow, object deletedObject)
        {
            if (DeleteXmlCheckBox.Checked)
            {
                string fileName = GetFileNameForObject(deletedObject);

                if (System.IO.File.Exists(fileName))
                {
                    try
                    {
                        System.IO.File.Delete(fileName);
                    }
                    catch
                    {
                        MessageBox.Show("Could not delete the file\n" + fileName);
                    }
                }
            }
        }

        public string GetFileNameForObject(object deletedObject)
        {
            if (deletedObject is ElementSave)
            {
                ElementSave asElement = deletedObject as ElementSave;

                return asElement.GetFullPathXmlFile();
            }
            else if (deletedObject is InstanceSave)
            {
                return null;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        void HandleDeleteOptionsShow(Forms.DeleteOptionsWindow obj, object objectToDelete)
        {
            obj.AddUi(DeleteXmlCheckBox);
            DeleteXmlCheckBox.Text = "Delete XML file";
            DeleteXmlCheckBox.Width = 220;
        }

        private void CreateDeleteXmlFileComboBox()
        {
            DeleteXmlCheckBox = new CheckBox();
            DeleteXmlCheckBox.Checked = true;
        }

    }
}
