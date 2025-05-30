using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Database;
using Newtonsoft.Json;
using UnityEngine;

[Serializable]
public class PlayersRecommendationByFeatures
{
    public string id;
    public string userName;
    public float averageFruit;
    public float averageTime;
    public float averageEnemiesKilled;
    public float averageKnockBacks;
    public float averageFruitL1W;
    public float averageTimeL1W;
    public float averageEnemiesKilledL1W;
    public float averageKnockBacksL1W;
    public float averageFruitL1M;
    public float averageTimeL1M;
    public float averageEnemiesKilledL1M;
    public float averageKnockBacksL1M;
}

public class PlayersRecommendationByBehaviors
{
    public string id;
    public string userName;
    public int easyLevelCompleted;
    public int normalLevelCompleted;
    public int hardLevelCompleted;
    public float totalTimePlayGame;
    public int[] playTimeInDay = new int[8];
}

public class RecommendedPlayerInfo : RecommendedPlayerData
{
    public float suitabilityScore;
}

[Serializable]
public class RecommendedPlayerData
{
    public string userId;
    public string userName;
    public string status;
    public bool isOnline;
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

    private readonly float[] contentBasedWeights = new float[] { 1.0f, 0.5f, 1.5f, 0.3f, 1.5f, 1.0f, 2.0f, 0.8f, 1.3f, 0.8f, 1.8f, 0.6f };
    private readonly float[] collaborativeWeights = new float[] { 1.0f, 1.5f, 2.0f, 1.5f, 0.8f, 0.8f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f };

