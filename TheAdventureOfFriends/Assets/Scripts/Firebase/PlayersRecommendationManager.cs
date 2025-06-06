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
    public float multiplayerWinRate;
    public float multiplayerWinRateL1W;
    public float multiplayerWinRateL1M;
    public float averageMultiplayerScore;
    public float averageMultiplayerScoreL1W;
    public float averageMultiplayerScoreL1M;
}

public class PlayersRecommendationByBehaviors
{
    public string id;
    public string userName;
    public int easyLevelCompleted;
    public int normalLevelCompleted;
    public int hardLevelCompleted;
    public float totalTimePlayGame;
    public float totalTimePlayGameL1W;
    public float totalTimePlayGameL1M;
    public int[] playTimeInDay = new int[8];
    public float averageRatingReceived;
    public float averageRatingReceivedL1W;
    public float averageRatingReceivedL1M;
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

    private readonly float[] contentBasedWeights = new float[] {
        1.0f, 0.5f, 1.5f, 0.3f, 1.5f, 1.0f, 2.0f, 0.8f, 1.3f, 0.8f, 1.8f, 0.6f,
        2.5f, 3.0f, 2.8f, // win rates (All, L1W, L1M)
        2.0f, 2.2f, 2.1f  // average scores (All, L1W, L1M)
    };
    private readonly float[] collaborativeWeights = new float[] {
        1.0f, 1.5f, 2.0f, 1.5f, 1.0f, 1.0f, 0.8f, 0.8f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f,
        2.5f, 2.8f, 2.6f  // average ratings received (All, L1W, L1M)
    };

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

                        await EnhanceWithMultiplayerStats(playerStats);

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

