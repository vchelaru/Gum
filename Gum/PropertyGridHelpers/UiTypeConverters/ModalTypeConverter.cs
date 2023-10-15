using System;
using System.Collections.Generic;
using System.Drawing.Design;
using System.ComponentModel;
using System.Windows.Forms.Design;
using System.Windows.Forms;
using CommonFormsAndControls;

namespace Gum.PropertyGridHelpers.UiTypeConverters
{
    public class ModalTypeConverter : UITypeEditor
    {
        // My initial plan was to have the UITypeEditor be part
        // of the plugin, and have it be added to the Attribute list.
        // Unfortunately it seems like if we do this Gum can't dynamically
        // instantiate the UITypeEditor because it's not part of this assembly.
        // So instead we use a standard UITypeConverter (ModalTypeConverter).  Of
        // course since we don't control the instantiation we have to have a way for
        // plugins to tell it what form to use.  So...I do it through this static dictionary.
        // It's a bit of a hack but I couldn't figure out any other way to do it.  I think it's
        // just because MEF + customizing the PropertyGrid UI is just a combination that perhaps
        // has some holes.        
        public static Dictionary<string, Form> VariableNameFormAssocaition = new Dictionary<string,Form>();

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            if (context == null || context.Instance == null)
                return base.GetEditStyle(context);

            return UITypeEditorEditStyle.Modal;
        }

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            if (context == null || context.Instance == null || provider == null)
                return value;

            var editorService = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
            string variableName = context.PropertyDescriptor.DisplayName;

            Form form = VariableNameFormAssocaition[variableName];
            FormUtilities.Self.PositionCenterToCursor(form);
            DialogResult result = form.ShowDialog();

            object toReturn = value;

            if (result == DialogResult.OK)
            {
                // For now I'll use ToString, but maybe we
                // can adjust this in the future by making a
                // base class.
                toReturn = form.ToString();
            }

            return toReturn;

        }

    }
}
