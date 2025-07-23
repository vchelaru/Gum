using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace ToolsUtilities
{
    internal static class StringFunctions
    {
        #region Fields

        static char[] sValidNumericalChars = { '-', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.', ',' };
        static char[] sWhitespaceChars = new char[] { ' ', '\n', '\t', '\r' };

        #endregion


        #region XML Docs
        /// <summary>
        /// Returns the first integer found after the argument stringToSearchFor in whereToSearch.
        /// </summary>
        /// <remarks>
        /// This method is used to help simplify parsing of text files and data strings.
        /// If stringToSearchFor is "Y:" and whereToSearch is "X: 30, Y:32", then the value
        /// of 32 will be returned.
        /// </remarks>
        /// <param name="stringToSearchFor">The string pattern to search for.</param>
        /// <param name="whereToSearch">The string that will be searched.</param>
        /// <returns>The integer value found after the argument stringToSearchFor.</returns>
        #endregion
        public static int GetIntAfter(string stringToSearchFor, string whereToSearch)
        {
            return GetIntAfter(stringToSearchFor, whereToSearch, 0);
        }

        #region XML Docs
        /// <summary>
        /// Returns the first integer found after the argument stringToSearchFor.  The search begins
        /// at the argument startIndex.
        /// </summary>
        /// <param name="stringToSearchFor">The string pattern to search for.</param>
        /// <param name="whereToSearch">The string that will be searched.</param>
        /// <param name="startIndex">The index to begin searching at.  This method
        /// will ignore any instances of stringToSearchFor which begin at an index smaller
        /// than the argument startIndex.</param>
        /// <returns></returns>
        #endregion
        public static int GetIntAfter(string stringToSearchFor, string whereToSearch, int startIndex)
        {
            int startOfNumber = -1;
            int endOfNumber = -1;
            int enterAt = -1;
            int carriageReturn = -1;
            string substring = "uninitialized";

            try
            {
                int indexOf = whereToSearch.IndexOf(stringToSearchFor, startIndex);
                if (indexOf != -1)
                {
                    startOfNumber = indexOf + stringToSearchFor.Length;
                    endOfNumber = whereToSearch.IndexOf(' ', startOfNumber);
                    enterAt = whereToSearch.IndexOf('\n', startOfNumber);

                    carriageReturn = whereToSearch.IndexOf('\r', startOfNumber);
                    if (carriageReturn != -1 && carriageReturn < enterAt)
                        enterAt = whereToSearch.IndexOf('\r', startOfNumber);

                    if (endOfNumber == -1)
                        endOfNumber = whereToSearch.Length;

                    for (int i = startOfNumber; i < endOfNumber; i++)
                    {
                        bool found = false;
                        for (int indexInArray = 0; indexInArray < sValidNumericalChars.Length; indexInArray++)
                        {
                            if (whereToSearch[i] == sValidNumericalChars[indexInArray])
                            {
                                found = true;
                                break;
                            }
                        }

                        if (!found)
                        {
                            // if we got here, then the character is not valid, so end the string
                            endOfNumber = i;
                            break;
                        }

                    }


                    if (endOfNumber == -1 || (enterAt != -1 && enterAt < endOfNumber))
                        endOfNumber = enterAt;
                    if (endOfNumber == -1)
                        endOfNumber = whereToSearch.Length;

                    substring = whereToSearch.Substring(startOfNumber,
                        endOfNumber - startOfNumber);

                    // this method is called when reading from a file.  
                    // usually, files use the . rather than other numerical formats, so if this fails, just use the regular . format
                    int toReturn = int.Parse(substring);
                    return toReturn;

                }
            }
            catch (System.FormatException)
            {
                return int.Parse(substring, System.Globalization.NumberFormatInfo.InvariantInfo);

            }

            return 0;
        }

        public static int GetIntAfter(string stringToSearchFor, string whereToSearch, ref int startIndex)
        {
            int startOfNumber = -1;
            int endOfNumber = -1;

#if NET6_0_OR_GREATER
            ReadOnlySpan<char> span = null;
#else
            string substring = string.Empty;
#endif

            try
            {
                int indexOf = whereToSearch.IndexOf(stringToSearchFor, startIndex);

                if (indexOf != -1)
                {
                    startOfNumber = indexOf + stringToSearchFor.Length;
                    endOfNumber = startIndex;

                    startIndex = startOfNumber;
                    for (int i = startOfNumber; i < whereToSearch.Length; i++)
                    {
                        startIndex = i + 1;
                        if (!sValidNumericalChars.Contains(whereToSearch[i]))
                        {
                            break;
                        }
                        else
                        {
                            endOfNumber = i + 1;
                        }
                    }

                    if (endOfNumber > startOfNumber)
                    {
#if NET6_0_OR_GREATER
                        span = whereToSearch.AsSpan(startOfNumber, endOfNumber - startOfNumber);
                        int toReturn = int.Parse(span);
#else
                        substring = whereToSearch.Substring(startOfNumber, endOfNumber - startOfNumber);
                        // this method is called when reading from a file.  
                        // usually, files use the . rather than other numerical formats, so if this fails, just use the regular . format
                        int toReturn = int.Parse(substring);
#endif

                        return toReturn;
                    }
                }
            }
            catch (System.FormatException)
            {
#if NET6_0_OR_GREATER
                return int.Parse(span, System.Globalization.NumberStyles.Integer, System.Globalization.NumberFormatInfo.InvariantInfo);
#else
                return int.Parse(substring, System.Globalization.NumberFormatInfo.InvariantInfo);
#endif
            }
            return 0;
        }


        public static string GetWordAfter(string stringToStartAfter, string entireString)
		{
            return GetWordAfter(stringToStartAfter, entireString, 0);
        }

        public static string GetWordAfter(string stringToStartAfter, string entireString, int startIndex)
        {
            int indexOf = entireString.IndexOf(stringToStartAfter, startIndex);
			if (indexOf != -1)
			{
				int startOfWord = indexOf + stringToStartAfter.Length;

                while (sWhitespaceChars.Contains(entireString[startOfWord]) && startOfWord < entireString.Length)
                {
                    startOfWord++;
                }

                int endOfWord = entireString.IndexOfAny(sWhitespaceChars, startOfWord);

                if (endOfWord == -1 && startOfWord < entireString.Length - 1)
                {
                    endOfWord = entireString.Length;
                }

				return entireString.Substring(startOfWord, endOfWord - startOfWord);
			}
			else
			{
				return null;
			}

		}

		public static string WildcardToRegex(string pattern)
		{
			return "^" + Regex.Escape(pattern).
			 Replace("\\*", ".*").
			 Replace("\\?", ".") + "$";
		}

        public static bool AreListsEqual(List<string> first, List<string> second)
        {
            if (first == second)
            {
                return true;
            }

            if ((first == null && second != null) ||
                (first != null && second == null))
            {
                return false;
            }

            if (first.Count != second.Count)
            {
                return false;
            }

            for (int i = 0; i < first.Count; i++)
            {
                if (first[i] != second[i])
                {
                    return false;
                }
            }

            return true;
        }

        public static bool AreArraysEqual(string[] first, string[] second)
        {
            if (first == second)
            {
                return true;
            }

            if ((first == null && second != null) ||
                (first != null && second == null))
            {
                return false;
            }

            if (first.Length != second.Length)
            {
                return false;
            }

            for (int i = 0; i < first.Length; i++)
            {
                if (first[i] != second[i])
                {
                    return false;
                }
            }

            return true;
        }

        public static bool Contains<T>(this T[] values, T value)
        {
            return Array.IndexOf(values, value) != -1;
        }

        public static string MakeStringUnique(string stringToMakeUnique, IEnumerable<string> strings)
        {
            while (strings.Contains(stringToMakeUnique))
                stringToMakeUnique = IncrementNumberAtEnd(stringToMakeUnique);

            return stringToMakeUnique;
        }

        #region XML Docs
        /// <summary>
        /// Increments the number at the end of a string or adds a number if none exists.
        /// </summary>
        /// <remarks>
        /// This method begins looking at the end of a string for numbers and moves towards the beginning of the string
        /// until it encounters a character which is not a numerical digit or the beginning of the string.  "Sprite123" would return
        /// "Sprite124", and "MyString" would return "MyString1".
        /// </remarks>
        /// <param name="originalString">The string to "increment".</param>
        /// <returns>Returns a string with the number at the end incremented, or with a number added on the end if none existed before.</returns>
        #endregion
        public static string IncrementNumberAtEnd(string originalString)
        {
            if (string.IsNullOrEmpty(originalString))
            {
                return "1";
            }

            // first we get the number at the end of the string

            // start at the end of the string and move backwards until reacing a non-Digit.
            int letterChecking = originalString.Length;
            do
            {
                letterChecking--;
            } while (letterChecking > -1 && Char.IsDigit(originalString[letterChecking]));


            if (letterChecking == originalString.Length - 1 && !Char.IsDigit(originalString[letterChecking]))
            {
                // we don't have a number there, so let's add one
                originalString = originalString + ((int)1).ToString();
                return originalString;
            }
            string numAtEnd = originalString.Substring(letterChecking + 1, originalString.Length - letterChecking - 1);
            string baseString = originalString.Remove(letterChecking + 1, originalString.Length - letterChecking - 1);
            int numAtEndAsInt = System.Convert.ToInt32(numAtEnd);
            numAtEndAsInt++;
            return baseString + numAtEndAsInt.ToString();

        }

        #region XML Docs
        /// <summary>
        /// Inserts spaces before every capital letter in a camel-case
        /// string.  Ignores the first letter.
        /// </summary>
        /// <remarks>
        /// For example "HelloThereIAmCamelCase" becomes
        /// "Hello There I Am Camel Case".
        /// </remarks>
        /// <param name="originalString">The string in which to insert spaces.</param>
        /// <returns>The string with spaces inserted.</returns>
        #endregion
        public static string InsertSpacesInCamelCaseString(string originalString)
        {
            // Normally in reverse loops you go til i > -1, but 
            // we don't want the character at index 0 to be tested.
            for (int i = originalString.Length - 1; i > 0; i--)
            {
                if (char.IsUpper(originalString[i]) && i != 0
                    // make sure there's not already a space there
                    && originalString[i-1] != ' '
                    )
                {
                    originalString = originalString.Insert(i, " ");
                }
            }

            return originalString;
        }

        private static char[] validHexCharacters = new char[] {
            '0',
            '1',
            '2',
            '3',
            '4',
            '5',
            '6',
            '7',
            '8',
            '9',
            'a','A',
            'b','B',
            'c','C',
            'd','D',
            'e','E',
            'f','F'
        };

        public static bool IsValidHex(string stringWithout0x)
        {
            if (string.IsNullOrEmpty(stringWithout0x))
            {
                return false;
            }

            for (int i = 0; i < stringWithout0x.Length; i++)
            {
                if (validHexCharacters.Contains(stringWithout0x[i]) == false)
                {
                    return false;
                }
            }

            if (stringWithout0x.Length > 8)
            {
                return false;
            }

            return true;
        }

        public static int CountOf(this string whatToLookFor, string entireString)
        {
            if (string.IsNullOrEmpty(entireString))
            {
                return 0;
            }

            int toReturn = 0;
            int currentIndex = 0;
            while (true)
            {
                int foundIndex = entireString.IndexOf(whatToLookFor, currentIndex);

                if (foundIndex == -1)
                {
                    break;
                }
                else
                {
                    toReturn++;
                    currentIndex = foundIndex + whatToLookFor.Length;
                }
            }

            return toReturn;
        }

        public static int CountOf(this string instanceToSearchIn, char characterToSearchFor)
        {
            return instanceToSearchIn.CountOf(characterToSearchFor, 0, instanceToSearchIn.Length);
        }

        public static int CountOf(this string instanceToSearchIn, char characterToSearchFor, int startIndex, int searchLength)
        {
            int count = 0;

            for (int i = startIndex; i < searchLength; i++)
            {
                char characterAtIndex = instanceToSearchIn[i];

                if (characterAtIndex == characterToSearchFor)
                {
                    count++;
                }
            }

            return count;
        }

        public static bool ContainsNoAlloc(string containingString, char charToLookFor)
        {
            for(int i = containingString.Length-1; i > -1; i--)
            {
                if(containingString[i] == charToLookFor)
                {
                    return true;
                }
            }
            return false;
        }

        public static void ReplaceLine(ref string contents, string contentsOfLineToReplace, string whatToReplaceWith)
        {
            int startOfLine;
            int endOfLine;
            GetStartAndEndOfLineContaining(contents, contentsOfLineToReplace, out startOfLine, out endOfLine);

            if (startOfLine != -1)
            {
                contents = contents.Remove(startOfLine, endOfLine - startOfLine);
                contents = contents.Insert(startOfLine, whatToReplaceWith);
            }
        }

        public static void GetStartAndEndOfLineContaining(string contents, string whatToSearchFor, out int start, out int end)
        {
            int indexOfWhatToSearchFor = contents.IndexOf(whatToSearchFor);

            start = -1;
            end = -1;

            if (indexOfWhatToSearchFor != -1)
            {
                start = contents.LastIndexOfAny(new char[] { '\n', '\r' }, indexOfWhatToSearchFor) + 1;
                end = contents.IndexOfAny(new char[] { '\n', '\r' }, indexOfWhatToSearchFor);
            }
        }

        public static string RemoveWhitespace(string stringToRemoveWhitespaceFrom)
        {
            return stringToRemoveWhitespaceFrom.Replace(" ", "").Replace("\t", "").Replace("\n", "").Replace("\r", "");
        }

        public static bool IsNumber(string stringToCheck)
        {
            double throwaway;
            return double.TryParse(stringToCheck, out throwaway);
        }
    }
}
