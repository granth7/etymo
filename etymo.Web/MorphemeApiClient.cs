using Microsoft.AspNetCore.Mvc;
using Shared.Models;
using System.Threading;

namespace etymo.Web;

public class MorphemeApiClient(HttpClient httpClient)
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

    public async Task<HttpResponseMessage> CreateWordListOverview(WordListOverview wordListOverview)
    {
        var response = await httpClient.PostAsJsonAsync($"/word-list-overview", wordListOverview);
        return response;
    }

    public async Task<List<WordListOverview>> GetWordListOverviewsByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        List<WordListOverview> WordListOverviews = [];
        await foreach (var wordListOverview in httpClient.GetFromJsonAsAsyncEnumerable<WordListOverview>($"/word-list-overviews?userId={userId}", cancellationToken))
        {
            if (wordListOverview is not null)
            {
                WordListOverviews.Add(wordListOverview);
            }
        }

        return WordListOverviews;
    }
}

public record Morpheme(KeyValuePair<string, string> Kvp)
{
    public bool IsOnTop { get; set; }
    public bool GuessedCorrectly { get; set; }

    public int PreviousIndex { get; set; }

    public int NextIndex { get; set; }
}
