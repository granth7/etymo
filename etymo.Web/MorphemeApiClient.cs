using etymo.Web.Components.Services;
using Shared.Models;
using System.Net.Http;

namespace etymo.Web;

public class MorphemeApiClient(HttpClient httpClient, IAntiforgeryService antiforgeryService)
{
    public required List<Morpheme> Morphemes = [];
    private int morphemeCount = 0;

    public async Task<Morpheme[]> GetMorphemesAsync(int maxItems = 5, string gameType = "latinPrefixes", CancellationToken cancellationToken = default)
    {
        await foreach (var morepheme in httpClient.GetFromJsonAsAsyncEnumerable<Morpheme>($"/morphemelist?gameType={gameType}", cancellationToken))
        {
            morphemeCount++;
            if (morphemeCount > maxItems)
            {
                break;
            }
            if (morepheme is not null)
            {
                Morphemes.Add(morepheme);
            }
        }

        return [.. Morphemes];
    }

    public async Task<Morpheme[]> GetWordListAsync(int maxItems = 100, string wordListGuid = "", bool isPublic = true, string? userId = null, CancellationToken cancellationToken = default)
    {
        // Start with the base path
        var path = "/morphemelist";

        // Build query string manually
        var queryParams = new List<string>
        {
            $"wordListGuid={Uri.EscapeDataString(wordListGuid)}",
            $"isPublic={isPublic}"
        };

        if (!string.IsNullOrEmpty(userId))
            queryParams.Add($"userId={Uri.EscapeDataString(userId)}");

        // Append query string if there are parameters
        string requestUri = path;
        if (queryParams.Count > 0)
            requestUri += "?" + string.Join("&", queryParams);

        await foreach (var morepheme in httpClient.GetFromJsonAsAsyncEnumerable<Morpheme>(requestUri, cancellationToken))
        {
            morphemeCount++;
            if (morphemeCount > maxItems)
            {
                break;
            }
            if (morepheme is not null)
            {
                Morphemes.Add(morepheme);
            }
        }

        return [.. Morphemes];
    }

    public async Task<HttpResponseMessage> CreateWordList(WordList wordList)
    {
        var response = await httpClient.PutAsJsonAsync($"/postgres/word-list", wordList);
        return response;
    }

    public async Task<HttpResponseMessage> CreateWordListOverview(WordListOverview wordListOverview)
    {
        var response = await httpClient.PutAsJsonAsync($"/postgres/word-list-overview", wordListOverview);
        return response;
    }

    public async Task<List<WordListOverview>> GetWordListOverviewsAsync(
       string? userId = null,
       DateRange? dateRange = null,
       int pageNumber = 1,
       int pageSize = 10,
       CancellationToken cancellationToken = default)
    {
        List<WordListOverview> WordListOverviews = [];

        // Start with the base path
        var path = "/postgres/word-list-overviews";

        // Build query string manually
        var queryParams = new List<string>();

        if (!string.IsNullOrEmpty(userId))
            queryParams.Add($"userId={Uri.EscapeDataString(userId)}");

        if (dateRange.HasValue)
            queryParams.Add($"dateRange={Uri.EscapeDataString(dateRange.Value.ToString())}");

        // Add pagination parameters
        queryParams.Add($"pageNumber={pageNumber}");
        queryParams.Add($"pageSize={pageSize}");

        // Append query string if there are parameters
        string requestUri = path;
        if (queryParams.Count > 0)
            requestUri += "?" + string.Join("&", queryParams);

        await foreach (var wordListOverview in httpClient.GetFromJsonAsAsyncEnumerable<WordListOverview>(requestUri, cancellationToken))
        {
            if (wordListOverview is not null)
            {
                WordListOverviews.Add(wordListOverview);
            }
        }

        return WordListOverviews;
    }

    public async Task<List<WordListOverview>> GetPrivateWordListOverviewsAsync(
        string userId,
        DateRange? dateRange = null,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        List<WordListOverview> WordListOverviews = [];

        // Start with the base path
        var path = "/postgres/private-word-list-overviews";

        // Build query string manually
        var queryParams = new List<string>
        {
            $"userId={Uri.EscapeDataString(userId)}"
        };

        if (dateRange.HasValue)
            queryParams.Add($"dateRange={Uri.EscapeDataString(dateRange.Value.ToString())}");

        // Add pagination parameters
        queryParams.Add($"pageNumber={pageNumber}");
        queryParams.Add($"pageSize={pageSize}");

        // Append query string if there are parameters
        string requestUri = path;
        if (queryParams.Count > 0)
            requestUri += "?" + string.Join("&", queryParams);

        await foreach (var wordListOverview in httpClient.GetFromJsonAsAsyncEnumerable<WordListOverview>(requestUri, cancellationToken))
        {
            if (wordListOverview is not null)
            {
                WordListOverviews.Add(wordListOverview);
            }
        }

        return WordListOverviews;
    }

