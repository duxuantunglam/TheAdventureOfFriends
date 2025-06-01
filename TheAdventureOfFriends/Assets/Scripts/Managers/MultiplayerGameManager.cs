using System;
using Firebase.Database;
using Firebase.Extensions;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class MultiplayerPlayerStats
{
    public string playerId;
    public string playerName;
    public int fruitCollected;
    public float completionTime;
    public int enemiesKilled;
    public int knockBacks;
    public float totalScore;
    public bool hasFinished;

    public MultiplayerPlayerStats()
    {
        fruitCollected = 0;
        completionTime = 0f;
        enemiesKilled = 0;
        knockBacks = 0;
        totalScore = 0f;
        hasFinished = false;
    }
}

[Serializable]
public class MultiplayerGameStats
{
    public MultiplayerPlayerStats player1;
    public MultiplayerPlayerStats player2;
    public string gameStatus;
    public string winnerId;
    public string winnerName;

    public MultiplayerGameStats()
    {
        player1 = new MultiplayerPlayerStats();
        player2 = new MultiplayerPlayerStats();
        gameStatus = "playing";
        winnerId = "";
        winnerName = "";
    }
}

public class MultiplayerGameManager : MonoBehaviour
{
    public static MultiplayerGameManager instance;

    private Multiplayer_InGameUI inGameUI;

    [Header("Level Management")]
    [SerializeField] private float gameTimer;

    [Header("Fruit Management")]
    public bool fruitAreRandom;
    public int fruitCollected;
    public int totalFruit;
    public Transform fruitParent;

    [Header("Enemy Management")]
    public int enemiesKilled;
    [Header("Knockback Management")]
    public int knockBacks;

    [Header("Managers")]
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private SkinManager skinManager;
    [SerializeField] private DifficultyManager difficultyManager;
    [SerializeField] private ObjectCreator objectCreator;

    [Header("Multiplayer Settings")]
    [SerializeField] private float fruitWeight = 2.0f;
    [SerializeField] private float timeWeight = 1.5f;
    [SerializeField] private float enemyWeight = 1.0f;
    [SerializeField] private float knockbackWeight = -1.0f;

    // Multiplayer specific variables
    private string currentRoomId;
    private string currentPlayerId;
    private string currentPlayerName;
    private MultiplayerGameStats gameStats;
    private bool hasPlayerFinished = false;
    private DatabaseReference roomsRef;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        inGameUI = Multiplayer_InGameUI.instance;

        roomsRef = FirebaseDatabase.DefaultInstance.GetReference("Rooms");

        if (FirebaseManager.CurrentUser != null)
        {
            currentPlayerId = FirebaseManager.CurrentUser.id;
            currentPlayerName = FirebaseManager.CurrentUser.userName;
        }

        // Get room ID from new PlayerPrefs key set by UI_WaitingRoom
        currentRoomId = PlayerPrefs.GetString("CurrentMultiplayerRoomId", "");

        if (string.IsNullOrEmpty(currentRoomId))
        {
            Debug.LogError("MultiplayerGameManager: No room ID found! Cannot initialize multiplayer game.");
            return;
        }

        Debug.Log($"MultiplayerGameManager: Initializing game for room {currentRoomId}, player {currentPlayerId}");

        gameTimer = 0;

        // Track that this player has entered the Multiplayer scene
        TrackPlayerPresenceInScene();

        InitializeMultiplayerGame();
        CollectFruitInfo();
        CreateManagersIfNeeded();

