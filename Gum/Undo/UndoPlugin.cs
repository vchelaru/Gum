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
            this.InstancesDelete += HandleInstancesDelete;

            this.BehaviorReferencesChanged += HandleBehaviorReferencesChanged;
        }

        private void HandleBehaviorReferencesChanged(ElementSave obj)
        {
            UndoManager.Self.RecordUndo();
        }

        void HandleInstancesDelete(ElementSave arg1, InstanceSave[] arg2)
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
            UndoManager.Self.BroadcastUndosChanged();
        }

        void HandleInstanceSelected(DataTypes.ElementSave elementSave, InstanceSave instanceSave)
        {
            UndoManager.Self.RecordState();
            // the instance could have changed the element, so broadcast anyway
            UndoManager.Self.BroadcastUndosChanged();
        }
    }
}
