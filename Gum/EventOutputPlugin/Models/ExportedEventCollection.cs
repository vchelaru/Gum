using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace EventOutputPlugin.Models
{
    // This data model associates users with a list of gum events
    // keeping events in user-based lists helps prevent git conflicts
    // if multiple users are working on a single gum project
    public class ExportedEventCollection
    {
        public Dictionary<string, List<ExportedEvent>> UserEvents { get; set; }

        public ExportedEventCollection()
        {
            UserEvents = new Dictionary<string, List<ExportedEvent>>();
        }

        /// <summary>
        /// Deserializes a gum_events.json file. An empty file, a null <see cref="UserEvents"/>
        /// or a null per-user list comes back empty rather than null.
        /// </summary>
        public static ExportedEventCollection FromJson(string json)
        {
            ExportedEventCollection collection = JsonConvert.DeserializeObject<ExportedEventCollection>(json)
                ?? new ExportedEventCollection();

            // JSON can set these to null even though the constructor initializes them.
            if (collection.UserEvents == null)
            {
                collection.UserEvents = new Dictionary<string, List<ExportedEvent>>();
            }

            foreach (string user in collection.UserEvents.Keys.ToList())
            {
                if (collection.UserEvents[user] == null)
                {
                    collection.UserEvents[user] = new List<ExportedEvent>();
                }
            }

            return collection;
        }
    }
}
