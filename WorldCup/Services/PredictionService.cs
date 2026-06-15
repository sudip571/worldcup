using System;
using System.Collections.Generic;
using System.Text;
using WorldCup.Data;
using WorldCup.Models;
using Microsoft.EntityFrameworkCore;

namespace WorldCup.Services;

public class PredictionService(AppDbContext db)
{
    // ── Save / update a prediction ───────────────────────────────────
    public async Task UpsertPredictionAsync(int userId, string matchId, int home, int away)
    {
        var existing = await db.Predictions
            .FirstOrDefaultAsync(p => p.UserId == userId && p.MatchId == matchId);

        if (existing is null)
        {
            db.Predictions.Add(new Prediction
            {
                UserId = userId,
                MatchId = matchId,
                PredictedHome = home,
                PredictedAway = away
            });
        }
        else
        {
            existing.PredictedHome = home;
            existing.PredictedAway = away;
            existing.PointsEarned = null; // reset until recalculated
        }
        await db.SaveChangesAsync();
    }

    public async Task<Prediction?> GetPredictionAsync(int userId, string matchId)
        => await db.Predictions
                   .FirstOrDefaultAsync(p => p.UserId == userId && p.MatchId == matchId);

    // ── Score a finished match against all predictions ───────────────
    public async Task ScoreMatchAsync(Match match)
    {
        if (!match.IsFinished) return;
        int actualHome = int.Parse(match.HomeScore);
        int actualAway = int.Parse(match.AwayScore);

        var preds = await db.Predictions
            .Where(p => p.MatchId == match.Id && p.PointsEarned == null)
            .ToListAsync();

        foreach (var p in preds)
        {
            p.PointsEarned = CalculatePoints(
                p.PredictedHome, p.PredictedAway, actualHome, actualAway);
        }

        // Update user totals
        var userIds = preds.Select(p => p.UserId).Distinct().ToList();
        foreach (var uid in userIds)
        {
            var user = await db.Users.FindAsync(uid);
            if (user is null) continue;
            user.TotalPoints = await db.Predictions
                .Where(p => p.UserId == uid && p.PointsEarned.HasValue)
                .SumAsync(p => p.PointsEarned!.Value);
        }

        await db.SaveChangesAsync();
    }

    // ── Scoring rules (POC: exact = 3pts, correct result = 1pt) ─────
    private static int CalculatePoints(int ph, int pa, int ah, int aa)
    {
        if (ph == ah && pa == aa) return 3; // exact score
        bool predictedHomeWin = ph > pa;
        bool predictedAwayWin = ph < pa;
        bool predictedDraw = ph == pa;
        bool actualHomeWin = ah > aa;
        bool actualAwayWin = ah < aa;
        bool actualDraw = ah == aa;
        if ((predictedHomeWin && actualHomeWin) ||
            (predictedAwayWin && actualAwayWin) ||
            (predictedDraw && actualDraw)) return 1; // correct result
        return 0;
    }

    // ── Leaderboard ──────────────────────────────────────────────────
    public async Task<List<LeaderboardEntry>> GetLeaderboardAsync(int take = 20)
    {
        var users = await db.Users
            .OrderByDescending(u => u.TotalPoints)
            .Take(take)
            .ToListAsync();

        var result = new List<LeaderboardEntry>();
        int rank = 1;
        foreach (var u in users)
        {
            // last 5 scored predictions
            var last5 = await db.Predictions
                .Where(p => p.UserId == u.Id && p.PointsEarned.HasValue)
                .OrderByDescending(p => p.SubmittedAt)
                .Take(5)
                .Select(p => p.PointsEarned!.Value)
                .ToListAsync();

            result.Add(new LeaderboardEntry
            {
                DisplayName = u.DisplayName,
                AvatarUrl = u.AvatarUrl,
                TotalPoints = u.TotalPoints,
                Rank = rank++,
                Last5 = last5.Select(pts => pts == 3 ? "W" : pts == 1 ? "D" : "L").ToList()
            });
        }
        return result;
    }
}