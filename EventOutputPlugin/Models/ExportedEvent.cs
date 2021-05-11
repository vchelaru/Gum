using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventOutputPlugin.Models
{
    public class ExportedEvent
    {
        public string NewName { get; set; }
        public string OldName { get; set; }
        public string ElementType { get; set; }
        public GumEventTypes EventType { get; set; }
        public DateTime TimestampUtc { get; set; }
    }


}
