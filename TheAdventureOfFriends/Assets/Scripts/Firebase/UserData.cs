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
public class UserData
{
    public string userName;
    public string id;

    public int completedLevelCount = 0;
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

    public Dictionary<string, LevelStats> levelProgress = new Dictionary<string, LevelStats>();

    public Dictionary<string, bool> skinUnlockedName = new Dictionary<string, bool>();

    public GameData gameProgress = new GameData();
    public int[] playTimeInDay = new int[8];

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