    private async Task<List<PlayersRecommendationByFeatures>> LoadAllPlayerFeatureAsync()
    {
        List<PlayersRecommendationByFeatures> allPlayerStats = new List<PlayersRecommendationByFeatures>();
        try
        {
            DataSnapshot snapshot = await dbReference.Child("PlayerStats").GetValueAsync();

            if (snapshot.Exists && snapshot.ChildrenCount > 0)
            {
                foreach (var childSnapshot in snapshot.Children)
                {
                    string json = childSnapshot.GetRawJsonValue();
                    PlayersRecommendationByFeatures playerStats = JsonConvert.DeserializeObject<PlayersRecommendationByFeatures>(json);

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

    private async Task<List<PlayersRecommendationByBehaviors>> LoadAllPlayerBehaviorsAsync()
    {
        List<PlayersRecommendationByBehaviors> allPlayerBehaviors = new List<PlayersRecommendationByBehaviors>();
        try
        {
            DataSnapshot snapshot = await dbReference.Child("PlayerStats").GetValueAsync();

            if (snapshot.Exists && snapshot.ChildrenCount > 0)
            {
                foreach (var childSnapshot in snapshot.Children)
                {
                    string json = childSnapshot.GetRawJsonValue();
                    PlayersRecommendationByBehaviors playerBehavior = JsonConvert.DeserializeObject<PlayersRecommendationByBehaviors>(json);

                    if (playerBehavior != null)
                    {
                        playerBehavior.id = childSnapshot.Key;
                        allPlayerBehaviors.Add(playerBehavior);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load player behaviors from Firebase: {e}");
        }

        return allPlayerBehaviors;
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
                DataSnapshot snapshot = await dbReference.Child("PlayerStats").Child(playerId).Child("isOnline").GetValueAsync();

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

    private List<float[]> NormalizeMinMaxContentBased(List<PlayersRecommendationByFeatures> playerFeaturesList, List<string> playerIds)
    {
        if (playerFeaturesList == null || playerFeaturesList.Count == 0) return new List<float[]>();

        float minFruit = playerFeaturesList.Min(p => p.averageFruit);
        float maxFruit = playerFeaturesList.Max(p => p.averageFruit);

        float minTime = playerFeaturesList.Min(p => p.averageTime);
        float maxTime = playerFeaturesList.Max(p => p.averageTime);

        float minEnemiesKilled = playerFeaturesList.Min(p => p.averageEnemiesKilled);
        float maxEnemiesKilled = playerFeaturesList.Max(p => p.averageEnemiesKilled);

        float minKnockBacks = playerFeaturesList.Min(p => p.averageKnockBacks);
        float maxKnockBacks = playerFeaturesList.Max(p => p.averageKnockBacks);

        float minFruitL1W = playerFeaturesList.Min(p => p.averageFruitL1W);
        float maxFruitL1W = playerFeaturesList.Max(p => p.averageFruitL1W);

        float minTimeL1W = playerFeaturesList.Min(p => p.averageTimeL1W);
        float maxTimeL1W = playerFeaturesList.Max(p => p.averageTimeL1W);

        float minEnemiesKilledL1W = playerFeaturesList.Min(p => p.averageEnemiesKilledL1W);
        float maxEnemiesKilledL1W = playerFeaturesList.Max(p => p.averageEnemiesKilledL1W);

        float minKnockBacksL1W = playerFeaturesList.Min(p => p.averageKnockBacksL1W);
        float maxKnockBacksL1W = playerFeaturesList.Max(p => p.averageKnockBacksL1W);

        float minFruitL1M = playerFeaturesList.Min(p => p.averageFruitL1M);
        float maxFruitL1M = playerFeaturesList.Max(p => p.averageFruitL1M);

        float minTimeL1M = playerFeaturesList.Min(p => p.averageTimeL1M);
        float maxTimeL1M = playerFeaturesList.Max(p => p.averageTimeL1M);

        float minEnemiesKilledL1M = playerFeaturesList.Min(p => p.averageEnemiesKilledL1M);
        float maxEnemiesKilledL1M = playerFeaturesList.Max(p => p.averageEnemiesKilledL1M);

        float minKnockBacksL1M = playerFeaturesList.Min(p => p.averageKnockBacksL1M);
        float maxKnockBacksL1M = playerFeaturesList.Max(p => p.averageKnockBacksL1M);

        List<float[]> normalizedVectors = new List<float[]>();

        foreach (var playerStats in playerFeaturesList)
        {
            if (!playerIds.Contains(playerStats.id)) continue;

            float[] vector = new float[12];
            float rangeFruit = maxFruit - minFruit;
            vector[0] = (rangeFruit == 0) ? 0 : (playerStats.averageFruit - minFruit) / rangeFruit;

            float rangeTime = maxTime - minTime;
            vector[1] = (rangeTime == 0) ? 0 : (playerStats.averageTime - minTime) / rangeTime;

            float rangeEnemies = maxEnemiesKilled - minEnemiesKilled;
            vector[2] = (rangeEnemies == 0) ? 0 : (playerStats.averageEnemiesKilled - minEnemiesKilled) / rangeEnemies;

            float rangeKnockBacks = maxKnockBacks - minKnockBacks;
            vector[3] = (rangeKnockBacks == 0) ? 0 : (playerStats.averageKnockBacks - minKnockBacks) / rangeKnockBacks;

            float rangeFruitL1W = maxFruitL1W - minFruitL1W;
            vector[4] = (rangeFruitL1W == 0) ? 0 : (playerStats.averageFruitL1W - minFruitL1W) / rangeFruitL1W;

            float rangeTimeL1W = maxTimeL1W - minTimeL1W;
            vector[5] = (rangeTimeL1W == 0) ? 0 : (playerStats.averageTimeL1W - minTimeL1W) / rangeTimeL1W;

            float rangeEnemiesKilledL1W = maxEnemiesKilledL1W - minEnemiesKilledL1W;
            vector[6] = (rangeEnemiesKilledL1W == 0) ? 0 : (playerStats.averageEnemiesKilledL1W - minEnemiesKilledL1W) / rangeEnemiesKilledL1W;

            float rangeKnockBacksL1W = maxKnockBacksL1W - minKnockBacksL1W;
            vector[7] = (rangeKnockBacksL1W == 0) ? 0 : (playerStats.averageKnockBacksL1W - minKnockBacksL1W) / rangeKnockBacksL1W;

            float rangeFruitL1M = maxFruitL1M - minFruitL1M;
            vector[8] = (rangeFruitL1M == 0) ? 0 : (playerStats.averageFruitL1M - minFruitL1M) / rangeFruitL1M;

            float rangeTimeL1M = maxTimeL1M - minTimeL1M;
            vector[9] = (rangeTimeL1M == 0) ? 0 : (playerStats.averageTimeL1M - minTimeL1M) / rangeTimeL1M;

            float rangeEnemiesKilledL1M = maxEnemiesKilledL1M - minEnemiesKilledL1M;
            vector[10] = (rangeEnemiesKilledL1M == 0) ? 0 : (playerStats.averageEnemiesKilledL1M - minEnemiesKilledL1M) / rangeEnemiesKilledL1M;

            float rangeKnockBacksL1M = maxKnockBacksL1M - minKnockBacksL1M;
            vector[11] = (rangeKnockBacksL1M == 0) ? 0 : (playerStats.averageKnockBacksL1M - minKnockBacksL1M) / rangeKnockBacksL1M;

            normalizedVectors.Add(vector);
        }

        return normalizedVectors;
    }

    private List<float[]> NormalizeMinMaxCollaborative(List<PlayersRecommendationByBehaviors> playerBehaviorsList, List<string> playerIds)
    {
        if (playerBehaviorsList == null || playerBehaviorsList.Count == 0) return new List<float[]>();

        float minEasy = playerBehaviorsList.Min(p => p.easyLevelCompleted);
        float maxEasy = playerBehaviorsList.Max(p => p.easyLevelCompleted);

        float minNormal = playerBehaviorsList.Min(p => p.normalLevelCompleted);
        float maxNormal = playerBehaviorsList.Max(p => p.normalLevelCompleted);

        float minHard = playerBehaviorsList.Min(p => p.hardLevelCompleted);
        float maxHard = playerBehaviorsList.Max(p => p.hardLevelCompleted);

        float minTotalTimePlayGame = playerBehaviorsList.Min(p => p.totalTimePlayGame);
        float maxTotalTimePlayGame = playerBehaviorsList.Max(p => p.totalTimePlayGame);

        float[] minPlayTime = new float[8];
        float[] maxPlayTime = new float[8];
        for (int i = 0; i < 8; i++)
        {
            int slot = i;
            minPlayTime[i] = playerBehaviorsList.Min(p => p.playTimeInDay[slot]);
            maxPlayTime[i] = playerBehaviorsList.Max(p => p.playTimeInDay[slot]);
        }

        List<float[]> normalizedVectors = new List<float[]>();

        foreach (var playerBehavior in playerBehaviorsList)
        {
            if (!playerIds.Contains(playerBehavior.id)) continue;

            float[] vector = new float[11];
            float rangeEasy = maxEasy - minEasy;
            vector[0] = (rangeEasy == 0) ? 0 : (playerBehavior.easyLevelCompleted - minEasy) / rangeEasy;

            float rangeNormal = maxNormal - minNormal;
            vector[1] = (rangeNormal == 0) ? 0 : (playerBehavior.normalLevelCompleted - minNormal) / rangeNormal;

            float rangeHard = maxHard - minHard;
            vector[2] = (rangeHard == 0) ? 0 : (playerBehavior.hardLevelCompleted - minHard) / rangeHard;

            float rangeTotalTimePlayGame = maxTotalTimePlayGame - minTotalTimePlayGame;
            vector[3] = (rangeTotalTimePlayGame == 0) ? 0 : (playerBehavior.totalTimePlayGame - minTotalTimePlayGame) / rangeTotalTimePlayGame;

            for (int i = 0; i < 8; i++)
            {
                float rangePlayTime = maxPlayTime[i] - minPlayTime[i];
                vector[i + 4] = (rangePlayTime == 0) ? 0 : (playerBehavior.playTimeInDay[i] - minPlayTime[i]) / rangePlayTime;
            }

            normalizedVectors.Add(vector);
        }

        return normalizedVectors;
    }

    private float CalculateCosineSimilarity(float[] vector1, float[] vector2, float[] weights)
    {
        if (vector1.Length != vector2.Length || vector1.Length != weights.Length)
        {
            Debug.LogError("Vectors and Weights must have the same length for Cosine Similarity.");
            return 0;
        }

        float dotProduct = 0;
        float magnitude1 = 0;
        float magnitude2 = 0;

        for (int i = 0; i < vector1.Length; i++)
        {
            float _vector1 = vector1[i] * weights[i];
            float _vector2 = vector2[i] * weights[i];
            dotProduct += _vector1 * _vector2;
            magnitude1 += _vector1 * _vector1;
            magnitude2 += _vector2 * _vector2;
        }

        magnitude1 = Mathf.Sqrt(magnitude1);
        magnitude2 = Mathf.Sqrt(magnitude2);

        if (magnitude1 == 0 || magnitude2 == 0) return 0;

        return dotProduct / (magnitude1 * magnitude2);
    }

    public async Task<List<RecommendedPlayerData>> GetContentBasedRecommendedPlayersAsync(string currentUserId)
    {
        List<PlayersRecommendationByFeatures> allPlayerStats = await LoadAllPlayerFeatureAsync();

        if (allPlayerStats == null || allPlayerStats.Count == 0)
        {
            Debug.LogWarning("No player stats loaded.");
            return new List<RecommendedPlayerData>();
        }

        PlayersRecommendationByFeatures currentUserStats = allPlayerStats.FirstOrDefault(p => p.id == currentUserId);

        if (currentUserStats == null)
        {
            Debug.LogWarning($"Stats not found for current user ID: {currentUserId}. Cannot generate recommendations.");

            return allPlayerStats.Where(p => p.id != currentUserId)
                                .Select(p => new RecommendedPlayerData { userId = p.id, userName = p.userName, status = "Unknown", isOnline = false })
                                .ToList();
        }

        List<string> otherPlayerIds = allPlayerStats.Where(p => p.id != currentUserId).Select(p => p.id).ToList();
        otherPlayerIds.Add(currentUserId);

        Dictionary<string, bool> onlineStatus = await LoadPlayerOnlineStatusAsync(otherPlayerIds);

        List<float[]> normalizedVectors = NormalizeMinMaxContentBased(allPlayerStats, otherPlayerIds);

        int currentUserIndex = allPlayerStats.FindIndex(p => p.id == currentUserId);
        if (currentUserIndex == -1) return new List<RecommendedPlayerData>();
        float[] currentUserVector = normalizedVectors[currentUserIndex];

        List<RecommendedPlayerInfo> recommendedPlayers = new List<RecommendedPlayerInfo>();

        for (int i = 0; i < allPlayerStats.Count; i++)
        {
            var otherPlayerStats = allPlayerStats[i];
            if (otherPlayerStats.id == currentUserId) continue;

            float[] otherPlayerVector = normalizedVectors[i];

            float suitabilityScore = CalculateCosineSimilarity(currentUserVector, otherPlayerVector, contentBasedWeights);

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

        return recommendedPlayers.Cast<RecommendedPlayerData>().ToList();
    }

    public async Task<List<RecommendedPlayerData>> GetCollaborativeRecommendedPlayersAsync(string currentUserId)
    {
        List<PlayersRecommendationByBehaviors> allPlayerBehaviors = await LoadAllPlayerBehaviorsAsync();

        if (allPlayerBehaviors == null || allPlayerBehaviors.Count == 0)
        {
            Debug.LogWarning("No player behaviors loaded.");
            return new List<RecommendedPlayerData>();
        }

        PlayersRecommendationByBehaviors currentUserBehavior = allPlayerBehaviors.FirstOrDefault(p => p.id == currentUserId);

        if (currentUserBehavior == null)
        {
            Debug.LogWarning($"Behavior stats not found for current user ID: {currentUserId}. Cannot generate recommendations.");

            return allPlayerBehaviors.Where(p => p.id != currentUserId)
                                     .Select(p => new RecommendedPlayerData { userId = p.id, userName = p.userName, status = "Unknown", isOnline = false })
                                     .ToList();
        }

        List<string> otherPlayerIds = allPlayerBehaviors.Where(p => p.id != currentUserId).Select(p => p.id).ToList();
        otherPlayerIds.Add(currentUserId);

        Dictionary<string, bool> onlineStatus = await LoadPlayerOnlineStatusAsync(otherPlayerIds);

        List<float[]> normalizedVectors = NormalizeMinMaxCollaborative(allPlayerBehaviors, otherPlayerIds);

        int currentUserIndex = allPlayerBehaviors.FindIndex(p => p.id == currentUserId);
        if (currentUserIndex == -1) return new List<RecommendedPlayerData>();
        float[] currentUserVector = normalizedVectors[currentUserIndex];

        List<RecommendedPlayerInfo> recommendedPlayers = new List<RecommendedPlayerInfo>();

        for (int i = 0; i < allPlayerBehaviors.Count; i++)
        {
            var otherPlayerBehavior = allPlayerBehaviors[i];
            if (otherPlayerBehavior.id == currentUserId) continue;

            float[] otherPlayerVector = normalizedVectors[i];

            float suitabilityScore = CalculateCosineSimilarity(currentUserVector, otherPlayerVector, collaborativeWeights);

            RecommendedPlayerInfo recommendedPlayer = new RecommendedPlayerInfo
            {
                userId = otherPlayerBehavior.id,
                userName = otherPlayerBehavior.userName,
                isOnline = onlineStatus.ContainsKey(otherPlayerBehavior.id) ? onlineStatus[otherPlayerBehavior.id] : false,
                status = (onlineStatus.ContainsKey(otherPlayerBehavior.id) && onlineStatus[otherPlayerBehavior.id]) ? "Online" : "Offline",
                suitabilityScore = suitabilityScore
            };

            recommendedPlayers.Add(recommendedPlayer);
        }

        recommendedPlayers = recommendedPlayers.OrderByDescending(p => p.suitabilityScore).ToList();

        return recommendedPlayers.Cast<RecommendedPlayerData>().ToList();
    }

    public string GetCurrentUserId()
    {
        if (FirebaseManager.CurrentUser != null)
        {
            return FirebaseManager.CurrentUser.id;
        }
        Debug.LogError("PlayersRecommendationManager: Cannot get current user ID. FirebaseManager.CurrentUser is null.");
        return null;
    }
}