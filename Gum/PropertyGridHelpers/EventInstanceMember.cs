using Gum.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfDataUi.DataTypes;

namespace Gum.PropertyGridHelpers
{
    public class EventInstanceMember : InstanceMember
    {
        EventSave mEventSave;



        public EventInstanceMember(ElementSave element, InstanceSave instance, EventSave eventSave)
        {
            mEventSave = eventSave;
            if (!string.IsNullOrEmpty(eventSave.ExposedAsName))
            {
                this.Name = eventSave.ExposedAsName;
            }
            else
            {
                this.Name = eventSave.Name;
            }

            this.CustomSetEvent += HandlePropertyChanged;
            this.CustomGetEvent += HandlePropertyGet;
            this.CustomGetTypeEvent += HandleGetType;
        }

        private Type HandleGetType(object arg)
        {
            return typeof(bool);
        }

        private object HandlePropertyGet(object arg)
        {
            return mEventSave.Enabled;
        }

        private void HandlePropertyChanged(object arg1, object arg2)
        {
            mEventSave.Enabled = (bool)arg2;

            GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();
        }

    }
}
