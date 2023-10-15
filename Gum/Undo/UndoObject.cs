using System.Collections.Generic;
using Gum.DataTypes.Variables;
using Gum.DataTypes;
using ToolsUtilities;

namespace Gum.Undo
{
    public class UndoObject
    {
        StateSave mStateSave;

        public StateSave StateSave
        {
            get { return mStateSave; }
        }

        public List<InstanceSave> Instances
        {
            get;
            private set;
        }

        public object Parent
        {
            get;
            private set;
        }

        UndoObject(StateSave objectToSave, List<InstanceSave> instances, object parent)
        {
            mStateSave = objectToSave;
            Instances = instances;
            Parent = parent;
        }

        public static UndoObject CreateUndoObject(StateSave objectToSave, List<InstanceSave> instances, object parent)
        {
            StateSave cloned = FileManager.CloneSaveObject(objectToSave);
            List<InstanceSave> instancesCloned = null;
            if (instances != null)
            {
                instancesCloned = new List<InstanceSave>();
                foreach (var instance in instances)
                {
                    instancesCloned.Add(instance); // no need to clone this, I don't think
                }
            }
            UndoObject undoObject = new UndoObject(
                cloned,
                instancesCloned,
                parent);

            return undoObject;
        }
    }
}
