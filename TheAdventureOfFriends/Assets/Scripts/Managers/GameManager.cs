using System.Collections;
using System.Collections.Generic;
using Firebase.Auth;
using UnityEditor.SearchService;
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

        currentLevelIndex = SceneManager.GetActiveScene().buildIndex;

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

        if (Authentication.CurrentUser != null)
        {
            string levelKey = "Level" + currentLevelIndex;
            if (!Authentication.CurrentUser.levelProgress.ContainsKey(levelKey))
            {
                Authentication.CurrentUser.levelProgress[levelKey] = new LevelStats();
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

        SaveAverageFruit();
        SaveAverageTime();
        SaveAverageEnemiesKilled();
        SaveAverageKnockBack();

        if (Authentication.CurrentUser != null)
        {
            Authentication.CurrentUser.completedLevelCount++;
        }

        SaveCurrentUserData();

        LoadNextScene();
    }

    private void SaveFruitInfo()
    {
        if (Authentication.CurrentUser == null) return;

        string levelKey = "Level" + currentLevelIndex;
        if (!Authentication.CurrentUser.levelProgress.ContainsKey(levelKey))
        {
            Authentication.CurrentUser.levelProgress[levelKey] = new LevelStats();
        }

        int fruitCollectedBefore = Authentication.CurrentUser.levelProgress[levelKey].bestFruitCollected;

        if (fruitCollectedBefore < fruitCollected)
        {
            Authentication.CurrentUser.levelProgress[levelKey].bestFruitCollected = fruitCollected;
        }

        Authentication.CurrentUser.totalFruitAmount += fruitCollected;
    }

    private void SaveAverageFruit()
    {
        if (Authentication.CurrentUser == null) return;

        float currentAverageFruit = Authentication.CurrentUser.averageFruit;
        int currentCompletedSessionsCount = Authentication.CurrentUser.completedLevelCount;

        float newAverageFruit;
        if (currentCompletedSessionsCount == 0)
        {
            newAverageFruit = fruitCollected;
        }
        else
        {
            newAverageFruit = ((currentAverageFruit * currentCompletedSessionsCount) + fruitCollected) / (currentCompletedSessionsCount + 1);
        }

        Authentication.CurrentUser.averageFruit = newAverageFruit;
    }

    private void SaveBestTime()
    {
        if (Authentication.CurrentUser == null) return;

        string levelKey = "Level" + currentLevelIndex;
        if (!Authentication.CurrentUser.levelProgress.ContainsKey(levelKey))
        {
            Authentication.CurrentUser.levelProgress[levelKey] = new LevelStats();
        }

        float lastTime = Authentication.CurrentUser.levelProgress[levelKey].bestTime;

        if (levelTimer < lastTime)
        {
            Authentication.CurrentUser.levelProgress[levelKey].bestTime = levelTimer;
        }
    }

    private void SaveAverageTime()
    {
        if (Authentication.CurrentUser == null) return;

        float currentAverageTime = Authentication.CurrentUser.averageTime;
        int currentCompletedSessionsCount = Authentication.CurrentUser.completedLevelCount;

        float newAverageTime;
        if (currentCompletedSessionsCount == 0)
        {
            newAverageTime = levelTimer;
        }
        else
        {
            newAverageTime = ((currentAverageTime * currentCompletedSessionsCount) + levelTimer) / (currentCompletedSessionsCount + 1);
        }

        Authentication.CurrentUser.averageTime = newAverageTime;
    }

    private void SaveEnemiesKilled()
    {
        if (Authentication.CurrentUser == null) return;

        Authentication.CurrentUser.enemiesKilled += enemiesKilled;
    }

    private void SaveAverageEnemiesKilled()
    {
        if (Authentication.CurrentUser == null) return;

        float currentAverageEnemiesKilled = Authentication.CurrentUser.averageEnemiesKilled;
        int currentCompletedSessionsCount = Authentication.CurrentUser.completedLevelCount;

        float newAverageEnemiesKilled;
        if (currentCompletedSessionsCount == 0)
        {
            newAverageEnemiesKilled = enemiesKilled;
        }
        else
        {
            newAverageEnemiesKilled = ((currentAverageEnemiesKilled * currentCompletedSessionsCount) + enemiesKilled) / (currentCompletedSessionsCount + 1);
        }

        Authentication.CurrentUser.averageEnemiesKilled = newAverageEnemiesKilled;
    }

    private void SaveKnockBacks()
    {
        if (Authentication.CurrentUser == null) return;

        Authentication.CurrentUser.knockBacks += knockBacks;
    }

    private void SaveAverageKnockBack()
    {
        if (Authentication.CurrentUser == null) return;

        float currentAverageKnockBacks = Authentication.CurrentUser.averageKnockBacks;
        int currentCompletedSessionsCount = Authentication.CurrentUser.completedLevelCount;

        float newAverageKnockBacks;
        if (currentCompletedSessionsCount == 0)
        {
            newAverageKnockBacks = knockBacks;
        }
        else
        {
            newAverageKnockBacks = ((currentAverageKnockBacks * currentCompletedSessionsCount) + knockBacks) / (currentCompletedSessionsCount + 1);
        }

        Authentication.CurrentUser.averageKnockBacks = newAverageKnockBacks;
    }

    private void SaveLevelProgression()
    {
        if (Authentication.CurrentUser == null) return;

        string nextLevelKey = "Level" + nextLevelIndex;
        if (!Authentication.CurrentUser.levelProgress.ContainsKey(nextLevelKey))
        {
            Authentication.CurrentUser.levelProgress[nextLevelKey] = new LevelStats();
        }
        Authentication.CurrentUser.levelProgress[nextLevelKey].unlocked = true;

        if (NoMoreLevels() == false)
        {
            Authentication.CurrentUser.gameProgress.continueLevelNumber = nextLevelIndex;

            SkinManager skinManager = SkinManager.instance;

            if (skinManager != null)
            {
                Authentication.CurrentUser.gameProgress.lastUsedSkin = skinManager.GetSkinId();
            }
        }
    }

    private void SaveCurrentUserData()
    {
        if (Authentication.instance != null && Authentication.CurrentUser != null)
        {
            Authentication.instance.SaveUserDataToRealtimeDatabase();
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
        int lastLevelIndex = SceneManager.sceneCountInBuildSettings - 3;
        bool noMoreLevels = currentLevelIndex == lastLevelIndex;

        return noMoreLevels;
    }
}