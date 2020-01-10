using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenderingLibrary.Graphics
{
    public enum NineSliceSections
    {
        TopLeft,
        Top,
        TopRight,
        Left,
        Center,
        Right,
        BottomLeft,
        Bottom,
        BottomRight
    }

    public static class NineSliceExtensions
    {
        public static string[] PossibleNineSliceEndings
        {
            get;
            private set;
        }

        static NineSliceExtensions()
        {
            PossibleNineSliceEndings = new string[9];
            PossibleNineSliceEndings[(int)NineSliceSections.Center] = "_center";
            PossibleNineSliceEndings[(int)NineSliceSections.Left] = "_left";
            PossibleNineSliceEndings[(int)NineSliceSections.Right] = "_right";
            PossibleNineSliceEndings[(int)NineSliceSections.TopLeft] = "_topLeft";
            PossibleNineSliceEndings[(int)NineSliceSections.Top] = "_topCenter";
            PossibleNineSliceEndings[(int)NineSliceSections.TopRight] = "_topRight";
            PossibleNineSliceEndings[(int)NineSliceSections.BottomLeft] = "_bottomLeft";
            PossibleNineSliceEndings[(int)NineSliceSections.Bottom] = "_bottomCenter";
            PossibleNineSliceEndings[(int)NineSliceSections.BottomRight] = "_bottomRight";
        }

        public static bool GetIfShouldUsePattern(string absoluteTexture)
        {
            bool usePattern = false;

            string withoutExtension = RemoveExtension(absoluteTexture);
            foreach (var kvp in PossibleNineSliceEndings)
            {
                if (withoutExtension.EndsWith(kvp, StringComparison.OrdinalIgnoreCase))
                {
                    usePattern = true;
                    break;
                }
            }
            return usePattern;
        }



        public static string GetBareTextureForNineSliceTexture(string absoluteTexture)
        {
            string extension = GetExtension(absoluteTexture);

            string withoutExtension = RemoveExtension(absoluteTexture);

            string toReturn = withoutExtension;

            foreach (var kvp in PossibleNineSliceEndings)
            {
                if (withoutExtension.EndsWith(kvp, StringComparison.OrdinalIgnoreCase))
                {
                    toReturn = withoutExtension.Substring(0, withoutExtension.Length - kvp.Length);
                    break;
                }
            }

            // No extensions, because we'll need to append that
            //toReturn += "." + extension;

            return toReturn;
        }

        // local method for portability:
        static string RemoveExtension(string fileName)
        {
            int extensionLength = GetExtension(fileName).Length;

            if (extensionLength == 0)
                return fileName;

            if (fileName.Length > extensionLength && fileName[fileName.Length - (extensionLength + 1)] == '.')
                return fileName.Substring(0, fileName.Length - (extensionLength + 1));
            else
                return fileName;
        }

        static string GetExtension(string fileName)
        {
            try
            {
                if (fileName == null)
                {
                    return "";
                }


                int i = fileName.LastIndexOf('.');
                if (i != -1)
                {
                    bool hasDotSlash = false;

                    if (i == fileName.Length - 1)
                    {
                        return "";
                    }

                    if (i < fileName.Length + 1 && (fileName[i + 1] == '/' || fileName[i + 1] == '\\'))
                    {
                        hasDotSlash = true;
                    }

                    if (hasDotSlash)
                    {
                        return "";
                    }
                    else
                    {
                        return fileName.Substring(i + 1, fileName.Length - (i + 1)).ToLower();
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
    }
}
