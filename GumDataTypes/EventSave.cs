using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gum.DataTypes
{
    public class EventSave
    {
        public string Name { get; set; }
        public bool Enabled { get; set; }
        public string ExposedAsName { get; set; }

        public string GetRootName()
        {
            if (Name.Contains('.'))
            {
                return Name.Substring(Name.IndexOf('.') + 1);
            }
            else
            {
                return Name;
            }
        }
    }
}
