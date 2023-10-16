namespace Gum.DataTypes
{
    public class EventSave
    {
        public string Name { get; set; }
        public bool Enabled { get; set; }
        public string ExposedAsName { get; set; }

        public string GetExposedOrRootName()
        {
            if (!string.IsNullOrEmpty(ExposedAsName))
            {
                return ExposedAsName;
            }
            else
            {
                return GetRootName();
            }
        }


        public string GetRootName()
        {
            if (ToolsUtilities.StringFunctions.ContainsNoAlloc(Name, '.'))
            {
                return Name.Substring(Name.IndexOf('.') + 1);
            }
            else
            {
                return Name;
            }
        }

        public string GetSourceObject()
        {
            if (ToolsUtilities.StringFunctions.ContainsNoAlloc(Name, '.'))
            {
                int indexOfDot = Name.IndexOf(".");

                return Name.Substring(0, indexOfDot);
            }
            else
            {
                return null;
            }
            
        }
    }
}
