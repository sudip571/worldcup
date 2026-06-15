using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace WorldCup.Models;

public class GameResponse
{
    public List<Match> Games { get; set; } = new();
}
public class Match
{
    [JsonPropertyName("_id")] public string MongoId { get; set; } = "";
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("home_team_id")] public string HomeTeamId { get; set; } = "";
    [JsonPropertyName("away_team_id")] public string AwayTeamId { get; set; } = "";
    [JsonPropertyName("home_score")] public string HomeScore { get; set; } = "0";
    [JsonPropertyName("away_score")] public string AwayScore { get; set; } = "0";
    [JsonPropertyName("home_scorers")] public string HomeScorers { get; set; } = "null";
    [JsonPropertyName("away_scorers")] public string AwayScorers { get; set; } = "null";
    [JsonPropertyName("group")] public string Group { get; set; } = "";
    [JsonPropertyName("matchday")] public string Matchday { get; set; } = "";
    [JsonPropertyName("local_date")] public string LocalDate { get; set; } = "";
    [JsonPropertyName("stadium_id")] public string StadiumId { get; set; } = "";
    [JsonPropertyName("finished")] public string Finished { get; set; } = "FALSE";
    [JsonPropertyName("time_elapsed")] public string TimeElapsed { get; set; } = "notstarted";
    [JsonPropertyName("type")] public string Type { get; set; } = "";
    [JsonPropertyName("home_team_name_en")] public string HomeTeamLabel { get; set; } = "";
    [JsonPropertyName("away_team_name_en")] public string AwayTeamLabel { get; set; } = "";

    // ── Computed helpers ─────────────────────────────────────────────
    public bool IsLive => TimeElapsed != "notstarted" && TimeElapsed != "fulltime"
                              && Finished == "FALSE";
    public bool IsFinished => Finished.Equals("TRUE", StringComparison.OrdinalIgnoreCase)
                              || TimeElapsed == "fulltime";
    public bool IsUpcoming => !IsLive && !IsFinished;

    public DateTime LocalDateTime =>
        DateTime.TryParseExact(LocalDate, "MM/dd/yyyy HH:mm",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var dt) ? dt : DateTime.MinValue;

    public MatchStatus Status => IsLive ? MatchStatus.Live
        : IsFinished ? MatchStatus.Finished
        : MatchStatus.Upcoming;
}

public enum MatchStatus { Upcoming, Live, Finished }


public class AppUser
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string AvatarUrl { get; set; } = "";
    public int TotalPoints { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Prediction> Predictions { get; set; } = [];
}

public class Prediction
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string MatchId { get; set; } = ""; // maps to Match.Id from API
    public int PredictedHome { get; set; }
    public int PredictedAway { get; set; }
    public int? PointsEarned { get; set; }       // null = not calculated yet
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public AppUser User { get; set; } = null!;
}

// ── Leaderboard read model (projected, not stored) ──────────────────
public class LeaderboardEntry
{
    public string DisplayName { get; set; } = "";
    public string AvatarUrl { get; set; } = "";
    public int TotalPoints { get; set; }
    public int Rank { get; set; }
    // Last 5 results: W / D / L / ?
    public List<string> Last5 { get; set; } = [];
}