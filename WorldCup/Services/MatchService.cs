using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;
using WorldCup.Models;

namespace WorldCup.Services;

/// <summary>
/// Fetches matches from the external API and polls for live updates.
/// POC: single HttpClient, no caching layer.
/// </summary>
public class MatchService : IAsyncDisposable
{
    // ── Replace with your real API endpoint ─────────────────────────
    private const string ApiUrl = "https://worldcup26.ir/get/games";
    private const int PollSeconds = 30; // poll every 30s for live scores

    private readonly HttpClient _http;
    private readonly PeriodicTimer _timer;
    private readonly CancellationTokenSource _cts = new();

    public List<Match> Matches { get; private set; } = [];
    public event Action? OnMatchesUpdated;

    public MatchService(HttpClient http)
    {
        _http = http;
        _timer = new PeriodicTimer(TimeSpan.FromSeconds(PollSeconds));
        _ = PollAsync(_cts.Token);
    }

    // Called by pages that need an immediate refresh
    public async Task RefreshAsync()
    {
        try
        {
           
            var data = await _http.GetFromJsonAsync<GameResponse>(ApiUrl);
            if (data is not null)
            {
                Matches = data.Games;
                OnMatchesUpdated?.Invoke();
            }
        }
        catch { /* swallow in POC; add logging in production */ }
    }

    private async Task PollAsync(CancellationToken ct)
    {
        await RefreshAsync();                          // immediate first load
        while (await _timer.WaitForNextTickAsync(ct))
            await RefreshAsync();
    }

    // ── Filtered views ───────────────────────────────────────────────
    public IEnumerable<Match> Live => Matches.Where(m => m.IsLive);
    public IEnumerable<Match> Upcoming => Matches.Where(m => m.IsUpcoming)
                                                  .OrderBy(m => m.LocalDateTime);
    public IEnumerable<Match> Finished => Matches.Where(m => m.IsFinished)
                                                  .OrderByDescending(m => m.LocalDateTime);

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        _timer.Dispose();
        _cts.Dispose();
    }
}