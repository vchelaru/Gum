using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ToolsUtilities;

namespace Gum.DataTypes
{
    public enum ElementType
    {
        Screen,
        Component,
        Standard
    }

    public class ElementReference
    {
        public const string ScreenSubfolder = "Screens";
        public const string ComponentSubfolder = "Components";
        public const string StandardSubfolder = "Standards";

        public ElementType ElementType
        {
            get;
            set;
        }

        string Subfolder
        {
            get
            {
                switch (ElementType)
                {
                    case DataTypes.ElementType.Standard:
                        return StandardSubfolder;
                    case DataTypes.ElementType.Component:
                        return ComponentSubfolder;
                    case DataTypes.ElementType.Screen:
                        return ScreenSubfolder;
                }
                throw new InvalidOperationException();
            }
        }

        public string Name;

        public ElementSave ToElementSave(string projectroot, string extension)
        {
            string fullName = projectroot + Subfolder + "/" + Name + "." + extension;

            ElementSave elementSave = FileManager.XmlDeserialize<ElementSave>(fullName);

            return elementSave;
        }


        public T ToElementSave<T>(string projectroot, string extension) where T : ElementSave
        {
            string fullName = projectroot + Subfolder + "/" + Name + "." + extension;



            T elementSave = FileManager.XmlDeserialize<T>(fullName);

            return elementSave;
        }

        public override string ToString()
        {
            return Name;
        }

    }



}
