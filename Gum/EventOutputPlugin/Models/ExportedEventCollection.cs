using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventOutputPlugin.Models
{
    // This data model associates users with a list of gum events
    // keeping events in user-based lists helps prevent git conflicts
    // if multiple users are working on a single gum project
    public class ExportedEventCollection
    {
        public Dictionary<string, List<ExportedEvent>> UserEvents { get; set; }
    }
}
