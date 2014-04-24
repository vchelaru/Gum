using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Gum.DataTypes.Variables
{
    public class StateSaveCategory
    {
        public string Name
        {
            get;
            set;
        }

        [XmlElement("State")]
        public List<StateSave> States
        {
            get;
            set;
        }


        public StateSaveCategory()
        {
            States = new List<StateSave>();
        }
    }
}
