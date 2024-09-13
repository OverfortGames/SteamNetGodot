using System.Text.RegularExpressions;

namespace OverfortGames.SteamNetGodot
{
    public static class NameChecker
    {
        public enum ErrorMessage
        {
            None,
            InvalidCharPattern,
            MultipleSpaces,
            InvalidLenght,
            Empty
        }

        // Method to check if the string contains only a-z, is less than 18 characters, and doesn't have multiple spaces
        public static bool IsValid(string input, out ErrorMessage errorMessage, int maxLenght, int maxSpaces = 1)
        {
            errorMessage = ErrorMessage.None;

            // Regular expression to match any character that is not a-z
            string invalidCharPattern = @"[^a-zA-Z ']+";

            // Check if the string contains any invalid characters
            bool hasInvalidChars = Regex.IsMatch(input, invalidCharPattern);

            bool hasMoreThanAllowedSpaces = false;
            if (maxSpaces >= 0)
            {
                // Check the number of spaces in the input
                int spaceCount = input.Split(' ').Length - 1;
                hasMoreThanAllowedSpaces = spaceCount > maxSpaces;
            }

            // Check if the string length is less than 18 characters
            bool isLengthValid = input.Length < maxLenght;

            bool isEmpty = string.IsNullOrEmpty(input);

            if (hasInvalidChars)
                errorMessage = ErrorMessage.InvalidCharPattern;

            if (hasMoreThanAllowedSpaces)
                errorMessage = ErrorMessage.MultipleSpaces;

            if (!isLengthValid)
                errorMessage = ErrorMessage.InvalidLenght;

            if (isEmpty)
                errorMessage = ErrorMessage.Empty;

            // Return true if the string is valid
            return !hasInvalidChars && !hasMoreThanAllowedSpaces && isLengthValid && isEmpty == false;
        }
    }

}