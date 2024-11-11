using System.Collections;
using System.Collections.Generic;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Player")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private float respawnDelay;
    public Player player;

    [Header("Fruit Management")]
    public bool fruitAreRandom;
    public int fruitCollected;
    public int totalFruit;

    [Header("Checkpoint")]
    public bool canReactive;

    [Header("Traps")]
    public GameObject arrowPrefab;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        CollectFruitInfo();
    }

    private void CollectFruitInfo()
    {
        Fruit[] allFruit = FindObjectsByType<Fruit>(FindObjectsSortMode.None);
        totalFruit = allFruit.Length;
    }

    public void UpdateRespawnPosition(Transform newRespawnPoint) => respawnPoint = newRespawnPoint;

    public void RespawnPlayer() => StartCoroutine(RespawnCoroutine());

    private IEnumerator RespawnCoroutine()
    {
        yield return new WaitForSeconds(respawnDelay);

        GameObject newPlayer = Instantiate(playerPrefab, respawnPoint.position, Quaternion.identity);
        player = newPlayer.GetComponent<Player>();
    }

    public void AddFruit() => fruitCollected++;

    public bool FruitHaveRandomLook() => fruitAreRandom;

    public void CreateObject(GameObject prefab, Transform target, float delay = 0)
    {
        StartCoroutine(CreateObjectCoroutine(prefab, target, delay));
    }
    private IEnumerator CreateObjectCoroutine(GameObject prefab, Transform target, float delay)
    {
        Vector3 newPosition = target.position;

        yield return new WaitForSeconds(delay);

        GameObject newObject = Instantiate(prefab, newPosition, Quaternion.identity);
    }

    public void LoadTheEndScene() => SceneManager.LoadScene("TheEnd");

    public void LevelFinished()
    {
        // SaveLevelProgression();
        // SaveBestTime();
        // SaveFruitsInfo();

        // LoadNextScene();
        UI_InGame.instance.fadeEffect.ScreenFade(1, .75f, LoadTheEndScene);
    }

    // private void SaveFruitsInfo()
    // {
    //     int fruitsCollectedBefore = PlayerPrefs.GetInt("Level" + currentLevelIndex + "FruitsCollected");

    //     if(fruitsCollectedBefore < fruitsCollected)
    //         PlayerPrefs.SetInt("Level" + currentLevelIndex + "FruitsCollected",fruitsCollected);

    //     int totalFruitsInBank = PlayerPrefs.GetInt("TotalFruitsAmount");
    //     PlayerPrefs.SetInt("TotalFruitsAmount", totalFruitsInBank + fruitsCollected);
    // }
    // private void SaveBestTime()
    // {
    //     float lastTime = PlayerPrefs.GetFloat("Level" + currentLevelIndex + "BestTime", 99);

    //     if(levelTimer < lastTime)
    //         PlayerPrefs.SetFloat("Level" + currentLevelIndex + "BestTime", levelTimer);
    // }
    // private void SaveLevelProgression()
    // {
    //     PlayerPrefs.SetInt("Level" + nextLevelIndex + "Unlocked", 1);

    //     if (NoMoreLevels() == false)
    //         PlayerPrefs.SetInt("ContinueLevelNumber", nextLevelIndex);
    // }

    // public void RestartLevel()
    // {
    //     UI_InGame.instance.fadeEffect.ScreenFade(1, .75f, LoadCurrentScene);
    // }

    // private void LoadCurrentScene() => SceneManager.LoadScene("Level_" + currentLevelIndex);
    // private void LoadTheEndScene() => SceneManager.LoadScene("TheEnd");
    // private void LoadNextLevel()
    // {
    //     SceneManager.LoadScene("Level_" + nextLevelIndex);
    // }
    // private void LoadNextScene()
    // {
    //     UI_FadeEffect fadeEffect = UI_InGame.instance.fadeEffect;

    //     if (NoMoreLevels())
    //         fadeEffect.ScreenFade(1, 1.5f, LoadTheEndScene);
    //     else
    //         fadeEffect.ScreenFade(1, 1.5f, LoadNextLevel);
    // }
    // private bool NoMoreLevels()
    // {
    //     int lastLevelIndex = SceneManager.sceneCountInBuildSettings - 2; // We have main menu and The End scene, that's why we use number 2
    //     bool noMoreLevels = currentLevelIndex == lastLevelIndex;

    //     return noMoreLevels;
    // }
}