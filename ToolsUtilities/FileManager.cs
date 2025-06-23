﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using System.Runtime.InteropServices.ComTypes;

namespace ToolsUtilities
{
    /// <summary>
    /// Utility class used to help dealing with files.
    /// </summary>
    public static partial class FileManager
    {
        public const char DefaultSlash = '\\';
        #region Fields

        static bool IsMobile =>
#if NET6_0_OR_GREATER
            System.OperatingSystem.IsAndroid() || 
                System.OperatingSystem.IsIOS() ;
#elif ANDROID || IOS
        true;
#else
        false;
#endif

        public static string ExeLocation
        {
            get
            {
                if (IsMobile)
                {
                    return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location).ToLower().Replace("/", "\\") + "\\";
                }
                else
                {

                    string result = AppContext.BaseDirectory;

                    // Blazor-WASM returns "/" for BaseDirectory, which is invalid for GetDirectoryName.
                    if (result != "/")
                        result = Path.GetDirectoryName(result);

                    return result.Replace('/', Path.DirectorySeparatorChar)
                                 .Replace('\\', Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                }
            }
        }

        static string mRelativeDirectory = ExeLocation;

        static Dictionary<Type, XmlSerializer> mXmlSerializers = new Dictionary<Type, XmlSerializer>();

        #endregion

        #region Properties

        public static string RelativeDirectory
        {
            get { return mRelativeDirectory; }
            set
            {
                if (IsRelative(value))
                {
                    if (IsMobile)
                    {
                        mRelativeDirectory = "./" + value;
                    }
                    else
                    {
                        mRelativeDirectory = ExeLocation + value;
                    }
                }
                else
                {
                    mRelativeDirectory = value.Replace('/', Path.DirectorySeparatorChar)
                        .Replace('\\', Path.DirectorySeparatorChar);
                }
            }
        }

        #endregion

        #region Delegates

        public static Func<string, Stream> CustomGetStreamFromFile;

        #endregion

        #region Methods

        public static bool AreSaveObjectsEqual<T>(T first, T second)
        {
            string firstAsString;
            string secondAsString;

            XmlSerialize(first, out firstAsString);
            XmlSerialize(second, out secondAsString);

            return firstAsString == secondAsString;
        }

        public static T CloneSaveObject<T>(T objectToClone)
        {
            string container;

            XmlSerialize(objectToClone, out container);

            XmlSerializer serializer = GetXmlSerializer(typeof(T));// new XmlSerializer(type);

            return (T)serializer.Deserialize(new StringReader(container));
        }

        public static T CloneSaveObjectCast<U, T>(U objectToClone)
        {
            string container;

            XmlSerialize(objectToClone, out container);

            XmlSerializer serializer = GetXmlSerializer(typeof(T));// new XmlSerializer(type);

            return (T)serializer.Deserialize(new StringReader(container));
        }

        public static bool FileExists(string fileName)
        {
            return FileExists(fileName, false);
        }