    public async Task<WordList?> FetchWordListAsync(Guid wordListId, CancellationToken cancellationToken = default)
    {
        try
        {
            var wordList = await httpClient.GetFromJsonAsync<WordList>(
                $"/postgres/word-list?wordListId={wordListId}",
                cancellationToken);

            return wordList;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<WordListOverview?> FetchWordListOverviewAsync(Guid wordListOverviewId, CancellationToken cancellationToken = default)
    {
        try
        {
            var wordList = await httpClient.GetFromJsonAsync<WordListOverview>(
                $"/postgres/word-list-overview?wordListOverviewId={wordListOverviewId}",
                cancellationToken);

            return wordList;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<WordList?> FetchPrivateWordListAsync(Guid wordListId, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var wordList = await httpClient.GetFromJsonAsync<WordList>(
                $"/postgres/private-word-list?wordListId={wordListId}&userId={userId}",
                cancellationToken);

            return wordList;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<WordListOverview?> FetchPrivateWordListOverviewAsync(Guid wordListOverviewId, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var wordList = await httpClient.GetFromJsonAsync<WordListOverview>(
                $"/postgres/private-word-list-overview?wordListOverviewId={wordListOverviewId}&userId={userId}",
                cancellationToken);

            return wordList;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<HttpResponseMessage> DeleteWordListOverviewAsync(Guid wordListOverviewId, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Create a request message
            var request = new HttpRequestMessage(HttpMethod.Delete, $"/postgres/word-list-overview?wordListOverviewId={wordListOverviewId}&userId={userId}");

            // Get antiforgery token
            var token = await antiforgeryService.GetAntiforgeryTokenAsync();

            // Add token to header matching your configuration
            request.Headers.Add("X-CSRF-TOKEN", token);

            var response = await httpClient.SendAsync(request, cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting word list overview: {ex.Message}");
            throw;
        }
    }

    public async Task<UpvoteResult> ToggleUpvoteAsync(Guid wordListOverviewId)
    {
        try
        {
            // Create a request message
            var request = new HttpRequestMessage(HttpMethod.Post, $"/postgres/toggle-upvote?wordListOverviewId={wordListOverviewId}");

            // Get antiforgery token
            var token = await antiforgeryService.GetAntiforgeryTokenAsync();

            // Add token to header matching your configuration
            request.Headers.Add("X-CSRF-TOKEN", token);

            // Send the request
            var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<UpvoteResult>();
            return result ?? new UpvoteResult { IsUpvoted = false, UpvoteCount = 0 };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error toggling upvote: {ex.Message}");
            throw;
        }
    }

    public async Task<int> CreateReportAsync(ReportRequest reportRequest)
    {
        var response = await httpClient.PostAsJsonAsync("/postgres/report", reportRequest);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<int>();
    }

    public async Task<List<Report>> GetReportsAsync(CancellationToken cancellationToken = default)
    {
        List<Report> reports = [];
        await foreach (var report in httpClient.GetFromJsonAsAsyncEnumerable<Report>("/postgres/reports", cancellationToken))
        {
            if (report is not null)
            {
                reports.Add(report);
            }
        }
        return reports;
    }

    public async Task<int> ResolveReportAsync(int reportId, string action)
    {
        var response = await httpClient.PutAsJsonAsync($"/postgres/report?reportId={reportId}&action={Uri.EscapeDataString(action)}", new { });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<int>();
    }
}

public record Morpheme(KeyValuePair<string, string> Kvp)
{
    public bool IsOnTop { get; set; }
    public bool GuessedCorrectly { get; set; }
    public string? CardColor { get; set; }
    public Morpheme? Next { get; set; }
    public Morpheme? Previous { get; set; }
}

public class UpvoteResult
{
    public bool IsUpvoted { get; set; }
    public int UpvoteCount { get; set; }
}