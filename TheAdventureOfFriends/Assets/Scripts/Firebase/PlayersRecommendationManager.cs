using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Database;
using UnityEngine;

[Serializable]
public class PlayerStatsForRecommendation
{
    public string id;
    public string userName;
    public float averageFruit;
    public float averageTime;
    public float averageEnemiesKilled;
    public float averageKnockBacks;
}

public class RecommendedPlayerInfo : RecommendedPlayerData
{
    public float suitabilityScore;
}

public class PlayersRecommendationManager
{
    private static PlayersRecommendationManager instance;
    public static PlayersRecommendationManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new PlayersRecommendationManager();

                instance.dbReference = FirebaseDatabase.DefaultInstance.RootReference;
            }
            return instance;
        }
    }

    private DatabaseReference dbReference;

    private PlayersRecommendationManager() { }

    private async Task<List<PlayerStatsForRecommendation>> LoadAllPlayerStatsAsync()
    {
        List<PlayerStatsForRecommendation> allPlayerStats = new List<PlayerStatsForRecommendation>();
        try
        {
            DataSnapshot snapshot = await dbReference.Child("PlayerStats").GetValueAsync();

            if (snapshot.Exists && snapshot.ChildrenCount > 0)
            {
                foreach (var childSnapshot in snapshot.Children)
                {
                    string json = childSnapshot.GetRawJsonValue();
                    PlayerStatsForRecommendation playerStats = JsonUtility.FromJson<PlayerStatsForRecommendation>(json);

                    if (playerStats != null)
                    {
                        playerStats.id = childSnapshot.Key;
                        allPlayerStats.Add(playerStats);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load player stats from Firebase: {e}");
        }

        return allPlayerStats;
    }

    private async Task<Dictionary<string, bool>> LoadPlayerOnlineStatusAsync(List<string> playerIds)
    {
        Dictionary<string, bool> onlineStatus = new Dictionary<string, bool>();
        if (playerIds == null || playerIds.Count == 0)
        {
            return onlineStatus;
        }

        foreach (var playerId in playerIds)
        {
            try
            {
                DataSnapshot snapshot = await dbReference.Child("Users").Child(playerId).Child("isOnline").GetValueAsync();

                if (snapshot.Exists)
                {
                    bool status = (bool)snapshot.Value;
                    onlineStatus[playerId] = status;
                }
                else
                {
                    onlineStatus[playerId] = false;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load online status for player {playerId} from Firebase: {e}");
                onlineStatus[playerId] = false;
            }
        }

        return onlineStatus;
    }

    private float[] NormalizeVector(float[] vector)
    {
        float sumOfSquares = 0;
        for (int i = 0; i < vector.Length; i++)
        {
            sumOfSquares += vector[i] * vector[i];
        }
        float magnitude = Mathf.Sqrt(sumOfSquares);

        if (magnitude == 0) return new float[vector.Length];

        float[] normalizedVector = new float[vector.Length];
        for (int i = 0; i < vector.Length; i++)
        {
            normalizedVector[i] = vector[i] / magnitude;
        }
        return normalizedVector;
    }

    private float CalculateCosineSimilarity(float[] vector1, float[] vector2)
    {
        if (vector1.Length != vector2.Length)
        {
            Debug.LogError("Vectors must have the same length for Cosine Similarity.");
            return 0;
        }

        float dotProduct = 0;
        float magnitude1 = 0;
        float magnitude2 = 0;

        for (int i = 0; i < vector1.Length; i++)
        {
            dotProduct += vector1[i] * vector2[i];
            magnitude1 += vector1[i] * vector1[i];
            magnitude2 += vector2[i] * vector2[i];
        }

        magnitude1 = Mathf.Sqrt(magnitude1);
        magnitude2 = Mathf.Sqrt(magnitude2);

        if (magnitude1 == 0 || magnitude2 == 0) return 0;

        return dotProduct / (magnitude1 * magnitude2);
    }

    public async Task<List<RecommendedPlayerData>> GetRecommendedPlayersAsync(string currentUserId)
    {
        List<PlayerStatsForRecommendation> allPlayerStats = await LoadAllPlayerStatsAsync();

        if (allPlayerStats == null || allPlayerStats.Count == 0)
        {
            Debug.LogWarning("No player stats loaded.");
            return new List<RecommendedPlayerData>();
        }

        PlayerStatsForRecommendation currentUserStats = allPlayerStats.FirstOrDefault(p => p.id == currentUserId);

        if (currentUserStats == null)
        {
            Debug.LogWarning($"Stats not found for current user ID: {currentUserId}. Cannot generate recommendations.");

            return allPlayerStats.Where(p => p.id != currentUserId)
                                .Select(p => new RecommendedPlayerData { userId = p.id, userName = p.userName, status = "Unknown", isOnline = false })
                                .ToList();
        }

        List<string> otherPlayerIds = allPlayerStats.Where(p => p.id != currentUserId).Select(p => p.id).ToList();

        Dictionary<string, bool> onlineStatus = await LoadPlayerOnlineStatusAsync(otherPlayerIds);

        float[] currentUserVector = new float[] {
            currentUserStats.averageFruit,
            currentUserStats.averageTime,
            currentUserStats.averageEnemiesKilled,
            currentUserStats.averageKnockBacks
        };

        float[] normalizedCurrentUserVector = NormalizeVector(currentUserVector);

        List<RecommendedPlayerInfo> recommendedPlayers = new List<RecommendedPlayerInfo>();

        foreach (var otherPlayerStats in allPlayerStats.Where(p => p.id != currentUserId))
        {
            float[] otherPlayerVector = new float[] {
                otherPlayerStats.averageFruit,
                otherPlayerStats.averageTime,
                otherPlayerStats.averageEnemiesKilled,
                otherPlayerStats.averageKnockBacks
            };

            float[] normalizedOtherPlayerVector = NormalizeVector(otherPlayerVector);

            float suitabilityScore = CalculateCosineSimilarity(normalizedCurrentUserVector, normalizedOtherPlayerVector);

            RecommendedPlayerInfo recommendedPlayer = new RecommendedPlayerInfo
            {
                userId = otherPlayerStats.id,
                userName = otherPlayerStats.userName,
                isOnline = onlineStatus.ContainsKey(otherPlayerStats.id) ? onlineStatus[otherPlayerStats.id] : false,
                status = (onlineStatus.ContainsKey(otherPlayerStats.id) && onlineStatus[otherPlayerStats.id]) ? "Online" : "Offline",
                suitabilityScore = suitabilityScore
            };

            recommendedPlayers.Add(recommendedPlayer);
        }

        recommendedPlayers = recommendedPlayers.OrderByDescending(p => p.suitabilityScore).ToList();

        if (!string.IsNullOrEmpty(currentUserId))
        {
            recommendedPlayers = recommendedPlayers.Where(p => p.userId != currentUserId).ToList();
            Debug.Log($"Filtered out current user {currentUserId} from recommended list. List count: {recommendedPlayers.Count}");
        }

        return recommendedPlayers.Cast<RecommendedPlayerData>().ToList();
    }

    public string GetCurrentUserId()
    {
        if (Authentication.CurrentUser != null)
        {
            return Authentication.CurrentUser.id;
        }
        Debug.LogError("PlayersRecommendationManager: Cannot get current user ID. Authentication.CurrentUser is null.");
        return null;
    }
}