        public static bool FileExists(string fileName, bool ignoreExtensions)
        {
            fileName = Standardize(fileName, preserveCase: true, makeAbsolute: true);
            if (!ignoreExtensions)
            {
                if (IsMobile)
                {
                    try
                    {
                        if (fileName.StartsWith(".\\") || fileName.StartsWith("./"))
                        {
                            fileName = fileName.Substring(2);
                        }
                        using (var stream = CustomGetStreamFromFile(fileName))
                        {
                            return stream != null;
                        }
                    }
                    catch
                    {
                        return false;
                    }
                }
                else
                {
                    return File.Exists(fileName);
                }
            }
            else
            {
                // This takes a little bit of work
                string fileWithoutExtension = FileManager.RemoveExtension(fileName);

                List<string> filesInDirectory = GetAllFilesInDirectory(
                    FileManager.GetDirectory(fileName),
                    null,
                    0);

                for (int i = 0; i < filesInDirectory.Count; i++)
                {
                    if (filesInDirectory[i] == fileName ||
                        FileManager.RemoveExtension(filesInDirectory[i]) == fileWithoutExtension)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public static bool DirectoryExists(string directory)
        {
            directory = Standardize(directory, true, true);

            return Directory.Exists(directory);
        }

        public static string FromFileText(string fileName)
        {
            Encoding encoding = Encoding.Default;
            return FromFileText(fileName, encoding);
        }

        public static string FromFileText(string fileName, Encoding encoding)
        {
            string containedText = "";

            if (IsRelative(fileName))
            {
                fileName = RelativeDirectory + fileName;
            }

            fileName = TryRemoveLeadingDotSlash(fileName);

            // Creating a filestream then using that enables us to open files that are open by other apps.
            using (var fileStream = GetStreamForFile(fileName))
            {
                using (System.IO.StreamReader sr = new StreamReader(fileStream, encoding))
                {
                    containedText = sr.ReadToEnd();
                    sr.Close();
                }
            }

            return containedText;
        }




        public static string GetDirectory(string fileName)
        {
            string directoryToReturn = "";

            if (fileName == null)
            {
                throw new Exception("The fileName passed to GetDirectory is null.  Non-null is required");
            }

            int lastIndex = System.Math.Max(
                fileName.LastIndexOf('/'), fileName.LastIndexOf('\\'));

            if (lastIndex == fileName.Length - 1)
            {
                // If this happens then fileName is actually a directory.
                // So we should return the parent directory of the argument.

                lastIndex = System.Math.Max(
                    fileName.LastIndexOf('/', fileName.Length - 2),
                    fileName.LastIndexOf('\\', fileName.Length - 2));
            }

            if (lastIndex != -1)
            {
                //bool isFtp = false;

                //if (FileManager.IsUrl(fileName) || isFtp)
                //{
                //    // don't standardize URLs - they're case sensitive!!!

                //}
                //else
                //{
                //    return FileManager.Standardize(fileName.Substring(0, lastIndex + 1));
                //}
                directoryToReturn = fileName.Substring(0, lastIndex + 1);

            }
            else
            {
                directoryToReturn = ""; // there was no directory found.
            }

            return directoryToReturn;
        }

        /// <summary>
        /// Returns the extension in a filename.
        /// </summary>
        /// <remarks>
        /// The extension returned will not contain a period.
        /// 
        /// <para>
        /// <code>
        /// // this code returns a string containing "png", not ".png"
        /// FileManager.GetExtension(@"FolderName/myImage.png");
        /// </code>
        /// </para>
        /// </remarks>
        /// <param name="fileName">The filename.</param>
        /// <returns>Returns the extension or an empty string if no period is found in the filename.</returns>
        public static string GetExtension(string fileName)
        {
            try
            {
                if (fileName == null)
                {
                    return "";
                }


                int dotIndex = fileName.LastIndexOf('.');
                int lastSlash = fileName.LastIndexOf("/");
                int lastBackSlash = fileName.LastIndexOf("\\");

                if (dotIndex != -1)
                {
                    bool hasDotSlash = false;

                    if (dotIndex == fileName.Length - 1 || lastSlash > dotIndex || lastBackSlash > dotIndex)
                    {
                        return "";
                    }




                    if (dotIndex < fileName.Length - 1 && (fileName[dotIndex + 1] == '/' || fileName[dotIndex + 1] == '\\'))
                    {
                        hasDotSlash = true;
                    }

                    if (hasDotSlash)
                    {
                        return "";
                    }
                    else
                    {
                        return fileName.Substring(dotIndex + 1, fileName.Length - (dotIndex + 1)).ToLower();
                    }
                }
                else
                {
                    return ""; // This returns "" because calling the method with a string like "redball" should return no extension
                }
            }
            catch
            {
                //EMP: Removed to clean up Warnings
                //int m = 3;
                throw new Exception();
            }
        }






        public static string GetWordAfter(string stringToStartAfter, string entireString)
        {
            return GetWordAfter(stringToStartAfter, entireString, 0);
        }

        static char[] WhitespaceChars = new char[] { ' ', '\n', '\t', '\r' };
        public static string GetWordAfter(string stringToStartAfter, string entireString, int indexToStartAt)
        {
            int indexOf = entireString.IndexOf(stringToStartAfter, indexToStartAt);
            if (indexOf != -1)
            {
                int startOfWord = indexOf + stringToStartAfter.Length;

                // Let's not count the start of the word if it's a newline
                while (entireString[startOfWord] == WhitespaceChars[0] ||
                    entireString[startOfWord] == WhitespaceChars[1] ||
                    entireString[startOfWord] == WhitespaceChars[2] ||
                    entireString[startOfWord] == WhitespaceChars[3])
                {
                    startOfWord++;
                }

                int endOfWord = entireString.IndexOfAny(WhitespaceChars, startOfWord);

                return entireString.Substring(startOfWord, endOfWord - startOfWord);
            }
            else
            {
                return null;
            }

        }


        public static bool IsRelative(string fileName)
        {
            bool relative = false;

            if (fileName == null)
            {
                throw new System.ArgumentException("Cannot check if a null file name is relative.");
            }

            if (IsMobile)
            {
                // Justin Johnson 6/6/2017: this compiler flagged code might be eliminated now that 
                // this whole method is more cross platform friendly!
                if (fileName.Length > 1 && fileName[0] == '.' && (fileName[1] == '/' || fileName[1] == '\\'))
                    return false;
                else
                    return true;
            }
            else
            {
                if (IsUrl(fileName))
                {
                    relative = false;
                }
                else
                if (fileName.StartsWith(ExeLocation))
                {
                    relative = false;
                }
                else if (fileName == String.Empty)
                {
                    relative = true; // it doesn't have a prefix so it's technically relative:
                }
                else if (fileName.Length < 1 || !Path.IsPathRooted(fileName))
                {
                    // On linux and mac, we need to still check if it has a root:
                    var root = Path.GetPathRoot(fileName);


                    relative = string.IsNullOrWhiteSpace(root);
                }
            }

            return relative;
        }


        public static bool IsRelativeTo(string fileName, string directory)
        {
            directory = RemoveDotDotSlash(directory);
            if (!IsRelative(fileName))
            {
                // the filename is an absolute path

                if (!IsRelative(directory))
                {
                    // just have to make sure that the filename includes the path
                    fileName = fileName.ToLower().Replace('\\', '/');
                    directory = directory.ToLower().Replace('\\', '/');


                    if (directory.EndsWith("/") == false)
                    {
                        // Do this to simplify the code below by allowing a "contains" call
                        directory += "/";
                    }


                    return fileName.IndexOf(directory) == 0;
                }
            }
            else // fileName is relative
            {
                if (IsRelative(directory))
                {
                    // both are relative, so let's make em full and see what happens
                    string fullFileName = FileManager.Standardize(fileName);
                    string fullDirectory = FileManager.Standardize(directory);

                    return IsRelativeTo(fullFileName, fullDirectory);

                }
            }
            return false;
        }



        public static string MakeAbsolute(string pathToMakeAbsolute)
        {
            if (IsRelative(pathToMakeAbsolute) == false)
            {
                throw new ArgumentException("The path is already absolute: " + pathToMakeAbsolute);
            }

            return Standardize(pathToMakeAbsolute, true, true);// RelativeDirectory + pathToMakeAbsolute;
        }

        public static string MakeRelative(string pathToMakeRelative, string pathToMakeRelativeTo)
        {
            return MakeRelative(pathToMakeRelative, pathToMakeRelativeTo, false);
        }

        public static string MakeRelative(string pathToMakeRelative, string pathToMakeRelativeTo, bool preserveCase)
        {
            // If it's a URL we can't make it relative
            if (IsUrl(pathToMakeRelative))
            {
                return pathToMakeRelative;
            }

            if (string.IsNullOrEmpty(pathToMakeRelative) == false)
            {
                pathToMakeRelative = FileManager.Standardize(pathToMakeRelative, preserveCase);
                pathToMakeRelativeTo = FileManager.Standardize(pathToMakeRelativeTo, preserveCase);

                // Use the old method if we can
                if (pathToMakeRelative.StartsWith(pathToMakeRelativeTo, StringComparison.OrdinalIgnoreCase))
                {
                    pathToMakeRelative = pathToMakeRelative.Substring(pathToMakeRelativeTo.Length);
                }
                else
                {
                    // Otherwise, we have to use the new method to identify the common root

                    // Split the path strings
                    string[] path = pathToMakeRelative.Split(System.IO.Path.DirectorySeparatorChar);
                    string[] relpath = pathToMakeRelativeTo.Split(System.IO.Path.DirectorySeparatorChar);

                    string relativepath = string.Empty;

                    // build the new path
                    int start = 0;
                    while (start < path.Length && start < relpath.Length && path[start].Equals(relpath[start], StringComparison.OrdinalIgnoreCase))
                    {
                        start++;
                    }

                    // If start is 0, they aren't on the same drive, so there is no way to make the path relative without it being absolute
                    if (start != 0)
                    {
                        // add .. for every directory left in the relative path, this is the shared root
                        for (int i = start; i < relpath.Length; i++)
                        {
                            if (relpath[i] != string.Empty)
                                relativepath += @".." + System.IO.Path.DirectorySeparatorChar;
                        }

                        // if the current relative path is still empty, and there are more than one entries left in the path,
                        // the file is in a subdirectory.  Start with ./
                        if (relativepath == string.Empty && path.Length - start > 0)
                        {
                            relativepath += @"./";
                        }

                        // add the rest of the path
                        for (int i = start; i < path.Length; i++)
                        {
                            relativepath += path[i];
                            if (i < path.Length - 1) relativepath += System.IO.Path.DirectorySeparatorChar;
                        }

                        pathToMakeRelative = relativepath;
                    }
                }
                if (pathToMakeRelative.StartsWith("\\") || pathToMakeRelative.StartsWith("/"))
                {
                    pathToMakeRelative = pathToMakeRelative.Substring(1);
                }
            }



            return pathToMakeRelative;

        }

        public static string RemoveDotDotSlash(string fileNameToFix)
        {
            fileNameToFix = fileNameToFix.Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar);

            if (fileNameToFix.Contains(".."))
            {

                // First let's get rid of any ..'s that are in the middle
                // for example:
                //
                // "content/zones/area1/../../background/outdoorsanim/outdoorsanim.achx"
                //
                // would become
                // 
                // "content/background/outdoorsanim/outdoorsanim.achx"

                var dotDotSlash = ".." + Path.DirectorySeparatorChar;

                int indexOfNextDotDotSlash = fileNameToFix.IndexOf(dotDotSlash);

                bool shouldLoop = indexOfNextDotDotSlash > 0;

                while (shouldLoop)
                {
                    int indexOfPreviousDirectory = fileNameToFix.LastIndexOf(Path.DirectorySeparatorChar, indexOfNextDotDotSlash - 2, indexOfNextDotDotSlash - 2);

                    fileNameToFix = fileNameToFix.Remove(indexOfPreviousDirectory + 1, indexOfNextDotDotSlash - indexOfPreviousDirectory + 2);

                    indexOfNextDotDotSlash = fileNameToFix.IndexOf(dotDotSlash);

                    shouldLoop = indexOfNextDotDotSlash > 0;
                }
            }

            return fileNameToFix;
        }

        #region XML Docs
        /// <summary>
        /// Returns the fileName without an extension, or makes no changes if fileName has no extension.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <returns>The file name with extension removed if an extension existed.</returns>
        #endregion
        public static string RemoveExtension(string fileName)
        {
            int extensionLength = GetExtension(fileName).Length;

            if (extensionLength == 0)
                return fileName;

            if (fileName.Length > extensionLength && fileName[fileName.Length - (extensionLength + 1)] == '.')
                return fileName.Substring(0, fileName.Length - (extensionLength + 1));
            else
                return fileName;
        }


        #region XML Docs
        /// <summary>
        /// Modifies the fileName by removing its path, or makes no changes if the fileName has no path.
        /// </summary>
        /// <param name="fileName">The file name to change</param>
        #endregion
        public static void RemovePath(ref string fileName)
        {
            int indexOf1 = fileName.LastIndexOf('/', fileName.Length - 1, fileName.Length);
            if (indexOf1 == fileName.Length - 1 && fileName.Length > 1)
            {
                indexOf1 = fileName.LastIndexOf('/', fileName.Length - 2, fileName.Length - 1);
            }

            int indexOf2 = fileName.LastIndexOf('\\', fileName.Length - 1, fileName.Length);
            if (indexOf2 == fileName.Length - 1 && fileName.Length > 1)
            {
                indexOf2 = fileName.LastIndexOf('\\', fileName.Length - 2, fileName.Length - 1);
            }


            if (indexOf1 > indexOf2)
                fileName = fileName.Remove(0, indexOf1 + 1);
            else if (indexOf2 != -1)
                fileName = fileName.Remove(0, indexOf2 + 1);
        }

        #region XML Docs
        /// <summary>
        /// Returns the fileName without a path, or makes no changes if the fileName has no path.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <returns>The modified fileName if a path is found.</returns>
        #endregion
        public static string RemovePath(string fileName)
        {
            RemovePath(ref fileName);

            return fileName;
        }



        public static string Standardize(string fileName, bool preserveCase = false, bool makeAbsolute = false)
        {
            // Justin Johnson 6/6/2017:
            // This used to normalize everything to backslashes, which is
            // the opposite of FRB and breaks Mac and other platforms.
            // Method revised to standardize on forward slash

            var newFileName = fileName;

            if (makeAbsolute)
            {
                if (IsRelative(newFileName))
                {
                    newFileName = Path.Combine(RelativeDirectory, newFileName);
                }
            }

            if (!preserveCase)
            {
                newFileName = newFileName.ToLower();
            }

            // normalize slash direction
            newFileName = newFileName.Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar);

            if (newFileName.Contains(".." + Path.DirectorySeparatorChar))
            {
                newFileName = RemoveDotDotSlash(newFileName);
            }

            return newFileName;
        }


        public static T XmlDeserialize<T>(string fileName)
        {
            T objectToReturn = default(T);


            //if (FileManager.IsRelative(fileName))
            //    fileName = FileManager.RelativeDirectory + fileName;




            //ThrowExceptionIfFileDoesntExist(fileName);


            if (IsMobile)
            {
                // Mobile platforms don't like ./ at the start of the file name, but that's what we use to identify an absolute path
                fileName = TryRemoveLeadingDotSlash(fileName);
            }


            using (Stream stream = GetStreamForFile(fileName))
            {
                try
                {
                    objectToReturn = XmlDeserializeFromStream<T>(stream);
                }
                catch (Exception e)
                {
                    throw new IOException("Could not deserialize the XML file"
                        + Environment.NewLine + fileName, e);
                }
                stream.Close();
            }

            return objectToReturn;
        }

        static string TryRemoveLeadingDotSlash(string fileName)
        {
            if (fileName != null && fileName.Length > 1 && fileName[0] == '.' && (fileName[1] == '/' || fileName[1] == '\\'))
            {
                fileName = fileName.Substring(2);
            }
            return fileName;
        }

        public static Stream GetStreamForFile(string fileName)
        {

            try
            {

#if ANDROID || IOS
                fileName = TryRemoveLeadingDotSlash(fileName);
			    return Microsoft.Xna.Framework.TitleContainer.OpenStream(fileName);
#else
                if (CustomGetStreamFromFile != null)
                {
                    return CustomGetStreamFromFile(fileName);
                }
                else
                {
                    if (IsRelative(fileName))
                    {
                        fileName = FileManager.MakeAbsolute(fileName);
                    }

                    fileName = fileName.Replace('\\', Path.DirectorySeparatorChar)
                        .Replace('/', Path.DirectorySeparatorChar);

                    return System.IO.File.OpenRead(fileName);
                }
#endif
            }
            catch (Exception e)
            {
                throw new IOException("Could not get the stream for the file " + fileName, e);
            }
        }

        public static T XmlDeserializeFromStream<T>(Stream stream)
        {
            Type type = typeof(T);

            XmlSerializer serializer = GetXmlSerializer(type);

            T objectToReturn = (T)serializer.Deserialize(stream);

            return objectToReturn;
        }



        public static XmlSerializer GetXmlSerializer(Type type)
        {
            lock (mXmlSerializers)
            {
                if (mXmlSerializers.ContainsKey(type))
                {
                    return mXmlSerializers[type];
                }
                else
                {

                    // For info on this block, see:
                    // http://stackoverflow.com/questions/1127431/xmlserializer-giving-filenotfoundexception-at-constructor
#if DEBUG
                    XmlSerializer newSerializer = XmlSerializer.FromTypes(new[] { type })[0];
#else
                XmlSerializer newSerializer = null;
                newSerializer = new XmlSerializer(type);
#endif
                    mXmlSerializers.Add(type, newSerializer);
                    return newSerializer;
                }
            }
        }



        public static bool DoesFileHaveSvnConflict(string fileName)
        {
            string fileContents = FromFileText(fileName);
            // This is how SVN marks conflicts, and we want to check for both <<<<<<< and ======= just in case one or the other happens to appear in a string somewhere.
            return fileContents.Contains("<<<<<<<") && fileContents.Contains("=======");
        }


        public static void XmlSerialize<T>(T objectToSerialize, out string stringToSerializeTo)
        {
            MemoryStream memoryStream = new MemoryStream();

            XmlSerializer serializer = GetXmlSerializer(typeof(T));

            serializer.Serialize(memoryStream, objectToSerialize);


#if MONODROID

            byte[] asBytes = memoryStream.ToArray();

            stringToSerializeTo = System.Text.Encoding.UTF8.GetString(asBytes, 0, asBytes.Length);
#else

            stringToSerializeTo = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
#endif

        }
        #endregion
    }


