using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum.Plugins.BaseClasses;
using System.ComponentModel.Composition;
using Gum.DataTypes;

namespace Gum.Undo
{
    [Export(typeof(Gum.Plugins.BaseClasses.PluginBase))]
    public class UndoPlugin : InternalPlugin
    {
        public override void StartUp()
        {
            this.ElementSelected += new Action<DataTypes.ElementSave>(HandleElementSelected);
            this.InstanceSelected += new Action<DataTypes.ElementSave,DataTypes.InstanceSave>(HandleInstanceSelected);
            this.ProjectLoad += new Action<DataTypes.GumProjectSave>(HandleProjectLoad);
            this.InstanceAdd += new Action<ElementSave, InstanceSave>(HandleInstanceAdd);
            this.InstanceDelete += new Action<ElementSave, InstanceSave>(HandleInstanceDelete);
        }

        void HandleInstanceDelete(ElementSave arg1, InstanceSave arg2)
        {
            UndoManager.Self.RecordUndo();
        }

        void HandleInstanceAdd(ElementSave arg1, InstanceSave arg2)
        {
            UndoManager.Self.RecordUndo();
        }

        void HandleProjectLoad(DataTypes.GumProjectSave obj)
        {
            UndoManager.Self.ClearAll();
        }

        void HandleElementSelected(DataTypes.ElementSave obj)
        {
            UndoManager.Self.RecordState();
        }

        void HandleInstanceSelected(DataTypes.ElementSave elementSave, InstanceSave instanceSave)
        {
            UndoManager.Self.RecordState();
        }
    }
}
