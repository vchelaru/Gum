using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ToolsUtilities;

namespace Gum.DataTypes.Behaviors
{
    public class BehaviorReference
    {
        public const string Subfolder = "Behaviors";
        public const string Extension = "behx";

        public string Name;

        public BehaviorSave ToBehaviorSave(string projectRoot)
        {
            string fullName = projectRoot + Subfolder + "/" + Name + "." + Extension;

            if (FileManager.FileExists(fullName))
            {
                BehaviorSave behaviorSave = FileManager.XmlDeserialize<BehaviorSave>(fullName);

                return behaviorSave;
            }
            else
            {
                // todo: eventually add this:
                //result.MissingFiles.Add(fullName);


                BehaviorSave behaviorSave = new BehaviorSave();

                behaviorSave.Name = Name;
                behaviorSave.IsSourceFileMissing = true;

                return behaviorSave;
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
