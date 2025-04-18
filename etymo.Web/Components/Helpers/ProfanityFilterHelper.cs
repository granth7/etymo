namespace etymo.Web.Components.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web;

    public class ProfanityFilterHelper
    {
        private static readonly HttpClient _client = new();

        // Cache the filtered results to reduce API calls
        private static readonly Dictionary<string, string> _cache = [];
        private static readonly SemaphoreSlim _cacheLock = new(1, 1);

        // The API response model
        private class PurgoMalumResponse
        {
            [JsonPropertyName("result")]
            public string? Result { get; set; } // keeps PascalCase but maps to lowercase JSON
        }
        /// <summary>
        /// Sanitizes text by removing profanity via PurgoMalum API
        /// </summary>
        public static async Task<string> SanitizeTextAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // First check cache without lock (fast path)
            if (_cache.TryGetValue(text, out string? cachedResult))
                return cachedResult;

            // Slow path - need to check more carefully
            await _cacheLock.WaitAsync();
            try
            {
                // Double-check in case another thread added it while we were waiting
                if (_cache.TryGetValue(text, out cachedResult))
                    return cachedResult;

                // Proceed with API call
                try
                {
                    var encodedText = HttpUtility.UrlEncode(text);
                    var response = await _client.GetStringAsync(
                        $"https://www.purgomalum.com/service/json?text={encodedText}");

                    var result = JsonSerializer.Deserialize<PurgoMalumResponse>(response);
                    string filteredText = result?.Result ?? text;

                    _cache[text] = filteredText;
                    return filteredText;
                }
                catch (HttpRequestException)
                {
                    // If API fails, cache the original text to avoid repeated failures
                    _cache[text] = text;
                    return text;
                }
            }
            finally
            {
                _cacheLock.Release();
            }
        }
        /// <summary>
        /// Sanitizes a collection of tags by removing profanity
        /// </summary>
        public static async Task<string[]> SanitizeTagsAsync(string[] tags)
        {
            if (tags == null || tags.Length == 0)
                return [];

            var tasks = tags.Select(SanitizeTextAsync);
            return await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Sanitizes a dictionary of words and definitions by removing profanity
        /// </summary>
        public static async Task<Dictionary<string, string>> SanitizeWordListAsync(
            Dictionary<string, string> wordList)
        {
            if (wordList == null)
                return [];

            var result = new Dictionary<string, string>();

            foreach (var kvp in wordList)
            {
                string sanitizedKey = await SanitizeTextAsync(kvp.Key);
                string sanitizedValue = await SanitizeTextAsync(kvp.Value);
                result[sanitizedKey] = sanitizedValue;
            }

            return result;
        }

        /// <summary>
        /// Checks if text contains profanity by comparing the original text
        /// with the sanitized version
        /// </summary>
        public static async Task<bool> ContainsProfanityAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            string sanitized = await SanitizeTextAsync(text);
            return !text.Equals(sanitized, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Clears the internal cache
        /// </summary>
        public static void ClearCache()
        {
            _cacheLock.Wait();
            try
            {
                _cache.Clear();
            }
            finally
            {
                _cacheLock.Release();
            }
        }
    }
}