using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    private UI_InGame inGameUI;

    [Header("Level Management")]
    [SerializeField] private float levelTimer;
    [SerializeField] private int currentLevelIndex;
    private int nextLevelIndex;

    [Header("Fruit Management")]
    public bool fruitAreRandom;
    public int fruitCollected;
    public int totalFruit;
    public Transform fruitParent;

    [Header("Enemy Management")]
    public int enemiesKilled;
    [Header("Knockback Management")]
    public int knockBacks;

    [Header("Checkpoint")]
    public bool canReactive;

    [Header("Managers")]
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private SkinManager skinManager;
    [SerializeField] private DifficultyManager difficultyManager;
    [SerializeField] private ObjectCreator objectCreator;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        inGameUI = UI_InGame.instance;

        currentLevelIndex = SceneManager.GetActiveScene().buildIndex - 2;

        nextLevelIndex = currentLevelIndex + 1;

        levelTimer = 0;

        CollectFruitInfo();
        CreateManagersIfNeeded();
    }

    private void Update()
    {
        levelTimer += Time.deltaTime;

        inGameUI.UpdateTimerUI(levelTimer);
    }

    private void CreateManagersIfNeeded()
    {
        if (AudioManager.instance == null)
            Instantiate(audioManager);

        if (PlayerManager.instance == null)
            Instantiate(playerManager);

        if (SkinManager.instance == null)
            Instantiate(skinManager);

        if (DifficultyManager.instance == null)
            Instantiate(difficultyManager);

        if (ObjectCreator.instance == null)
            Instantiate(objectCreator);
    }

    private void CollectFruitInfo()
    {
        Fruit[] allFruit = FindObjectsByType<Fruit>(FindObjectsSortMode.None);
        totalFruit = allFruit.Length;

        inGameUI.UpdateFruitUI(fruitCollected, totalFruit);

        if (FirebaseManager.CurrentUser != null)
        {
            string levelKey = "Level" + currentLevelIndex;
            if (!FirebaseManager.CurrentUser.levelProgress.ContainsKey(levelKey))
            {
                FirebaseManager.CurrentUser.levelProgress[levelKey] = new LevelStats();
            }
        }
    }

    [ContextMenu("Parent All Fruit")]
    private void ParentAllTheFruit()
    {
        if (fruitParent == null)
            return;

        Fruit[] allFruit = FindObjectsByType<Fruit>(FindObjectsSortMode.None);

        foreach (Fruit fruit in allFruit)
        {
            fruit.transform.parent = fruitParent;
        }
    }

    public void AddFruit()
    {
        fruitCollected++;
        inGameUI.UpdateFruitUI(fruitCollected, totalFruit);
    }

    public void RemoveFruit()
    {
        fruitCollected--;
        inGameUI.UpdateFruitUI(fruitCollected, totalFruit);
    }

    public int FruitCollected() => fruitCollected;

    public bool FruitHaveRandomLook() => fruitAreRandom;

    public void EnemyKilled()
    {
        enemiesKilled++;
    }

    public void PlayerKnockedBack()
    {
        knockBacks++;
    }

    public void LevelFinished()
    {
        SaveLevelProgression();

        SaveBestTime();
        SaveFruitInfo();
        SaveEnemiesKilled();
        SaveKnockBacks();
        SaveTotalTimePlayGame();

        SaveAverageFruit();
        SaveAverageTime();
        SaveAverageEnemiesKilled();
        SaveAverageKnockBack();

        SaveDifficultyLevelCompletedCount();

        UpdateDailyStats();

        SaveCurrentUserData();

        LoadNextScene();
    }

    private void UpdateDailyStats()
    {
        UserData currentUserData = FirebaseManager.CurrentUser;

        if (currentUserData != null)
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");

            if (!currentUserData.dailyStatsHistory.ContainsKey(today))
            {
                currentUserData.dailyStatsHistory[today] = new DailyStats();
            }

            DailyStats dailyStats = currentUserData.dailyStatsHistory[today];

            dailyStats.completedLevelCount++;
            dailyStats.totalFruitAmount += fruitCollected;
            dailyStats.totalTimePlayGame += levelTimer;
            dailyStats.enemiesKilled += enemiesKilled;
            dailyStats.knockBacks += knockBacks;

            Debug.Log($"DailyStats have been updated {today}");
        }
        else
        {
            Debug.LogWarning("FirebaseManager.CurrentUser is null. Cannot update daily stats.");
        }

        FirebaseManager.instance.UpdateDataAfterLevelComplete();
    }

    private void SaveFruitInfo()
    {
        if (FirebaseManager.CurrentUser == null) return;

        string levelKey = "Level" + currentLevelIndex;
        if (!FirebaseManager.CurrentUser.levelProgress.ContainsKey(levelKey))
        {
            FirebaseManager.CurrentUser.levelProgress[levelKey] = new LevelStats();
        }

        LevelStats levelStats = FirebaseManager.CurrentUser.levelProgress[levelKey];

        levelStats.totalFruitsInLevel = totalFruit;

        int fruitCollectedBefore = levelStats.bestFruitCollected;

        if (fruitCollectedBefore < fruitCollected)
        {
            levelStats.bestFruitCollected = fruitCollected;
        }

        FirebaseManager.CurrentUser.totalFruitAmount += fruitCollected;
    }

    private void SaveAverageFruit()
    {
        if (FirebaseManager.CurrentUser == null) return;

        float currentAverageFruit = FirebaseManager.CurrentUser.averageFruit;
        int currentCompletedSessionsCount = FirebaseManager.CurrentUser.completedLevelCount;

        float newAverageFruit;
        if (currentCompletedSessionsCount == 0)
        {
            newAverageFruit = fruitCollected;
        }
        else
        {
            newAverageFruit = ((currentAverageFruit * currentCompletedSessionsCount) + fruitCollected) / (currentCompletedSessionsCount + 1);
        }

        FirebaseManager.CurrentUser.averageFruit = newAverageFruit;
    }

    private void SaveBestTime()
    {
        if (FirebaseManager.CurrentUser == null) return;

        string levelKey = "Level" + currentLevelIndex;
        if (!FirebaseManager.CurrentUser.levelProgress.ContainsKey(levelKey))
        {
            FirebaseManager.CurrentUser.levelProgress[levelKey] = new LevelStats();
        }

        float lastTime = FirebaseManager.CurrentUser.levelProgress[levelKey].bestTime;

        if (levelTimer < lastTime)
        {
            FirebaseManager.CurrentUser.levelProgress[levelKey].bestTime = levelTimer;
        }
    }

    private void SaveTotalTimePlayGame()
    {
        if (FirebaseManager.CurrentUser == null) return;

        FirebaseManager.CurrentUser.totalTimePlayGame += levelTimer;
    }

    private void SaveAverageTime()
    {
        if (FirebaseManager.CurrentUser == null) return;

        float currentAverageTime = FirebaseManager.CurrentUser.averageTime;
        int currentCompletedSessionsCount = FirebaseManager.CurrentUser.completedLevelCount;

        float newAverageTime;
        if (currentCompletedSessionsCount == 0)
        {
            newAverageTime = levelTimer;
        }
        else
        {
            newAverageTime = ((currentAverageTime * currentCompletedSessionsCount) + levelTimer) / (currentCompletedSessionsCount + 1);
        }

        FirebaseManager.CurrentUser.averageTime = newAverageTime;
    }

    private void SaveEnemiesKilled()
    {
        if (FirebaseManager.CurrentUser == null) return;

        FirebaseManager.CurrentUser.enemiesKilled += enemiesKilled;
    }

    private void SaveAverageEnemiesKilled()
    {
        if (FirebaseManager.CurrentUser == null) return;

        float currentAverageEnemiesKilled = FirebaseManager.CurrentUser.averageEnemiesKilled;
        int currentCompletedSessionsCount = FirebaseManager.CurrentUser.completedLevelCount;

        float newAverageEnemiesKilled;
        if (currentCompletedSessionsCount == 0)
        {
            newAverageEnemiesKilled = enemiesKilled;
        }
        else
        {
            newAverageEnemiesKilled = ((currentAverageEnemiesKilled * currentCompletedSessionsCount) + enemiesKilled) / (currentCompletedSessionsCount + 1);
        }

        FirebaseManager.CurrentUser.averageEnemiesKilled = newAverageEnemiesKilled;
    }

    private void SaveKnockBacks()
    {
        if (FirebaseManager.CurrentUser == null) return;

        FirebaseManager.CurrentUser.knockBacks += knockBacks;
    }

    private void SaveAverageKnockBack()
    {
        if (FirebaseManager.CurrentUser == null) return;

        float currentAverageKnockBacks = FirebaseManager.CurrentUser.averageKnockBacks;
        int currentCompletedSessionsCount = FirebaseManager.CurrentUser.completedLevelCount;

        float newAverageKnockBacks;
        if (currentCompletedSessionsCount == 0)
        {
            newAverageKnockBacks = knockBacks;
        }
        else
        {
            newAverageKnockBacks = ((currentAverageKnockBacks * currentCompletedSessionsCount) + knockBacks) / (currentCompletedSessionsCount + 1);
        }

        FirebaseManager.CurrentUser.averageKnockBacks = newAverageKnockBacks;
    }

    private void SaveDifficultyLevelCompletedCount()
    {
        if (FirebaseManager.CurrentUser != null)
        {
            FirebaseManager.CurrentUser.completedLevelCount++;
            int currentDifficulty = FirebaseManager.CurrentUser.gameProgress.gameDifficulty;
            switch (currentDifficulty)
            {
                case 1:
                    FirebaseManager.CurrentUser.easyLevelCompleted++;
                    break;
                case 2:
                    FirebaseManager.CurrentUser.normalLevelCompleted++;
                    break;
                case 3:
                    FirebaseManager.CurrentUser.hardLevelCompleted++;
                    break;
            }
        }
    }

    private void SaveLevelProgression()
    {
        if (FirebaseManager.CurrentUser == null) return;

        if (NoMoreLevels() == false)
        {
            string nextLevelKey = "Level" + nextLevelIndex;
            if (!FirebaseManager.CurrentUser.levelProgress.ContainsKey(nextLevelKey))
            {
                FirebaseManager.CurrentUser.levelProgress[nextLevelKey] = new LevelStats();
            }
            FirebaseManager.CurrentUser.levelProgress[nextLevelKey].unlocked = true;


            FirebaseManager.CurrentUser.gameProgress.continueLevelNumber = nextLevelIndex;

            SkinManager skinManager = SkinManager.instance;

            if (skinManager != null)
            {
                FirebaseManager.CurrentUser.gameProgress.lastUsedSkin = skinManager.GetSkinId();
            }
        }
    }

    private void SaveCurrentUserData()
    {
        if (FirebaseManager.instance != null && FirebaseManager.CurrentUser != null)
        {
            FirebaseManager.instance.SaveUserDataToRealtimeDatabase();
        }
    }

    public void RestartLevel()
    {
        UI_InGame.instance.fadeEffect.ScreenFade(1, .75f, LoadCurrentScene);
    }

    private void LoadCurrentScene() => SceneManager.LoadScene("Level_" + currentLevelIndex);
    private void LoadTheEndScene() => SceneManager.LoadScene("TheEnd");
    private void LoadNextLevel()
    {
        SceneManager.LoadScene("Level_" + nextLevelIndex);
    }
    private void LoadNextScene()
    {
        UI_FadeEffect fadeEffect = UI_InGame.instance.fadeEffect;

        if (NoMoreLevels())
            fadeEffect.ScreenFade(1, 1.5f, LoadTheEndScene);
        else
            fadeEffect.ScreenFade(1, 1.5f, LoadNextLevel);
    }
    private bool NoMoreLevels()
    {
        int lastLevelIndex = SceneManager.sceneCountInBuildSettings - 4;
        bool noMoreLevels = currentLevelIndex == lastLevelIndex;

        return noMoreLevels;
    }
}