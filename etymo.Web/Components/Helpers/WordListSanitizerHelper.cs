namespace etymo.Web.Components.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    public partial class WordListSanitizerHelper
    {
        /// <summary>
        /// Sanitizes a dictionary of words, removing or cleaning up potentially harmful entries
        /// </summary>
        public static Dictionary<string, string> SanitizeWordList(Dictionary<string, string> wordList)
        {
            if (wordList == null)
                return new Dictionary<string, string>();

            return wordList
                // Remove empty or whitespace entries
                .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Key) && !string.IsNullOrWhiteSpace(kvp.Value))

                // Trim and clean entries
                .Select(kvp => new KeyValuePair<string, string>(
                    TrimAndCleanInput(kvp.Key),
                    TrimAndCleanInput(kvp.Value)
                ))

                // Remove entries that are too long
                .Where(kvp =>
                    kvp.Key.Length <= 100 &&
                    kvp.Value.Length <= 500
                )

                // Remove entries with potentially harmful characters
                .Where(kvp =>
                    IsValidInput(kvp.Key) &&
                    IsValidInput(kvp.Value)
                )

                // Convert to dictionary
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        /// <summary>
        /// Trims input and removes potentially dangerous characters
        /// </summary>
        private static string TrimAndCleanInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // Trim whitespace
            input = input.Trim();

            // Remove control characters using the generated regex
            input = ControlCharRegex().Replace(input, string.Empty);

            // Remove multiple consecutive whitespace characters
            input = WhitespaceRegex().Replace(input, " ");

            return input;
        }

        /// <summary>
        /// Validates input to prevent SQL injection, emoji spam, and other potential threats
        /// </summary>
        private static bool IsValidInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            // Check for SQL injection patterns
            bool hasSqlInjectionRisk = SqlInjectionRegex().IsMatch(input);

            // Check for excessive emoji or special characters
            int specialCharCount = input.Count(c =>
                char.IsSurrogate(c) ||  // Catches most emojis
                char.IsSymbol(c) ||
                char.IsPunctuation(c)
            );

            // Check for unusual unicode characters
            bool hasUnusualUnicode = UnusualUnicodeRegex().IsMatch(input);

            // Prevent scripts or HTML
            bool hasScriptOrHtml = ScriptHtmlRegex().IsMatch(input);

            // Return false if any dangerous conditions are met
            return !(hasSqlInjectionRisk ||
                     specialCharCount > 5 ||
                     hasUnusualUnicode ||
                     hasScriptOrHtml);
        }

        /// <summary>
        /// Example usage method
        /// </summary>
        public static void ExampleUsage()
        {
            var inputWordList = new Dictionary<string, string>
        {
            { "good_key", "safe value" },
            { "  ", "  " },
            { "SQL_INJECTION_TEST", "SELECT * FROM Users;" },
            { "emoji_spam", "😀😀😀😀😀😀😀" }
        };

            var sanitizedWordList = SanitizeWordList(inputWordList);
            // sanitizedWordList will only contain safe entries
        }

        // Partial methods with GeneratedRegexAttribute
        [GeneratedRegex(@"[\p{C}]", RegexOptions.None)]
        private static partial Regex ControlCharRegex();

        [GeneratedRegex(@"\s+", RegexOptions.None)]
        private static partial Regex WhitespaceRegex();

        [GeneratedRegex(@"(;|\b(SELECT|INSERT|UPDATE|DELETE|DROP|UNION|ALTER)\b|\-\-|\#)", RegexOptions.IgnoreCase)]
        private static partial Regex SqlInjectionRegex();

        [GeneratedRegex(@"<script|<html|javascript:", RegexOptions.IgnoreCase)]
        private static partial Regex ScriptHtmlRegex();

        [GeneratedRegex(@"[\u0080-\uFFFF]", RegexOptions.None)]
        private static partial Regex UnusualUnicodeRegex();
    }
}
