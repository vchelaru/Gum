using System;
using System.Collections.Generic;

namespace ToolsUtilities
{
    public class FilePath
    {
        #region Fields

        public string Original { get; private set; }

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

        public string Extension { get; private set; }

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

        public string FullPath { get; private set; }

        public string Standardized { get; private set; }

        public string StandardizedCaseSensitive { get; private set; }

        #endregion

        public FilePath(string path)
        {
            Original = path;
            Standardized = string.IsNullOrEmpty(Original)
                    ? FileManager.RemoveDotDotSlash(StandardizeInternal("")).ToLowerInvariant()
                    : FileManager.RemoveDotDotSlash(StandardizeInternal(Original)).ToLowerInvariant();

            StandardizedCaseSensitive =
                FileManager.RemoveDotDotSlash(StandardizeInternal(Original));

            FullPath = string.IsNullOrEmpty(Original)
                ? FileManager.RemoveDotDotSlash(StandardizeInternal(""))
                : FileManager.RemoveDotDotSlash(StandardizeInternal(Original));

            Extension = FileManager.GetExtension(Original);
        }

        public override bool Equals(object obj)
        {
            if (obj is FilePath)
            {
                var path = obj as FilePath;
                return path != null &&
                       Standardized == path.Standardized;
            }
            else if (obj is string)
            {
                var path = new FilePath(obj as string);
                return path != null &&
                       Standardized == path.Standardized;
            }
            else
            {
                return false;
            }
        }

        public FilePath GetDirectoryContainingThis()
        {
            var directoryAsString = FileManager.GetDirectory(this.StandardizedCaseSensitive);
            if(string.IsNullOrEmpty(directoryAsString))
            {
                return null;
            }
            else
            {
                return directoryAsString;
            }
        }

        public override int GetHashCode()
        {
            var hashCode = 354063820;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Standardized);
            return hashCode;
        }

        public bool Exists()
        {
            var standardized = this.StandardizedCaseSensitive;
            if (standardized.EndsWith("/"))
            {
                return System.IO.Directory.Exists(this.StandardizedCaseSensitive);
            }
            else
            {
                // Update - this may be a directory like "c:/SomeDirectory/" or "c:/SomeDirectory/". We don't know, so we have to check both directory and file:
                return System.IO.File.Exists(standardized) ||
                    System.IO.Directory.Exists(standardized);
            }

        }

        public bool IsRootOf(FilePath otherFilePath)
        {
            return otherFilePath.Standardized.StartsWith(this.Standardized) && otherFilePath != this;
        }

        /// <summary>
        /// Returns a new FilePath with no extension.
        /// </summary>
        /// <returns>The new FilePath which has its extension removed.</returns>
        public FilePath RemoveExtension()
        {
            var fileString = FileManager.RemoveExtension(Original);

            return fileString;
        }

        public override string ToString()
        {
            return StandardizedCaseSensitive;
        }

        static void ReplaceSlashes(ref string stringToReplace)
        {
            bool isNetwork = false;
            if (stringToReplace.StartsWith("\\\\"))
            {
                stringToReplace = stringToReplace.Substring(2);
                isNetwork = true;
            }

            stringToReplace = stringToReplace.Replace("\\", "/");

            if (isNetwork)
            {
                stringToReplace = "\\\\" + stringToReplace;
            }
        }

        private string StandardizeInternal(string fileNameToFix)
        {
            if (fileNameToFix == null)
                return null;

            bool isNetwork = fileNameToFix.StartsWith("\\\\");

            ReplaceSlashes(ref fileNameToFix);

            if (!isNetwork)
            {
                if (FileManager.IsRelative(fileNameToFix))
                {
                    fileNameToFix = (FileManager.RelativeDirectory + fileNameToFix);
                    ReplaceSlashes(ref fileNameToFix);
                }
            }

            fileNameToFix = FileManager.RemoveDotDotSlash(fileNameToFix);

            if (fileNameToFix.StartsWith(".."))
            {
                throw new InvalidOperationException("Tried to remove all ../ but ended up with this: " + fileNameToFix);
            }

            // It's possible that there will be double forward slashes.
            fileNameToFix = fileNameToFix.Replace("//", "/");

            return fileNameToFix;
        }

        public int CompareTo(object obj)
        {
            if (obj is FilePath otherAsFilePath)
            {
                return this.FullPath.CompareTo(otherAsFilePath?.FullPath);
            }
            else if (obj is string asString)
            {
                return this?.FullPath.CompareTo(asString) ?? 0;
            }
            else
            {
                return 0;
            }
        }

        public string RelativeTo(FilePath otherFilePath)
        {
            return FileManager.MakeRelative(this.FullPath, otherFilePath.FullPath, preserveCase:true);
        }
    }
}
