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
            this.ElementSelected += HandleElementSelected;
            this.InstanceSelected += HandleInstanceSelected;
            this.ProjectLoad += HandleProjectLoad;
            this.InstanceAdd += HandleInstanceAdd;
            this.InstanceDelete += HandleInstanceDelete;

            this.BehaviorReferencesChanged += HandleBehaviorReferencesChanged;
        }

        private void HandleBehaviorReferencesChanged(ElementSave obj)
        {
            UndoManager.Self.RecordUndo();
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
