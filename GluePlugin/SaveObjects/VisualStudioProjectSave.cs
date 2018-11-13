using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using ToolsUtilities;

namespace GluePlugin.SaveObjects
{
    public class VisualStudioProjectSave
    {
        public XDocument XDocument { get; private set; }



        public static VisualStudioProjectSave Load(FilePath filePath)
        {
            var xDocument = XDocument.Load(filePath.StandardizedCaseSensitive);

            var toReturn = new VisualStudioProjectSave();
            toReturn.XDocument = xDocument;

            return toReturn;
        }

        public IReadOnlyCollection<string> GetCodeFilesInProject()
        {
            List<string> codeFiles = new List<string>();

            foreach(var element in XDocument.Elements())
            {
                AddCodeFiles(element, codeFiles);
            }

            return codeFiles.ToArray();
        }

        public string GetRootNamespace()
        {
            Func<XElement, bool> predicate =
                (element) => element.Name?.LocalName == "RootNamespace";

            var foundNamespace = RecursivelyFind(predicate);

            return foundNamespace?.Value;
        }

        private XElement RecursivelyFind(Func<XElement, bool> predicate, 
            IEnumerable<XElement> elements = null)
        {
            if (elements == null)
            {
                elements = XDocument.Elements();
            }
            foreach(var element in elements)
            {
                if(predicate(element))
                {
                    return element;
                }
                else
                {
                    var found = RecursivelyFind(predicate, element.Elements());
                    if(found != null)
                    {
                        return found;
                    }
                }
            }
            return null;
        }

        private void AddCodeFiles(XElement element, List<string> codeFiles)
        {
            var isCompile = element.Name?.LocalName == "Compile";
            if (isCompile)
            {
                var includeAttribute = element.Attributes().FirstOrDefault(item => item.Name.LocalName == "Include");

                if (includeAttribute != null)
                {
                    if( includeAttribute.Value.EndsWith(".cs"))
                    {
                        codeFiles.Add(includeAttribute.Value);
                    }
                }
            }

            foreach(var subelement in element.Elements())
            {
                AddCodeFiles(subelement, codeFiles);
            }
        }
    }
}
