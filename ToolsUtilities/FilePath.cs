using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ToolsUtilities
{
    public class FilePath
    {
        #region Fields

        public string Original
        {
            get;
            private set;
        }

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
            if(s == null)
            {
                return null;
            }
            else
            {
                return new FilePath(s);
            }
        }

        #endregion

        #region Properties

        public string Extension
        {
            get
            {
                return FileManager.GetExtension(Original);
            }
        }

        public string StandardizedNoPathNoExtension
        {
            get
            {
                return FileManager.RemovePath(FileManager.RemoveExtension(Standardized));
            }
        }

        public string CaseSensitiveNoPathNoExtension
        {
            get
            {
                return FileManager.RemovePath(FileManager.RemoveExtension(Original));
            }
        }

        public string FileNameNoPath
        {
            get
            {
                return FileManager.RemovePath(Original);
            }
        }

        public string FullPath
        {
            get
            {
                return FileManager.RemoveDotDotSlash(FileManager.Standardize(Original, preserveCase: true, makeAbsolute: true));
            }

        }

        public string Standardized
        {
            get
            {
                if(string.IsNullOrEmpty(Original))
                {
                    return FileManager.RemoveDotDotSlash(FileManager.Standardize("", preserveCase: false, makeAbsolute: true));
                }
                else
                {
                    return FileManager.RemoveDotDotSlash( FileManager.Standardize(Original, preserveCase: false, makeAbsolute: true));
                }
            }
        }

        public string StandardizedCaseSensitive
        {
            get
            {
                return FileManager.RemoveDotDotSlash(FileManager.Standardize(Original, preserveCase: true, makeAbsolute: true));
            }
        }

        #endregion

        public FilePath(string path)
        {
            if(string.IsNullOrEmpty(path))
            {
                throw new InvalidOperationException("Cannot create a FilePath with an empty string");
            }
            Original = path;
        }

        public override bool Equals(object obj)
        {
            var path = obj as FilePath;
            return path != null &&
                   Standardized == path.Standardized;
        }

        public bool Exists()
        {
            return System.IO.File.Exists(this.StandardizedCaseSensitive);
        }

        public FilePath GetDirectoryContainingThis()
        {
            return FileManager.GetDirectory(this.StandardizedCaseSensitive);
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
            var fileString = FileManager.RemoveExtension(Original);

            return fileString;
        }

        public override string ToString()
        {
            return StandardizedCaseSensitive;
        }

        public string RelativeTo(FilePath otherFilePath)
        {
            return FileManager.MakeRelative(this.FullPath, otherFilePath.FullPath);
        }
    }
}