    public static partial class FileManager
    {
        public static string UserApplicationData =>
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\";

        public static void CopyFilesRecursively(string source, string target)
        {
            DirectoryInfo sourceDirectory = new DirectoryInfo(source);
            DirectoryInfo targetDirectory = new DirectoryInfo(target);

            CopyFilesRecursively(sourceDirectory, targetDirectory);
        }
        public static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
        {
            if (!Directory.Exists(target.FullName))
            {
                Directory.CreateDirectory(target.FullName);
            }

            foreach (DirectoryInfo dir in source.GetDirectories())
            {
                CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
            }
            foreach (FileInfo file in source.GetFiles())
            {
                file.CopyTo(Path.Combine(target.FullName, file.Name), true);
            }
        }
        private static void CreateDirectory(string targetFileName)
        {
            string directory = FileManager.GetDirectory(targetFileName);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        public static void DeleteDirectory(string dir)
        {
            System.IO.DirectoryInfo info = new System.IO.DirectoryInfo(dir);
            if (!info.Exists) return;

            // Set the folder to NORMAL so it doesn't retain READONLY or ARCHIVE attributes
            // This happens because of OneDrive
            // Shouldn't matter if we change the attributes, we're going to delete the folder anyways
            info.Attributes = FileAttributes.Normal;

            string[] files = System.IO.Directory.GetFiles(dir);
            for (int i = 0; i < files.Length; i++)
            {
                System.IO.File.Delete(files[i]);
            }

            string[] dirs = System.IO.Directory.GetDirectories(dir);
            for (int i = 0; i < dirs.Length; i++)
            {
                DeleteDirectory(dirs[i]);
            }


            System.IO.Directory.Delete(dir);
        }

        private static bool DeleteTargetFileIfExists(string targetFileName)
        {
            bool succeeded = true;

            if (FileManager.FileExists(targetFileName))
            {
                File.Delete(targetFileName);
            }
            return succeeded;
        }

        public static string FindAndAddExtension(string fileName)
        {
            fileName = Standardize(fileName);
            // This takes a little bit of work
            string fileWithoutExtension = FileManager.RemoveExtension(fileName);

            List<string> filesInDirectory = GetAllFilesInDirectory(
                FileManager.GetDirectory(fileName),
                null,
                0);

            for (int i = 0; i < filesInDirectory.Count; i++)
            {
                if (filesInDirectory[i] == fileName ||
                    FileManager.RemoveExtension(filesInDirectory[i]) == fileWithoutExtension)
                {
                    return filesInDirectory[i];
                }
            }

            return fileName;
        }
        public static byte[] FromFileBytes(string fileName)
        {
            byte[] bytesToReturn = null;
            using (FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                bytesToReturn = new byte[fileStream.Length];

                fileStream.Read(bytesToReturn, 0, bytesToReturn.Length);
            }

            return bytesToReturn;

        }
        /// <summary>
        /// Returns a List containing all files which match the fileType argument which are 
        /// in the directory argument or a subfolder.  This recurs, returning all files.
        /// </summary>
        /// <param name="directory">String representing the directory to search.  If an empty
        /// string is passed, the method will search starting in the directory holding the .exe.</param>
        /// <param name="fileType">The file type to search for specified as an extension.  The extension
        /// can either have a period or not.  That is ".jpg" and "jpg" are both valid fileType arguments.  An empty
        /// or null value for this parameter will return all files regardless of file type.</param>
        /// <returns>A list containing all of the files found which match the fileType.</returns>
        public static List<string> GetAllFilesInDirectory(string directory, string fileType)
        {
            return GetAllFilesInDirectory(directory, fileType, int.MaxValue);

        }

        /// <summary>
        /// Returns a List containing all files which match the fileType argument which are within
        /// the depthToSearch folder range relative to the directory argument.
        /// </summary>
        /// <param name="directory">String representing the directory to search.  If an empty
        /// string is passed, the method will search starting in the directory holding the .exe.</param>
        /// <param name="fileType">The file type to search for specified as an extension.  The extension
        /// can either have a period or not.  That is ".jpg" and "jpg" are both valid fileType arguments.  An empty
        /// or null value for this parameter will return all files regardless of file type.</param>
        /// <param name="depthToSearch">The depth to search through.  If the depthToSearch
        /// is 0, only the argument directory will be searched. To search infinte depth, use int.MaxValue</param>
        /// <returns>A list containing all of the files found which match the fileType.</returns>
        public static List<string> GetAllFilesInDirectory(string directory, string fileType, int depthToSearch)
        {
            List<string> arrayToReturn = new List<string>();

            GetAllFilesInDirectory(directory, fileType, depthToSearch, arrayToReturn);

            return arrayToReturn;
        }


        public static void GetAllFilesInDirectory(string directory, string fileType, int depthToSearch, List<string> arrayToReturn)
        {
            if (!Directory.Exists(directory))
            {
                return;
            }
            //if (directory == "")
            //    directory = mRelativeDirectory;

            if (directory.EndsWith(@"\") == false && directory.EndsWith("/") == false)
            {
                directory += @"/";
            }

            // if they passed in a fileType which begins with a period (like ".jpg"), then
            // remove the period so only the extension remains.  That is, convert
            // ".jpg" to "jpg"
            if (fileType != null && fileType.Length > 0 && fileType[0] == '.')
                fileType = fileType.Substring(1);

            string[] files = System.IO.Directory.GetFiles(directory);
            string[] directories = System.IO.Directory.GetDirectories(directory);

            if (string.IsNullOrEmpty(fileType))
            {
                for (int i = 0; i < files.Length; i++)
                {
                    files[i] = FileManager.Standardize(files[i]);
                }
                arrayToReturn.AddRange(files);
            }
            else
            {
                int fileCount = files.Length;

                for (int i = 0; i < fileCount; i++)
                {
                    string file = files[i];
                    if (GetExtension(file) == fileType)
                    {
                        arrayToReturn.Add(file);
                    }
                }
            }


            if (depthToSearch > 0)
            {
                int directoryCount = directories.Length;
                for (int i = 0; i < directoryCount; i++)
                {
                    string directoryChecking = directories[i];

                    GetAllFilesInDirectory(directoryChecking, fileType, depthToSearch - 1, arrayToReturn);
                }
            }
        }

        public static Stream GetStreamFromEmbeddedResource(Assembly assembly, string resourceName)
        {
            if (string.IsNullOrEmpty(resourceName))
            {
                throw new NullReferenceException("ResourceName must not be null - can't get the byte array for resource");
            }

            if (assembly == null)
            {
                throw new NullReferenceException("Assembly is null, so can't find the resource\n" + resourceName);
            }

            Stream resourceStream = assembly.GetManifestResourceStream(resourceName);

            if (resourceStream == null)
            {
                string message = "Could not find a resource stream for\n" + resourceName + "\n but found " +
                    "the following names:\n\n";

                foreach (string containedResource in assembly.GetManifestResourceNames())
                {
                    message += containedResource + "\n";
                }


                throw new NullReferenceException(message);
            }

            return resourceStream;
        }

        public static string GetStringFromEmbeddedResource(Assembly assembly, string resourceName)
        {
            var resourceStream = GetStreamFromEmbeddedResource(assembly, resourceName);

            using (resourceStream)
            {
                using (StreamReader reader = new StreamReader(resourceStream))
                {
                    string text = reader.ReadToEnd();
                    return text;
                }
            }
        }


        public static byte[] GetByteArrayFromEmbeddedResource(Assembly assembly, string resourceName)
        {
            if (string.IsNullOrEmpty(resourceName))
            {
                throw new NullReferenceException("ResourceName must not be null - can't get the byte array for resource");
            }

            if (assembly == null)
            {
                throw new NullReferenceException("Assembly is null, so can't find the resource\n" + resourceName);
            }

            Stream resourceStream = assembly.GetManifestResourceStream(resourceName);

            if (resourceStream == null)
            {
                string message = "Could not find a resource stream for\n" + resourceName + "\n but found " +
                    "the following names:\n\n";

                foreach (string containedResource in assembly.GetManifestResourceNames())
                {
                    message += containedResource + "\n";
                }


                throw new NullReferenceException(message);
            }

            using (resourceStream)
            {
                byte[] buffer = new byte[resourceStream.Length];
                resourceStream.Read(buffer, 0, buffer.Length);
                resourceStream.Close();
                resourceStream.Dispose();
                return buffer;
            }
        }

        static string GetProperDirectoryCapitalization(DirectoryInfo dirInfo)
        {
            DirectoryInfo parentDirInfo = dirInfo.Parent;
            if (null == parentDirInfo)
                return dirInfo.Name;
            return Path.Combine(GetProperDirectoryCapitalization(parentDirInfo),
                                parentDirInfo.GetDirectories(dirInfo.Name)[0].Name);
        }
        static string GetProperFilePathCapitalization(string filename)
        {
            if (FileManager.FileExists(filename))
            {
                FileInfo fileInfo = new FileInfo(filename);
                DirectoryInfo dirInfo = fileInfo.Directory;
                return Path.Combine(GetProperDirectoryCapitalization(dirInfo),
                                    dirInfo.GetFiles(fileInfo.Name)[0].Name);
            }
            else
            {
                // August 8, 2011
                // We used to return
                // null here but I'm not
                // sure why that is a good
                // idea.  If we can't recast
                // we should just return the original
                // name.
                return filename;
            }
        }
        public static string GetRecastedFileName(string fileName)
        {
            return GetProperFilePathCapitalization(fileName);
        }
        public static string GetRootObjectType(string fileName)
        {
            if (!File.Exists(fileName))
            {
                throw new ArgumentException("Could not find the file " + fileName, "fileName");

            }

            string typeToReturn = null;

            using (FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (System.IO.StreamReader sr = new StreamReader(fileStream))
                {
                    sr.ReadLine(); // this is the version and encoding line
                    string line = sr.ReadLine();

                    typeToReturn = StringFunctions.GetWordAfter("<", line);
                    sr.Close();
                }
            }

            return typeToReturn;
        }

        public static string MyDocuments
        {
            get { return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\"; }
        }

        public static void SaveByteArray(byte[] whatToSave, string fileName)
        {
            if (FileManager.FileExists(fileName))
            {
                File.Delete(fileName);
            }

            Directory.CreateDirectory(FileManager.GetDirectory(fileName));

            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    bw.Write(whatToSave);
                    bw.Close();
                    fs.Close();
                }
            }

        }


        public static void SaveEmbeddedResource(Assembly assembly, string resourceName, string targetFileName)
        {

            CreateDirectory(targetFileName);

            byte[] buffer = GetByteArrayFromEmbeddedResource(assembly, resourceName);

            bool succeeded = DeleteTargetFileIfExists(targetFileName);

            WriteStreamToFile(targetFileName, buffer, succeeded);
        }

        private static void WriteStreamToFile(string targetFileName, byte[] buffer, bool succeeded)
        {
            if (succeeded)
            {
                using (FileStream fs = new FileStream(targetFileName, FileMode.Create))
                {
                    using (BinaryWriter bw = new BinaryWriter(fs))
                    {
                        bw.Write(buffer);
                        bw.Close();
                        fs.Close();
                    }
                }
            }
        }


        public static void SaveText(string stringToSave, string fileName)
        {
            SaveText(stringToSave, fileName, Encoding.UTF8);
        }

        public static void SaveText(string stringToSave, string fileName, Encoding encoding)
        {
            FileInfo fileInfo = new FileInfo(fileName);

            if (!string.IsNullOrEmpty(FileManager.GetDirectory(fileName)) &&
                !Directory.Exists(FileManager.GetDirectory(fileName)))
            {
                Directory.CreateDirectory(FileManager.GetDirectory(fileName));
            }

            if (encoding == Encoding.UTF8)
            {
                StreamWriter writer = fileInfo.CreateText();

                writer.Write(stringToSave);

                writer.Close();
            }
            else
            {
                if (FileManager.FileExists(fileName))
                {
                    File.Delete(fileName);
                }

                using (FileStream fileStream = fileInfo.OpenWrite())
                {

                    byte[] bytes = encoding.GetBytes(stringToSave);

                    fileStream.Write(bytes, 0, bytes.Length);
                }

            }
        }


        /// <summary>
        /// Returns a full path for a folder which can be used to save user data. 
        /// </summary>
        /// <example>
        /// System.IO.File.SaveText("File Contents", FlatRedBall.UserApplicationDataForThisApplication + "File.txt");
        /// </example>
        public static string UserApplicationDataForThisApplication
        {
            get
            {
                string applicationDataName = Assembly.GetEntryAssembly().FullName;

                applicationDataName = applicationDataName.Substring(0, applicationDataName.IndexOf(','));

                return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\" + applicationDataName + @"\";
            }
        }

        public static void XmlSerialize(Type type, object objectToSerialize, string fileName)
        {
            // Make sure that the directory for the file exists
            string directory = FileManager.GetDirectory(fileName);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(FileManager.GetDirectory(fileName));
            }
            XmlSerializer serializer = GetXmlSerializer(type);

            // for info on why we do this:
            // https://stackoverflow.com/questions/8515720/how-to-prevent-system-io-file-open-with-filemode-truncate-from-causing-a-file-ch
            if (System.IO.File.Exists(fileName))
            {
                System.IO.File.Delete(fileName);
            }

            using (var fs = System.IO.File.Open(fileName, FileMode.OpenOrCreate))
            {
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                using (var writer = XmlWriter.Create(fs, settings))
                {
                    // for info on this, see
                    // http://stackoverflow.com/questions/1127431/xmlserializer-giving-filenotfoundexception-at-constructor

                    serializer.Serialize(writer, objectToSerialize);

                }
            }
        }

        public static void XmlSerialize<T>(T objectToSerialize, string fileName)
        {
            XmlSerialize(typeof(T), objectToSerialize, fileName);
        }


        public static T XmlDeserializeEmbeddedResource<T>(Assembly assembly, string location)
        {
            T objectToReturn = default(T);
            Type type = typeof(T);

            using (Stream resourceStream = assembly.GetManifestResourceStream(location))
            {
                XmlSerializer serializer = GetXmlSerializer(type);
                objectToReturn = (T)serializer.Deserialize(resourceStream);
                resourceStream.Close();

            }

            return objectToReturn;
        }

        public static bool IsUrl(string fileName)
        {
            return fileName.IndexOf("http:") == 0 || fileName.IndexOf("https:") == 0;
        }
    }
}
