using System.Collections;
using System.Collections.Generic;
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

    [Header("Checkpoint")]
    public bool canReactive;

    [Header("Managers")]
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private SkinManager skinManager;
    [SerializeField] private DifficultyManager difficultyManager;
    [SerializeField] private ObjectCreator objectCreator;

    [Header("PlayerStats")]
    public float averageTimeStat;
    public int bestFruitCollectedStat;

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

        PlayerPrefs.SetInt("Level" + currentLevelIndex + "TotalFruit", totalFruit);
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

    public void LevelFinished()
    {
        SaveLevelProgression();
        SaveBestTime();
        SaveFruitInfo();

        SaveAverageTime();

        LoadNextScene();
    }

    private void SaveFruitInfo()
    {
        int fruitCollectedBefore = PlayerPrefs.GetInt("Level" + currentLevelIndex + "FruitCollected");

        if (fruitCollectedBefore < fruitCollected)
            PlayerPrefs.SetInt("Level" + currentLevelIndex + "FruitCollected", fruitCollected);

        int totalFruitInBank = PlayerPrefs.GetInt("TotalFruitAmount");
        PlayerPrefs.SetInt("TotalFruitAmount", totalFruitInBank + fruitCollected);

        bestFruitCollectedStat = fruitCollectedBefore;
    }

    private void SaveBestTime()
    {
        float lastTime = PlayerPrefs.GetFloat("Level" + currentLevelIndex + "BestTime", 99);

        if (levelTimer < lastTime)
            PlayerPrefs.SetFloat("Level" + currentLevelIndex + "BestTime", levelTimer);
    }

    private void SaveAverageTime()
    {
        float averageTime = PlayerPrefs.GetFloat("AverageTime", 0);
        int levelCount = PlayerPrefs.GetInt("LevelCount", 0);

        if (levelCount == 0)
            averageTime = levelTimer;
        else
            averageTime = ((averageTime * levelCount) + levelTimer) / (levelCount + 1);

        PlayerPrefs.SetFloat("AverageTime", averageTime);
        PlayerPrefs.SetInt("LevelCount", levelCount + 1);
    }

    private void SaveEnemiesKilled()
    {

    }

    private void SaveKnockBacks()
    {

    }

    private void SaveLevelProgression()
    {
        PlayerPrefs.SetInt("Level" + nextLevelIndex + "Unlocked", 1);

        if (NoMoreLevels() == false)
        {
            PlayerPrefs.SetInt("ContinueLevelNumber", nextLevelIndex);

            SkinManager skinManager = SkinManager.instance;

            if (skinManager != null)
                PlayerPrefs.SetInt("LastUsedSkin", skinManager.GetSkinId());
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
        int lastLevelIndex = SceneManager.sceneCountInBuildSettings - 2; // 2 is MainMenu and TheEnd scene
        bool noMoreLevels = currentLevelIndex == lastLevelIndex;

        return noMoreLevels;
    }
}