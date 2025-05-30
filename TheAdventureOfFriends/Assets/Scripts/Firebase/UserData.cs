using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameData
{
    public int continueLevelNumber = 1;
    public int gameDifficulty = 1;
    public int lastUsedSkin = 0;
}

[Serializable]
public class LevelStats
{
    public float bestTime = 999f;
    public int bestFruitCollected = 0;
    public bool unlocked = false;
}

[Serializable]
public class DailyStats
{
    public int completedLevelCount = 0;
    public int totalFruitAmount = 0;
    public float totalTimePlayGame = 0f;
    public int enemiesKilled = 0;
    public int knockBacks = 0;
}

[Serializable]
public class UserData
{
    public string userName;
    public string id;
    public bool isOnline = false;
    public long lastOnlineTime = 0;

    public int completedLevelCount = 0;
    public float totalTimePlayGame = 0f;
    public int easyLevelCompleted = 0;
    public int normalLevelCompleted = 0;
    public int hardLevelCompleted = 0;
    public int totalFruitAmount = 0;
    public float averageFruit = 0f;
    public float averageTime = 0f;
    public int enemiesKilled = 0;
    public float averageEnemiesKilled = 0f;
    public int knockBacks = 0;
    public float averageKnockBacks = 0f;

    public float averageFruitL1W = 0f;
    public float averageTimeL1W = 0f;
    public float averageEnemiesKilledL1W = 0f;
    public float averageKnockBacksL1W = 0f;
    public float totalTimePlayGameL1W = 0f;

    public float averageFruitL1M = 0f;
    public float averageTimeL1M = 0f;
    public float averageEnemiesKilledL1M = 0f;
    public float averageKnockBacksL1M = 0f;
    public float totalTimePlayGameL1M = 0f;

    public Dictionary<string, LevelStats> levelProgress = new Dictionary<string, LevelStats>();
    public Dictionary<string, bool> skinUnlockedName = new Dictionary<string, bool>();
    public GameData gameProgress = new GameData();
    public int[] playTimeInDay = new int[8];
    public Dictionary<string, DailyStats> dailyStatsHistory = new Dictionary<string, DailyStats>();

    public UserData()
    {
        levelProgress["Level1"] = new LevelStats { unlocked = true };
        skinUnlockedName["0"] = true;
        averageTime = 0f;
        completedLevelCount = 0;
        gameProgress = new GameData();
    }

    public LevelStats GetLevelStats(string levelName)
    {
        if (!levelProgress.ContainsKey(levelName))
        {
            levelProgress[levelName] = new LevelStats();
        }
        return levelProgress[levelName];
    }
}