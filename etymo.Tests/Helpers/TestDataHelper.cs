using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http.HttpResults;
using Shared.Models;

namespace etymo.Tests.Helpers;
public static class TestDataHelper
{
    /// <summary>
    /// Creates a WordList object with optional parameters.
    /// </summary>
    /// <param name="guid">Optional GUID for the WordList. If not provided, a new GUID will be generated.</param>
    /// <param name="words">Optional dictionary of words and definitions. If not provided, a default dictionary will be used.</param>
    /// <returns>A WordList object.</returns>
    public static WordList CreateWordList(Guid? guid = null, Dictionary<string, string>? words = null)
    {
        return new WordList
        {
            Guid = guid ?? Guid.NewGuid(),
            Words = words ?? new Dictionary<string, string>
            {
                { "word1", "definition1" },
                { "word2", "definition2" }
            }
        };
    }

    /// <summary>
    /// Creates a WordListOverview object with optional parameters.
    /// </summary>
    /// <param name="guid">Optional GUID for the WordListOverview. If not provided, a new GUID will be generated.</param>
    /// <param name="createdByUserGuid">Optional GUID of the user who created the WordListOverview. If not provided, a new GUID will be generated.</param>
    /// <param name="wordListGuid">Optional GUID of the associated WordList. If not provided, a new GUID will be generated.</param>
    /// <param name="isPublic">Optional flag indicating if the WordListOverview is public. Default is true.</param>
    /// <param name="upvotes">Optional number of upvotes. Default is 0.</param>
    /// <param name="title">Optional title of the WordListOverview. Default is "Test Title".</param>
    /// <param name="description">Optional description of the WordListOverview. Default is "Test Description".</param>
    /// <param name="tags">Optional list of tags. Default is ["tag1", "tag2"].</param>
    /// <param name="wordSample">Optional dictionary of word samples. If not provided, a default dictionary will be used.</param>
    /// <param name="createdDate">Optional creation date. Default is DateTime.UtcNow.</param>
    /// <param name="lastModifiedDate">Optional last modified date. Default is DateTime.UtcNow.</param>
    /// <returns>A WordListOverview object.</returns>
    public static WordListOverview CreateWordListOverview(
        Guid? guid = null,
        Guid? createdByUserGuid = null,
        Guid? wordListGuid = null,
        bool isPublic = true,
        int upvotes = 0,
        string title = "Test Title",
        string description = "Test Description",
        string[]? tags = null,
        Dictionary<string, string>? wordSample = null,
        DateTime? createdDate = null,
        DateTime? lastModifiedDate = null)
    {
        return new WordListOverview
        {
            Guid = guid ?? Guid.NewGuid(),
            CreatedByUserGuid = createdByUserGuid ?? Guid.NewGuid(),
            WordListGuid = wordListGuid ?? Guid.NewGuid(),
            IsPublic = isPublic,
            Upvotes = upvotes,
            Title = title,
            Description = description,
            Tags = tags ?? ["tag1", "tag2"],
            WordSample = wordSample ?? new Dictionary<string, string>
            {
                { "word1", "definition1" },
                { "word2", "definition2" }
            },
            CreatedDate = createdDate ?? DateTime.UtcNow,
            LastModifiedDate = lastModifiedDate ?? DateTime.UtcNow
        };  
    }
}