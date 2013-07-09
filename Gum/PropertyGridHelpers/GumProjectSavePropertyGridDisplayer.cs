using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum.DataTypes;
using Gum.ToolStates;
using System.ComponentModel;
using Gum.DataTypes.ComponentModel;
using Gum.Settings;

namespace Gum.PropertyGridHelpers
{
    public class GumProjectSavePropertyGridDisplayer : PropertyGridDisplayer
    {
        ReflectingPropertyDescriptorHelper mHelper = new ReflectingPropertyDescriptorHelper();

        public GumProjectSave GumProjectSave
        {
            get;
            set;
        }

        public GeneralSettingsFile GeneralSettings
        {
            get;
            set;
        }

        public override System.ComponentModel.PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            PropertyDescriptorCollection pdc = mHelper.GetEmpty();

            mHelper.CurrentInstance = GumProjectSave;

            mHelper.Include(GumProjectSave, "DefaultCanvasHeight", ref pdc);
            mHelper.Include(GumProjectSave, "DefaultCanvasWidth", ref pdc);



            return pdc;
        }
    }
}
