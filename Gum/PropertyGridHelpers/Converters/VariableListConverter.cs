using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing.Design;
using System.ComponentModel;

namespace Gum.PropertyGridHelpers.Converters
{
    public class VariableListConverter : UITypeEditor
    {
        //Button mAddButton;

        public override UITypeEditorEditStyle GetEditStyle(
            System.ComponentModel.ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.Modal;
        }

        public override object EditValue(
            ITypeDescriptorContext context,
            IServiceProvider provider,
            object value)
        {

            List<string> toReturn = value as List<string>;

            FileListWindow fileListWindow = new FileListWindow();

            fileListWindow.FillFrom(toReturn);

            DialogResult result = fileListWindow.ShowDialog();


            if (result == DialogResult.OK)
            {
                toReturn = fileListWindow.GetList();
            }
            else
            {
                // do nothing, return the toReturn;
            }

            return toReturn;
        }
    }




}
