using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiParser
{
    public static class BracketUtil
    {
        public static bool IsWellFormed(string @string, char open, char close)
        {
            int openBrackets = 0;

            foreach (char character in @string)
            {
                if (openBrackets < 0)
                {
                    return false;
                }
                
                if (character == open)
                {
                    openBrackets++;
                }
                else if (character == close)
                {
                    openBrackets--;
                }
            }

            return openBrackets == 0;
        }

        /// <summary>
        /// Attempts to retrieve the first inner content of the outermost brackets.
        /// </summary>
        /// <remarks>
        /// <example>
        /// "lorem ipsum[dolor sit [amet]][consectetur]" => "dolor sit [amet]"
        /// </example>
        /// </remarks>
        /// <param name="string"></param>
        /// <param name="open"></param>
        /// <param name="close"></param>
        /// <param name="result"></param>
        /// <returns>True, if the <paramref name="string"/> is well formed and contains at least one 
        /// <paramref name="open"/>ing bracket. Otherwise false.</returns>
        public static bool TryGetInnerContent(string @string, char open, char close, out string result)
        {
            result = null;
            
            bool success = TryGetInnerContents(@string, open, close, out string[] contents);

            if (!success)
            {
                return false;
            }

            result = contents?.FirstOrDefault();

            return result != null;
        }

        /// <summary>
        /// Attempts to retrieve the inner contents of the outermost brackets.
        /// </summary>
        /// <remarks>
        /// <example>
        /// "lorem ipsum[dolor sit [amet]][consectetur]" => {"dolor sit [amet]", "consectetur"}
        /// </example>
        /// </remarks>
        /// <param name="string"></param>
        /// <param name="open"></param>
        /// <param name="close"></param>
        /// <param name="result"></param>
        /// <returns>True, if the <paramref name="string"/> is well formed and contains at least one 
        /// <paramref name="open"/>ing bracket. Otherwise false.</returns>
        public static bool TryGetInnerContents(string @string, char open, char close, out string[] result)
        {
            List<string> contents = new List<string>();
            
            result = null;

            int openBrackets = 0;
            bool foundFirst = false;
            bool foundAny = false;

            int startPosition = 0;
            int endPosition = 0;

            int position = 0;

            foreach (char character in @string)
            {
                if (openBrackets < 0) // not well formed
                {
                    return false;
                }

                if (character == open)
                {
                    openBrackets++;
                    if (!foundFirst)
                    {
                        startPosition = position;
                        foundFirst = true;
                        foundAny = true;
                    }
                }
                else if (character == close)
                {
                    openBrackets--;
                }

                if (foundFirst && openBrackets == 0)
                {
                    endPosition = position;

                    contents.Add(@string.Substring(startPosition + 1, endPosition - startPosition - 1)); // without the brackets themselves

                    foundFirst = false;
                }

                position++;
            }

            if (openBrackets != 0) // not well formed
            {
                return false;
            }

            if (!foundAny) // no (opening) brackets anywhere
            {
                return false;
            }

            result = contents.ToArray();
            return true;
        }
    }
}
