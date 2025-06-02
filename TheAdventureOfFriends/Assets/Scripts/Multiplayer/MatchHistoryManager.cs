using System;
using System.Collections.Generic;
using Firebase.Database;
using Firebase.Extensions;
using Newtonsoft.Json;
using UnityEngine;

public static class MatchHistoryManager
{
    private static DatabaseReference matchHistoryRef;

    static MatchHistoryManager()
    {
        matchHistoryRef = FirebaseDatabase.DefaultInstance.GetReference("Match_History");
    }

    public static void SaveMatchHistoryWithRating(
        string roomId,
        MultiplayerGameStats gameStats,
        string currentPlayerId,
        string rating,
        Action<bool> onComplete = null)
    {
        if (string.IsNullOrEmpty(roomId) || gameStats == null)
        {
            Debug.LogError("❌ MatchHistoryManager: Invalid parameters for saving match history");
            onComplete?.Invoke(false);
            return;
        }

        Debug.Log($"💾 MatchHistoryManager: Saving match history for room {roomId}...");
        Debug.Log($"🌟 Current player {currentPlayerId} rating: {rating}");

        matchHistoryRef.Child(roomId).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError($"❌ Failed to load existing match history: {task.Exception}");
                onComplete?.Invoke(false);
                return;
            }

            MatchHistoryData matchHistory;

            if (task.IsCompleted)
            {
                if (task.Result.Exists)
                {
                    try
                    {
                        string existingJson = task.Result.GetRawJsonValue();
                        matchHistory = JsonConvert.DeserializeObject<MatchHistoryData>(existingJson);
                        Debug.Log("📥 Loaded existing match history");
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"❌ Failed to parse existing match history: {e.Message}");
                        matchHistory = CreateNewMatchHistory(roomId, gameStats);
                    }
                }
                else
                {
                    matchHistory = CreateNewMatchHistory(roomId, gameStats);
                    Debug.Log("🆕 Created new match history");
                }

                UpdateRating(matchHistory, gameStats, currentPlayerId, rating);

                SaveMatchHistory(roomId, matchHistory, onComplete);
            }
        });
    }

    private static MatchHistoryData CreateNewMatchHistory(string roomId, MultiplayerGameStats gameStats)
    {
        var matchHistory = new MatchHistoryData();

        matchHistory.gameStats = new GameStatsData(gameStats);

        matchHistory.matchInfo.roomId = roomId;
        matchHistory.matchInfo.matchDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        matchHistory.matchInfo.players = new List<PlayerInfoData>
        {
            new PlayerInfoData(gameStats.player1.playerId, gameStats.player1.playerName),
            new PlayerInfoData(gameStats.player2.playerId, gameStats.player2.playerName)
        };

        Debug.Log($"✅ Created match history for room {roomId}");
        Debug.Log($"Players: {gameStats.player1.playerName} vs {gameStats.player2.playerName}");

        return matchHistory;
    }

    private static void UpdateRating(MatchHistoryData matchHistory, MultiplayerGameStats gameStats, string currentPlayerId, string rating)
    {
        bool isPlayer1 = gameStats.player1.playerId == currentPlayerId;

        if (isPlayer1)
        {
            matchHistory.ratings.player1RatesPlayer2 = rating;
            Debug.Log($"🌟 Player1 ({gameStats.player1.playerName}) rated Player2 ({gameStats.player2.playerName}): {rating}");
        }
        else
        {
            matchHistory.ratings.player2RatesPlayer1 = rating;
            Debug.Log($"🌟 Player2 ({gameStats.player2.playerName}) rated Player1 ({gameStats.player1.playerName}): {rating}");
        }
    }

    private static void SaveMatchHistory(string roomId, MatchHistoryData matchHistory, Action<bool> onComplete)
    {
        string json = JsonConvert.SerializeObject(matchHistory);

        matchHistoryRef.Child(roomId).SetRawJsonValueAsync(json)
            .ContinueWithOnMainThread(saveTask =>
            {
                if (saveTask.IsFaulted)
                {
                    Debug.LogError($"❌ MatchHistoryManager: Failed to save match history: {saveTask.Exception}");
                    onComplete?.Invoke(false);
                }
                else if (saveTask.IsCompleted)
                {
                    Debug.Log($"✅ MatchHistoryManager: Match history saved successfully for room {roomId}");
                    LogMatchHistorySummary(matchHistory);
                    onComplete?.Invoke(true);
                }
            });
    }

    private static void LogMatchHistorySummary(MatchHistoryData matchHistory)
    {
        Debug.Log("📊 Match History Summary:");
        Debug.Log($"Room: {matchHistory.matchInfo.roomId}");
        Debug.Log($"Date: {DateTimeOffset.FromUnixTimeSeconds(matchHistory.matchInfo.matchDate)}");
        Debug.Log($"Winner: {matchHistory.gameStats.winnerName}");
        Debug.Log($"Player1 rates Player2: {matchHistory.ratings.player1RatesPlayer2}");
        Debug.Log($"Player2 rates Player1: {matchHistory.ratings.player2RatesPlayer1}");
    }

    public static void CheckMatchHistoryExists(string roomId, Action<bool> onResult)
    {
        matchHistoryRef.Child(roomId).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                onResult?.Invoke(task.Result.Exists);
            }
            else
            {
                Debug.LogError($"Failed to check match history existence: {task.Exception}");
                onResult?.Invoke(false);
            }
        });
    }

    public static void GetMatchHistory(string roomId, Action<MatchHistoryData> onResult)
    {
        matchHistoryRef.Child(roomId).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                try
                {
                    string json = task.Result.GetRawJsonValue();
                    MatchHistoryData matchHistory = JsonConvert.DeserializeObject<MatchHistoryData>(json);
                    onResult?.Invoke(matchHistory);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to parse match history: {e.Message}");
                    onResult?.Invoke(null);
                }
            }
            else
            {
                onResult?.Invoke(null);
            }
        });
    }
}