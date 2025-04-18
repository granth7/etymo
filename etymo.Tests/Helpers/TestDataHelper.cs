using System.Diagnostics;
using System.Security.Claims;
using Shared.Models;

namespace etymo.Tests.Helpers;
public static class TestDataHelper
{
    /// <summary>
    /// Creates a WordList object with optional parameters.
    /// </summary>
    /// <param name="guid">Optional GUID for the WordList. If not provided, a new GUID will be generated.</param>
    /// <param name="creatorGuid">Optional GUID of the user who created the WordListOverview. If not provided, a new GUID will be generated.</param>
    /// <param name="ispublic">Optional bool set to true if public, false if private. If not provided, it will be set to true by default.</param>
    /// <param name="words">Optional dictionary of words and definitions. If not provided, a default dictionary will be used.</param>
    /// <returns>A WordList object.</returns>
    public static WordList CreateWordList(Guid? guid = null, Guid? creatorGuid = null, bool ispublic = true, Dictionary<string, string>? words = null)
    {
        return new WordList
        {
            Guid = guid ?? Guid.NewGuid(),
            CreatorGuid = creatorGuid ?? Guid.NewGuid(),
            IsPublic = ispublic,
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
    /// <param name="creatorGuid">Optional GUID of the user who created the WordListOverview. If not provided, a new GUID will be generated.</param>
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
        Guid? creatorGuid = null,
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
            CreatorGuid = creatorGuid ?? Guid.NewGuid(),
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

    /// <summary>
    /// Clean up any existing docker containers 
    /// </summary>
    public static async Task RunDockerComposeDown()
    {
        // Get the path to the workflows directory
        string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string workflowsDirectory = Path.Combine(currentDirectory, "../../../../.github/workflows");
        workflowsDirectory = Path.GetFullPath(workflowsDirectory);

        // Check if docker-compose.yml exists
        string composeFilePath = Path.Combine(workflowsDirectory, "docker-compose.yml");
        if (!File.Exists(composeFilePath))
        {
            Console.WriteLine($"docker-compose.yml not found at: {composeFilePath}");
            return;
        }

        // Start Docker Compose
        var startInfo = Process.Start(new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"compose -f \"{composeFilePath}\" down",
            WorkingDirectory = workflowsDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        });

        if (startInfo != null)
        {
            await startInfo.WaitForExitAsync();
        }
    }

    public static ClaimsPrincipal CreateUserClaimsPrincipalFromGuid(Guid _userGuid)
    {
        // Create claims principal for the test user
        var claims = new List<Claim>
        {
            new("sub", _userGuid.ToString()) // Keycloak's user ID
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new ClaimsPrincipal(identity);
    }
}