        SetDifficultyToEasy();
    }

    private void Update()
    {
        if (!hasPlayerFinished)
        {
            gameTimer += Time.deltaTime;
            inGameUI.UpdateMultiplayerTimerUI(gameTimer);
        }
    }

    private void SetDifficultyToEasy()
    {
        if (DifficultyManager.instance != null)
        {
            DifficultyManager.instance.SetDifficulty(DifficultyType.Easy);
            Debug.Log("MultiplayerGameManager: Difficulty set to Easy for multiplayer mode.");
        }
    }

    private void InitializeMultiplayerGame()
    {
        LoadMultiplayerGameStats();
    }

    private void LoadMultiplayerGameStats()
    {
        roomsRef.Child(currentRoomId).Child("gameStats").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("MultiplayerGameManager: Failed to load game stats: " + task.Exception);
                CreateNewGameStats();
                return;
            }

            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                if (snapshot.Exists)
                {
                    string json = snapshot.GetRawJsonValue();
                    gameStats = JsonConvert.DeserializeObject<MultiplayerGameStats>(json);
                    Debug.Log("MultiplayerGameManager: Game stats loaded successfully.");
                }
                else
                {
                    CreateNewGameStats();
                }
            }
        });
    }

    private void CreateNewGameStats()
    {
        gameStats = new MultiplayerGameStats();

        if (IsPlayer1())
        {
            gameStats.player1.playerId = currentPlayerId;
            gameStats.player1.playerName = currentPlayerName;
        }
        else
        {
            gameStats.player2.playerId = currentPlayerId;
            gameStats.player2.playerName = currentPlayerName;
        }

        SaveGameStatsToFirebase();
        Debug.Log("MultiplayerGameManager: New game stats created.");
    }

    private bool IsPlayer1()
    {
        return gameStats == null || string.IsNullOrEmpty(gameStats.player1.playerId);
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
        MultiplayerFruit[] allFruit = FindObjectsByType<MultiplayerFruit>(FindObjectsSortMode.None);
        totalFruit = allFruit.Length;

        inGameUI.UpdateMultiplayerFruitUI(fruitCollected, totalFruit);
    }

    [ContextMenu("Parent All Fruit")]
    private void ParentAllTheFruit()
    {
        if (fruitParent == null)
            return;

        MultiplayerFruit[] allFruit = FindObjectsByType<MultiplayerFruit>(FindObjectsSortMode.None);

        foreach (MultiplayerFruit fruit in allFruit)
        {
            fruit.transform.parent = fruitParent;
        }
    }

    public void AddFruit()
    {
        fruitCollected++;
        inGameUI.UpdateMultiplayerFruitUI(fruitCollected, totalFruit);
    }

    public void RemoveFruit()
    {
        fruitCollected--;
        inGameUI.UpdateMultiplayerFruitUI(fruitCollected, totalFruit);
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
        if (hasPlayerFinished) return;

        hasPlayerFinished = true;

        float finalScore = CalculateFinalScore();

        UpdateCurrentPlayerStats(finalScore);

        SaveGameStatsToFirebase();

        Debug.Log($"MultiplayerGameManager: Level finished! Score: {finalScore:F2}");

        // Instead of returning to waiting room, show results panel
        // The results panel will be handled in BƯỚC 2
        Debug.Log("MultiplayerGameManager: Player finished! Results panel will be shown.");
    }

    private float CalculateFinalScore()
    {
        float fruitScore = fruitCollected * fruitWeight;
        float timeScore = gameTimer * timeWeight;
        float enemyScore = enemiesKilled * enemyWeight;
        float knockbackScore = knockBacks * knockbackWeight;

        float totalScore = fruitScore + timeScore + enemyScore + knockbackScore;

        Debug.Log($"Score breakdown - Fruit: {fruitScore}, Time: {timeScore}, Enemy: {enemyScore}, Knockback: {knockbackScore}, Total: {totalScore}");

        return totalScore;
    }

    private void UpdateCurrentPlayerStats(float finalScore)
    {
        if (gameStats == null) return;

        MultiplayerPlayerStats currentPlayerStats;

        if (gameStats.player1.playerId == currentPlayerId)
        {
            currentPlayerStats = gameStats.player1;
        }
        else if (gameStats.player2.playerId == currentPlayerId)
        {
            currentPlayerStats = gameStats.player2;
        }
        else
        {
            if (string.IsNullOrEmpty(gameStats.player1.playerId))
            {
                gameStats.player1.playerId = currentPlayerId;
                gameStats.player1.playerName = currentPlayerName;
                currentPlayerStats = gameStats.player1;
            }
            else if (string.IsNullOrEmpty(gameStats.player2.playerId))
            {
                gameStats.player2.playerId = currentPlayerId;
                gameStats.player2.playerName = currentPlayerName;
                currentPlayerStats = gameStats.player2;
            }
            else
            {
                Debug.LogError("MultiplayerGameManager: No available player slot!");
                return;
            }
        }

        currentPlayerStats.fruitCollected = fruitCollected;
        currentPlayerStats.completionTime = gameTimer;
        currentPlayerStats.enemiesKilled = enemiesKilled;
        currentPlayerStats.knockBacks = knockBacks;
        currentPlayerStats.totalScore = finalScore;
        currentPlayerStats.hasFinished = true;

        CheckForGameCompletion();
    }

    private void CheckForGameCompletion()
    {
        if (gameStats.player1.hasFinished && gameStats.player2.hasFinished)
        {
            if (gameStats.player1.totalScore > gameStats.player2.totalScore)
            {
                gameStats.winnerId = gameStats.player1.playerId;
                gameStats.winnerName = gameStats.player1.playerName;
            }
            else if (gameStats.player2.totalScore > gameStats.player1.totalScore)
            {
                gameStats.winnerId = gameStats.player2.playerId;
                gameStats.winnerName = gameStats.player2.playerName;
            }
            else
            {
                gameStats.winnerName = "Tie";
            }

            gameStats.gameStatus = "finished";
            Debug.Log($"MultiplayerGameManager: Game completed! Winner: {gameStats.winnerName}");
        }
    }

    private void SaveGameStatsToFirebase()
    {
        if (gameStats == null || string.IsNullOrEmpty(currentRoomId)) return;

        string json = JsonConvert.SerializeObject(gameStats);
        roomsRef.Child(currentRoomId).Child("gameStats").SetRawJsonValueAsync(json)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("MultiplayerGameManager: Failed to save game stats: " + task.Exception);
                }
                else if (task.IsCompleted)
                {
                    Debug.Log("MultiplayerGameManager: Game stats saved successfully.");
                }
            });
    }

    private void TrackPlayerPresenceInScene()
    {
        if (string.IsNullOrEmpty(currentRoomId) || string.IsNullOrEmpty(currentPlayerId)) return;

        roomsRef.Child(currentRoomId).Child("playersInScene").Child(currentPlayerId).SetValueAsync(true)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError($"MultiplayerGameManager: Failed to track player presence: {task.Exception}");
                }
                else if (task.IsCompleted)
                {
                    Debug.Log($"MultiplayerGameManager: Player {currentPlayerId} presence tracked in scene.");
                }
            });
    }

    private void OnDestroy()
    {
        // Cleanup player presence when leaving scene
        if (!string.IsNullOrEmpty(currentRoomId) && !string.IsNullOrEmpty(currentPlayerId) && roomsRef != null)
        {
            roomsRef.Child(currentRoomId).Child("playersInScene").Child(currentPlayerId).RemoveValueAsync()
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsCompleted)
                    {
                        Debug.Log($"MultiplayerGameManager: Player {currentPlayerId} presence removed from scene.");
                    }
                });
        }
    }

    private void ReturnToWaitingRoom()
    {
        roomsRef.Child(currentRoomId).Child("status").SetValueAsync("waiting")
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("MultiplayerGameManager: Failed to update room status: " + task.Exception);
                }
                else if (task.IsCompleted)
                {
                    Debug.Log("MultiplayerGameManager: Room status updated to waiting.");
                }
            });

        PlayerPrefs.SetString("ReturnFromMultiplayerRoom", currentRoomId);
        PlayerPrefs.Save();

        Debug.Log($"MultiplayerGameManager: Set return flag for room {currentRoomId}. Loading MainMenu scene.");

        SceneManager.LoadScene("MainMenu");
    }

    public void RestartLevel()
    {
        fruitCollected = 0;
        enemiesKilled = 0;
        knockBacks = 0;
        gameTimer = 0;
        hasPlayerFinished = false;

        if (gameStats != null)
        {
            if (gameStats.player1.playerId == currentPlayerId)
            {
                gameStats.player1 = new MultiplayerPlayerStats
                {
                    playerId = currentPlayerId,
                    playerName = currentPlayerName
                };
            }
            else if (gameStats.player2.playerId == currentPlayerId)
            {
                gameStats.player2 = new MultiplayerPlayerStats
                {
                    playerId = currentPlayerId,
                    playerName = currentPlayerName
                };
            }

            SaveGameStatsToFirebase();
        }

        Multiplayer_InGameUI.instance.fadeEffect.ScreenFade(1, .75f, LoadCurrentScene);
    }

    private void LoadCurrentScene() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);

    public MultiplayerGameStats GetGameStats()
    {
        return gameStats;
    }

    public MultiplayerPlayerStats GetCurrentPlayerStats()
    {
        if (gameStats == null) return null;

        if (gameStats.player1.playerId == currentPlayerId)
            return gameStats.player1;
        else if (gameStats.player2.playerId == currentPlayerId)
            return gameStats.player2;

        return null;
    }
}