    private async Task EnhanceWithMultiplayerStats(PlayersRecommendationByFeatures playerStats)
    {
        try
        {
            var (winRate, winRateL1W, winRateL1M) = await CalculateWinRates(playerStats.id);
            playerStats.multiplayerWinRate = winRate;
            playerStats.multiplayerWinRateL1W = winRateL1W;
            playerStats.multiplayerWinRateL1M = winRateL1M;

            var (avgScore, avgScoreL1W, avgScoreL1M) = await CalculateAverageScores(playerStats.id);
            playerStats.averageMultiplayerScore = avgScore;
            playerStats.averageMultiplayerScoreL1W = avgScoreL1W;
            playerStats.averageMultiplayerScoreL1M = avgScoreL1M;

            Debug.Log($"Enhanced player {playerStats.userName} with multiplayer performance stats - WinRate: {winRate:F2}, AvgScore: {avgScore:F2}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to enhance player {playerStats.id} with multiplayer stats: {e.Message}");
            playerStats.multiplayerWinRate = 0f;
            playerStats.multiplayerWinRateL1W = 0f;
            playerStats.multiplayerWinRateL1M = 0f;
            playerStats.averageMultiplayerScore = 0f;
            playerStats.averageMultiplayerScoreL1W = 0f;
            playerStats.averageMultiplayerScoreL1M = 0f;
        }
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

                        await EnhanceWithSocialBehaviorStats(playerBehavior);

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

    private async Task EnhanceWithSocialBehaviorStats(PlayersRecommendationByBehaviors playerBehavior)
    {
        try
        {
            var (avgRating, avgRatingL1W, avgRatingL1M) = await CalculateAverageRatingsReceived(playerBehavior.id);

            playerBehavior.averageRatingReceived = avgRating;
            playerBehavior.averageRatingReceivedL1W = avgRatingL1W;
            playerBehavior.averageRatingReceivedL1M = avgRatingL1M;

            Debug.Log($"Enhanced player {playerBehavior.userName} with social behavior stats - AvgRating: {avgRating:F2}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to enhance player {playerBehavior.id} with social behavior stats: {e.Message}");
            playerBehavior.averageRatingReceived = 0f;
            playerBehavior.averageRatingReceivedL1W = 0f;
            playerBehavior.averageRatingReceivedL1M = 0f;
        }
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

        float minWinRate = playerFeaturesList.Min(p => p.multiplayerWinRate);
        float maxWinRate = playerFeaturesList.Max(p => p.multiplayerWinRate);
        float minWinRateL1W = playerFeaturesList.Min(p => p.multiplayerWinRateL1W);
        float maxWinRateL1W = playerFeaturesList.Max(p => p.multiplayerWinRateL1W);
        float minWinRateL1M = playerFeaturesList.Min(p => p.multiplayerWinRateL1M);
        float maxWinRateL1M = playerFeaturesList.Max(p => p.multiplayerWinRateL1M);

        float minMultiScore = playerFeaturesList.Min(p => p.averageMultiplayerScore);
        float maxMultiScore = playerFeaturesList.Max(p => p.averageMultiplayerScore);
        float minMultiScoreL1W = playerFeaturesList.Min(p => p.averageMultiplayerScoreL1W);
        float maxMultiScoreL1W = playerFeaturesList.Max(p => p.averageMultiplayerScoreL1W);
        float minMultiScoreL1M = playerFeaturesList.Min(p => p.averageMultiplayerScoreL1M);
        float maxMultiScoreL1M = playerFeaturesList.Max(p => p.averageMultiplayerScoreL1M);

        List<float[]> normalizedVectors = new List<float[]>();

        foreach (var playerStats in playerFeaturesList)
        {
            if (!playerIds.Contains(playerStats.id)) continue;

            float[] vector = new float[18];
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

            float rangeWinRate = maxWinRate - minWinRate;
            vector[12] = (rangeWinRate == 0) ? 0 : (playerStats.multiplayerWinRate - minWinRate) / rangeWinRate;

            float rangeWinRateL1W = maxWinRateL1W - minWinRateL1W;
            vector[13] = (rangeWinRateL1W == 0) ? 0 : (playerStats.multiplayerWinRateL1W - minWinRateL1W) / rangeWinRateL1W;

            float rangeWinRateL1M = maxWinRateL1M - minWinRateL1M;
            vector[14] = (rangeWinRateL1M == 0) ? 0 : (playerStats.multiplayerWinRateL1M - minWinRateL1M) / rangeWinRateL1M;

            float rangeMultiScore = maxMultiScore - minMultiScore;
            vector[15] = (rangeMultiScore == 0) ? 0 : (playerStats.averageMultiplayerScore - minMultiScore) / rangeMultiScore;

            float rangeMultiScoreL1W = maxMultiScoreL1W - minMultiScoreL1W;
            vector[16] = (rangeMultiScoreL1W == 0) ? 0 : (playerStats.averageMultiplayerScoreL1W - minMultiScoreL1W) / rangeMultiScoreL1W;

            float rangeMultiScoreL1M = maxMultiScoreL1M - minMultiScoreL1M;
            vector[17] = (rangeMultiScoreL1M == 0) ? 0 : (playerStats.averageMultiplayerScoreL1M - minMultiScoreL1M) / rangeMultiScoreL1M;

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

        float minTotalTimePlayGameL1W = playerBehaviorsList.Min(p => p.totalTimePlayGameL1W);
        float maxTotalTimePlayGameL1W = playerBehaviorsList.Max(p => p.totalTimePlayGameL1W);

        float minTotalTimePlayGameL1M = playerBehaviorsList.Min(p => p.totalTimePlayGameL1M);
        float maxTotalTimePlayGameL1M = playerBehaviorsList.Max(p => p.totalTimePlayGameL1M);

        float[] minPlayTime = new float[8];
        float[] maxPlayTime = new float[8];
        for (int i = 0; i < 8; i++)
        {
            int slot = i;
            minPlayTime[i] = playerBehaviorsList.Min(p => p.playTimeInDay[slot]);
            maxPlayTime[i] = playerBehaviorsList.Max(p => p.playTimeInDay[slot]);
        }

        float minRating = playerBehaviorsList.Min(p => p.averageRatingReceived);
        float maxRating = playerBehaviorsList.Max(p => p.averageRatingReceived);
        float minRatingL1W = playerBehaviorsList.Min(p => p.averageRatingReceivedL1W);
        float maxRatingL1W = playerBehaviorsList.Max(p => p.averageRatingReceivedL1W);
        float minRatingL1M = playerBehaviorsList.Min(p => p.averageRatingReceivedL1M);
        float maxRatingL1M = playerBehaviorsList.Max(p => p.averageRatingReceivedL1M);

        List<float[]> normalizedVectors = new List<float[]>();

        foreach (var playerBehavior in playerBehaviorsList)
        {
            if (!playerIds.Contains(playerBehavior.id)) continue;

            float[] vector = new float[17];
            float rangeEasy = maxEasy - minEasy;
            vector[0] = (rangeEasy == 0) ? 0 : (playerBehavior.easyLevelCompleted - minEasy) / rangeEasy;

            float rangeNormal = maxNormal - minNormal;
            vector[1] = (rangeNormal == 0) ? 0 : (playerBehavior.normalLevelCompleted - minNormal) / rangeNormal;

            float rangeHard = maxHard - minHard;
            vector[2] = (rangeHard == 0) ? 0 : (playerBehavior.hardLevelCompleted - minHard) / rangeHard;

            float rangeTotalTimePlayGame = maxTotalTimePlayGame - minTotalTimePlayGame;
            vector[3] = (rangeTotalTimePlayGame == 0) ? 0 : (playerBehavior.totalTimePlayGame - minTotalTimePlayGame) / rangeTotalTimePlayGame;

            float rangeTotalTimePlayGameL1W = maxTotalTimePlayGameL1W - minTotalTimePlayGameL1W;
            vector[4] = (rangeTotalTimePlayGameL1W == 0) ? 0 : (playerBehavior.totalTimePlayGameL1W - minTotalTimePlayGameL1W) / rangeTotalTimePlayGameL1W;

            float rangeTotalTimePlayGameL1M = maxTotalTimePlayGameL1M - minTotalTimePlayGameL1M;
            vector[5] = (rangeTotalTimePlayGameL1M == 0) ? 0 : (playerBehavior.totalTimePlayGameL1M - minTotalTimePlayGameL1M) / rangeTotalTimePlayGameL1M;

            for (int i = 0; i < 8; i++)
            {
                float rangePlayTime = maxPlayTime[i] - minPlayTime[i];
                vector[i + 6] = (rangePlayTime == 0) ? 0 : (playerBehavior.playTimeInDay[i] - minPlayTime[i]) / rangePlayTime;
            }

            float rangeRating = maxRating - minRating;
            vector[14] = (rangeRating == 0) ? 0 : (playerBehavior.averageRatingReceived - minRating) / rangeRating;

            float rangeRatingL1W = maxRatingL1W - minRatingL1W;
            vector[15] = (rangeRatingL1W == 0) ? 0 : (playerBehavior.averageRatingReceivedL1W - minRatingL1W) / rangeRatingL1W;

            float rangeRatingL1M = maxRatingL1M - minRatingL1M;
            vector[16] = (rangeRatingL1M == 0) ? 0 : (playerBehavior.averageRatingReceivedL1M - minRatingL1M) / rangeRatingL1M;

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

    public async Task<(float winRate, float winRateL1W, float winRateL1M)> CalculateWinRates(string userId)
    {
        var matchHistory = await GetUserMatchHistoryInternal(userId);

        if (matchHistory.Count == 0)
        {
            return (0f, 0f, 0f);
        }

        int totalWins = matchHistory.Count(m => m.gameStats.winnerId == userId);
        float winRate = (float)totalWins / matchHistory.Count;

        var oneWeekAgo = DateTimeOffset.UtcNow.AddDays(-7).ToUnixTimeSeconds();
        var l1wMatches = matchHistory.Where(m => m.matchInfo.matchDate >= oneWeekAgo).ToList();
        float winRateL1W = 0f;
        if (l1wMatches.Count > 0)
        {
            int l1wWins = l1wMatches.Count(m => m.gameStats.winnerId == userId);
            winRateL1W = (float)l1wWins / l1wMatches.Count;
        }

        var oneMonthAgo = DateTimeOffset.UtcNow.AddDays(-30).ToUnixTimeSeconds();
        var l1mMatches = matchHistory.Where(m => m.matchInfo.matchDate >= oneMonthAgo).ToList();
        float winRateL1M = 0f;
        if (l1mMatches.Count > 0)
        {
            int l1mWins = l1mMatches.Count(m => m.gameStats.winnerId == userId);
            winRateL1M = (float)l1mWins / l1mMatches.Count;
        }

        Debug.Log($"User {userId} Win Rates - Overall: {winRate:F2}, L1W: {winRateL1W:F2}, L1M: {winRateL1M:F2}");
        return (winRate, winRateL1W, winRateL1M);
    }

    public async Task<(float avgScore, float avgScoreL1W, float avgScoreL1M)> CalculateAverageScores(string userId)
    {
        var matchHistory = await GetUserMatchHistoryInternal(userId);

        if (matchHistory.Count == 0)
        {
            return (0f, 0f, 0f);
        }

        var userScores = GetUserScoresFromMatches(matchHistory, userId);

        float avgScore = userScores.Count > 0 ? userScores.Average() : 0f;

        var oneWeekAgo = DateTimeOffset.UtcNow.AddDays(-7).ToUnixTimeSeconds();
        var l1wMatches = matchHistory.Where(m => m.matchInfo.matchDate >= oneWeekAgo).ToList();
        var l1wScores = GetUserScoresFromMatches(l1wMatches, userId);
        float avgScoreL1W = l1wScores.Count > 0 ? l1wScores.Average() : 0f;

        var oneMonthAgo = DateTimeOffset.UtcNow.AddDays(-30).ToUnixTimeSeconds();
        var l1mMatches = matchHistory.Where(m => m.matchInfo.matchDate >= oneMonthAgo).ToList();
        var l1mScores = GetUserScoresFromMatches(l1mMatches, userId);
        float avgScoreL1M = l1mScores.Count > 0 ? l1mScores.Average() : 0f;

        Debug.Log($"User {userId} Avg Scores - Overall: {avgScore:F2}, L1W: {avgScoreL1W:F2}, L1M: {avgScoreL1M:F2}");
        return (avgScore, avgScoreL1W, avgScoreL1M);
    }

    public async Task<(float avgRating, float avgRatingL1W, float avgRatingL1M)> CalculateAverageRatingsReceived(string userId)
    {
        var matchHistory = await GetUserMatchHistoryInternal(userId);

        if (matchHistory.Count == 0)
        {
            return (0f, 0f, 0f);
        }

        var ratingsReceived = GetRatingsReceivedFromMatches(matchHistory, userId);

        float avgRating = ratingsReceived.Count > 0 ? ratingsReceived.Average() : 0f;

        var oneWeekAgo = DateTimeOffset.UtcNow.AddDays(-7).ToUnixTimeSeconds();
        var l1wMatches = matchHistory.Where(m => m.matchInfo.matchDate >= oneWeekAgo).ToList();
        var l1wRatings = GetRatingsReceivedFromMatches(l1wMatches, userId);
        float avgRatingL1W = l1wRatings.Count > 0 ? l1wRatings.Average() : 0f;

        var oneMonthAgo = DateTimeOffset.UtcNow.AddDays(-30).ToUnixTimeSeconds();
        var l1mMatches = matchHistory.Where(m => m.matchInfo.matchDate >= oneMonthAgo).ToList();
        var l1mRatings = GetRatingsReceivedFromMatches(l1mMatches, userId);
        float avgRatingL1M = l1mRatings.Count > 0 ? l1mRatings.Average() : 0f;

        Debug.Log($"User {userId} Avg Ratings - Overall: {avgRating:F2}, L1W: {avgRatingL1W:F2}, L1M: {avgRatingL1M:F2}");
        return (avgRating, avgRatingL1W, avgRatingL1M);
    }

    private async Task<List<MatchHistoryData>> GetUserMatchHistoryInternal(string userId)
    {
        List<MatchHistoryData> userMatches = new List<MatchHistoryData>();

        try
        {
            DataSnapshot snapshot = await dbReference.Child("Match_History").GetValueAsync();

            if (snapshot.Exists)
            {
                foreach (var childSnapshot in snapshot.Children)
                {
                    try
                    {
                        string json = childSnapshot.GetRawJsonValue();
                        MatchHistoryData match = JsonConvert.DeserializeObject<MatchHistoryData>(json);

                        if (match?.gameStats?.player1?.playerId == userId ||
                            match?.gameStats?.player2?.playerId == userId)
                        {
                            userMatches.Add(match);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error parsing match data: {e.Message}");
                    }
                }
            }

            Debug.Log($"Found {userMatches.Count} matches for user {userId}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading match history for user {userId}: {e.Message}");
        }

        return userMatches;
    }

    private List<float> GetUserScoresFromMatches(List<MatchHistoryData> matches, string userId)
    {
        List<float> scores = new List<float>();

        foreach (var match in matches)
        {
            if (match.gameStats.player1.playerId == userId)
            {
                scores.Add(match.gameStats.player1.totalScore);
            }
            else if (match.gameStats.player2.playerId == userId)
            {
                scores.Add(match.gameStats.player2.totalScore);
            }
        }

        return scores;
    }

    private List<float> GetRatingsReceivedFromMatches(List<MatchHistoryData> matches, string userId)
    {
        List<float> ratings = new List<float>();

        foreach (var match in matches)
        {
            string ratingReceived = null;

            if (match.gameStats.player1.playerId == userId)
            {
                ratingReceived = match.ratings.player2RatesPlayer1;
            }
            else if (match.gameStats.player2.playerId == userId)
            {
                ratingReceived = match.ratings.player1RatesPlayer2;
            }

            if (!string.IsNullOrEmpty(ratingReceived))
            {
                float ratingValue = ConvertRatingToNumber(ratingReceived);
                if (ratingValue > 0)
                {
                    ratings.Add(ratingValue);
                }
            }
        }

        return ratings;
    }

    private float ConvertRatingToNumber(string rating)
    {
        switch (rating.ToLower())
        {
            case "good":
                return 3.0f;
            case "medium":
                return 2.0f;
            case "bad":
                return 1.0f;
            default:
                return 0f;
        }
    }
}