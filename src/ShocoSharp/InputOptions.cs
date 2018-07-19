using System;

namespace ShocoSharp
{
    /// <summary>
    /// Model generator options for processing input chunks
    /// </summary>
    [Flags]
    public enum InputOptions
    {
        /// <summary>
        /// Read the input in its raw format
        /// </summary>
        None = 0,
        /// <summary>
        /// Split the input into chunks at each new line
        /// </summary>
        SplitNewLine = 1,
        /// <summary>
        /// Split the input into chunks at any whitespace character
        /// </summary>
        SplitWhitespaceAndNewLine = 2 | SplitNewLine,
        /// <summary>
        /// Remove leading and trailing whitespace characters from each chunk
        /// </summary>
        StripWhitespace = 4,
        /// <summary>
        /// Remove leading and trailing punctuation characters from each chunk
        /// </summary>
        StripPunctuation = 8,
        /// <summary>
        /// Remove leading and trailing whitespace and punctuation characters from each chunk
        /// </summary>
        StripWhitespaceAndPunctuation = StripWhitespace | StripPunctuation,
        /// <summary>
        /// Splits the input into chunks at each new line and removes leading and trailing whitespace characters from each chunk
        /// </summary>
        Default = SplitNewLine | StripWhitespace
    }
}
