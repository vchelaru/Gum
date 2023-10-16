using Gum.DataTypes;
using System;
using WpfDataUi.DataTypes;

namespace Gum.PropertyGridHelpers
{
    public class EventInstanceMember : InstanceMember
    {
        EventSave mEventSave;

        ElementSave mElementSave;

        public EventInstanceMember(ElementSave element, InstanceSave instance, EventSave eventSave)
        {
            mElementSave = element;
            mEventSave = eventSave;
            if (!string.IsNullOrEmpty(eventSave.ExposedAsName))
            {
                this.Name = eventSave.ExposedAsName;
            }
            else
            {
                this.Name = eventSave.Name;
            }

            if (instance != null)
            {
                this.DisplayName = eventSave.GetRootName();
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

            if (mEventSave.Enabled && !this.mElementSave.Events.Contains(mEventSave))
            {
                mElementSave.Events.Add(mEventSave);
            }


            GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();
        }

    }
}
