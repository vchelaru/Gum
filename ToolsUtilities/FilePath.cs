using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ToolsUtilities
{
    public class FilePath
    {
        #region Fields

        string original;

        #endregion

        #region Operators

        public static bool operator ==(FilePath f1, FilePath f2)
        {
            return f1?.Standardized == f2?.Standardized;
        }

        public static bool operator !=(FilePath f1, FilePath f2)
        {
            return (f1 == f2) == false;
        }

        public static implicit operator FilePath(string s)
        {
            // Code to convert the book into an XML structure
            return new FilePath(s);
        }

        #endregion

        #region Properties

        public string Extension
        {
            get
            {
                return FileManager.GetExtension(original);
            }
        }

        public string StandardizedNoPathNoExtension
        {
            get
            {
                return FileManager.RemovePath(FileManager.RemoveExtension(Standardized));
            }
        }

        public string FileNameNoPath
        {
            get
            {
                return FileManager.RemovePath(original);
            }
        }

        public string FullPath
        {
            get
            {
                return FileManager.RemoveDotDotSlash(FileManager.Standardize(original, preserveCase: true, makeAbsolute: true));
            }

        }

        public string Standardized
        {
            get
            {
                if(string.IsNullOrEmpty(original))
                {
                    return FileManager.RemoveDotDotSlash(FileManager.Standardize("", preserveCase: false, makeAbsolute: true));
                }
                else
                {
                    return FileManager.RemoveDotDotSlash( FileManager.Standardize(original, preserveCase: false, makeAbsolute: true));
                }
            }
        }

        public string StandardizedCaseSensitive
        {
            get
            {
                return FileManager.RemoveDotDotSlash(FileManager.Standardize(original, preserveCase: true, makeAbsolute: true));
            }
        }

        #endregion

        public FilePath(string path)
        {
            original = path;
        }

        public override bool Equals(object obj)
        {
            var path = obj as FilePath;
            return path != null &&
                   Standardized == path.Standardized;
        }

        public FilePath GetDirectoryContainingThis()
        {
            return FileManager.GetDirectory(this.Standardized);
        }

        public override int GetHashCode()
        {
            var hashCode = 354063820;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Standardized);
            return hashCode;
        }

        public bool IsRootOf(FilePath otherFilePath)
        {
            return otherFilePath.Standardized.StartsWith(this.Standardized);
        }

        public FilePath RemoveExtension()
        {
            var fileString = FileManager.RemoveExtension(original);

            return fileString;
        }

        public override string ToString()
        {
            return Standardized;
        }
    }
}
