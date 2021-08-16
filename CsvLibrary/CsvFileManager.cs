using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using ToolsUtilities;

namespace CsvLibrary
{
    public enum HeaderPresence
    {
        HasHeaders,
        NoHeaders
    }

    public static class CsvFileManager
    {
        public static RuntimeCsvRepresentation CsvDeserializeToRuntime(string fileName)
        {
            char delimiter = ',';
            return CsvDeserializeToRuntime(fileName, delimiter);
        }

        public static RuntimeCsvRepresentation CsvDeserializeToRuntime(string fileName, char delimiter, HeaderPresence headerPresence = HeaderPresence.HasHeaders)
        {
            if (FileManager.IsRelative(fileName))
            {
                throw new NotImplementedException();
                //fileName = FileManager.MakeAbsolute(fileName);
            }

            //FileManager.ThrowExceptionIfFileDoesntExist(fileName);


            string extension = FileManager.GetExtension(fileName);

#if SILVERLIGHT || XBOX360 || WINDOWS_PHONE
            
            Stream fileStream = FileManager.GetStreamForFile(fileName);
            
#else

            // Creating a filestream then using that enables us to open files that are open by other apps.
            FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            // StreamReader streamReader = new StreamReader(fileName);
#endif
            RuntimeCsvRepresentation runtimeCsvRepresentation = GetRuntimeCsvRepresentationFromStream(fileStream, headerPresence, true, delimiter);
            fileStream.Close();
            fileStream.Dispose();


#if XBOX360
            if (FileManager.IsFileNameInUserFolder(fileName))
            {
                FileManager.DisposeLastStorageContainer();
            }
#endif

            return runtimeCsvRepresentation;
        }

        public static RuntimeCsvRepresentation GetRuntimeCsvRepresentationFromEmbeddedResource( Assembly assembly, string location)
        {
            return GetRuntimeCsvRepresentationFromEmbeddedResource(assembly, location, HeaderPresence.HasHeaders);
        }


        public static RuntimeCsvRepresentation GetRuntimeCsvRepresentationFromEmbeddedResource(Assembly assembly, string location, HeaderPresence headerPresence, char delimiter = ',')
        {
            RuntimeCsvRepresentation toReturn = null;
            using (Stream resourceStream = assembly.GetManifestResourceStream(location))
            {
                if (resourceStream == null)
                {
                    string messageToShow = "Could not find resource stream for " + location + ".  The following names exist: ";

                    foreach(string name in assembly.GetManifestResourceNames())
                    {
                        messageToShow += "\n" + name;
                    }

                    throw new Exception(messageToShow);

                }
                toReturn = GetRuntimeCsvRepresentationFromStream(resourceStream, headerPresence, true, delimiter);
            }

            return toReturn;
        }

        public static RuntimeCsvRepresentation GetRuntimeCsvRepresentationFromStream(Stream fileStream)
        {
            return GetRuntimeCsvRepresentationFromStream(fileStream, HeaderPresence.HasHeaders, true);
        }

        public static RuntimeCsvRepresentation GetRuntimeCsvRepresentationFromStream(Stream fileStream, HeaderPresence headerPresence, bool trimEmptyLines)
        {
            char delimiter = ',';

            return GetRuntimeCsvRepresentationFromStream(fileStream, headerPresence, trimEmptyLines, delimiter);
        }

        public static RuntimeCsvRepresentation GetRuntimeCsvRepresentationFromStream(Stream stream, HeaderPresence headerPresence, bool trimEmptyLines, char delimiter)
        {
            System.IO.StreamReader streamReader = new StreamReader(stream);

            RuntimeCsvRepresentation runtimeCsvRepresentation = null;


            bool hasHeaders = headerPresence == HeaderPresence.HasHeaders;

           
            using (CsvReader csv = new CsvReader(streamReader, hasHeaders, delimiter, CsvReader.DefaultQuote, CsvReader.DefaultEscape, CsvReader.DefaultComment, true, CsvReader.DefaultBufferSize))
            {
                csv.SkipsComments = false;

                runtimeCsvRepresentation = new RuntimeCsvRepresentation();

                string[] fileHeaders = csv.GetFieldHeaders();
                runtimeCsvRepresentation.Headers = new CsvHeader[fileHeaders.Length];

                for (int i = 0; i < fileHeaders.Length; i++)
                {
                    runtimeCsvRepresentation.Headers[i] = new CsvHeader(fileHeaders[i]);
                }

                // use field count instead of header count because there may not be headers
                int numberOfHeaders = csv.FieldCount;

                runtimeCsvRepresentation.Records = new List<string[]>();

                while (csv.ReadNextRecord())
                {
                    string[] newRecord = new string[numberOfHeaders];

                    bool shouldAddRow = !trimEmptyLines;


                    for (int i = 0; i < numberOfHeaders; i++)
                    {
                        string record = csv[i];

                        newRecord[i] = record;
                        if (!string.IsNullOrEmpty(record))
                        {
                            shouldAddRow = true;
                        }
                    }

                    if (shouldAddRow)
                    {
                        runtimeCsvRepresentation.Records.Add(newRecord);
                    }

                }
            }

            // Vic says - not sure how this got here, but it causes a crash!
            //streamReader.DiscardBufferedData();
            streamReader.Close();
            streamReader.Dispose();
            return runtimeCsvRepresentation;
        }


        public static void CsvDeserializeDictionary<KeyType, ValueType>(string fileName, Dictionary<KeyType, ValueType> dictionaryToPopulate, out RuntimeCsvRepresentation rcr)
        {
            rcr = CsvDeserializeToRuntime(fileName);

            rcr.FillObjectDictionary<KeyType, ValueType>(dictionaryToPopulate, "Global");
        }



    }
}
