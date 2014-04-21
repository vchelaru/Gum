using Gum.DataTypes;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Managers
{
    class EventsViewModel
    {

        public InstanceSave InstanceSave { get; set; }
        public ElementSave ElementSave { get; set; }

        public bool Click 
        {
            get { return IsEventOn("Click"); }
            set { SetEvent("Click", value); }
        }

        public bool RollOver 
        {
            get { return IsEventOn("RollOver"); }

            set { SetEvent("RollOver", value); }

        }

        public bool RollOn 
        {
            get { return IsEventOn("RollOn"); }
            set { SetEvent("RollOn", value); }

        }
        
        public bool RollOff 
        {
            get { return IsEventOn("RollOff"); }

            set { SetEvent("RollOff", value); }

        }



        bool IsEventOn(string eventName)
        {
            if (InstanceSave != null)
            {
                eventName = InstanceSave.Name + "." + eventName;
            }

            return ElementSave.Events.Any(item => item.Name == eventName && item.Enabled);
        }

        void SetEvent(string eventName, bool value)
        {

            if (InstanceSave != null)
            {
                eventName = InstanceSave.Name + "." + eventName;
            }

            var eventInstance = ElementSave.Events.FirstOrDefault(item=>item.Name == eventName);


            if(eventInstance == null)
            {
                eventInstance = new EventSave();
                ElementSave.Events.Add(eventInstance);
                eventInstance.Name = eventName;
            }
            eventInstance.Enabled = value;

            // Save the change
            if (SelectedState.Self.SelectedElement != null)
            {
                GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();
            }
        }
    }



}
