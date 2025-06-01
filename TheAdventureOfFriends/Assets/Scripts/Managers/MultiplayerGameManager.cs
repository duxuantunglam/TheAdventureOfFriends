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
    public string gameStatus; // "playing", "finished"
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

    private UI_InGame inGameUI;

    [Header("Level Management")]
    [SerializeField] private float levelTimer;
    [SerializeField] private int currentLevelIndex;

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

    [Header("Multiplayer Settings")]
    [SerializeField] private float fruitWeight = 2.0f;
    [SerializeField] private float timeWeight = 1.5f;
    [SerializeField] private float enemyWeight = 1.0f;
    [SerializeField] private float knockbackWeight = 0.5f;

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
        inGameUI = UI_InGame.instance;

        // Initialize Firebase reference
        roomsRef = FirebaseDatabase.DefaultInstance.GetReference("Rooms");

        // Get current player info from Firebase
        if (FirebaseManager.CurrentUser != null)
        {
            currentPlayerId = FirebaseManager.CurrentUser.id;
            currentPlayerName = FirebaseManager.CurrentUser.userName;
        }

        // Get room ID from a static variable or PlayerPrefs (should be set when joining room)
        currentRoomId = PlayerPrefs.GetString("CurrentRoomId", "");

        if (string.IsNullOrEmpty(currentRoomId))
        {
            Debug.LogError("MultiplayerGameManager: No room ID found! Cannot initialize multiplayer game.");
            return;
        }

        currentLevelIndex = SceneManager.GetActiveScene().buildIndex;
        levelTimer = 0;

        InitializeMultiplayerGame();
        CollectFruitInfo();
        CreateManagersIfNeeded();

        // Force difficulty to Easy for multiplayer
        SetDifficultyToEasy();
    }

    private void Update()
    {
        if (!hasPlayerFinished)
        {
            levelTimer += Time.deltaTime;
            inGameUI.UpdateTimerUI(levelTimer);
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
        // Load existing game stats or create new ones
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

        // Initialize current player stats
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
        // Simple check: first player to join becomes player1
        // You might want to implement a more sophisticated logic
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
        Fruit[] allFruit = FindObjectsByType<Fruit>(FindObjectsSortMode.None);
        totalFruit = allFruit.Length;

        inGameUI.UpdateFruitUI(fruitCollected, totalFruit);
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
        if (hasPlayerFinished) return; // Prevent multiple calls

        hasPlayerFinished = true;

        // Calculate final score
        float finalScore = CalculateFinalScore();

        // Update current player's stats
        UpdateCurrentPlayerStats(finalScore);

        // Save to Firebase
        SaveGameStatsToFirebase();

        Debug.Log($"MultiplayerGameManager: Level finished! Score: {finalScore:F2}");

        // Return to waiting room after a short delay
        Invoke(nameof(ReturnToWaitingRoom), 3f);
    }

    private float CalculateFinalScore()
    {
        // Score calculation formula: higher is better
        // Fruit: more is better (positive)
        // Time: less is better (negative impact)  
        // Enemies: more is better (positive)
        // Knockbacks: less is better (negative impact)

        float fruitScore = fruitCollected * fruitWeight;
        float timeScore = Mathf.Max(0, (120f - levelTimer)) * timeWeight; // Bonus for finishing under 2 minutes
        float enemyScore = enemiesKilled * enemyWeight;
        float knockbackScore = Mathf.Max(0, (10 - knockBacks)) * knockbackWeight; // Penalty for knockbacks

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
            // Player not found, add as available slot
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

        // Update stats
        currentPlayerStats.fruitCollected = fruitCollected;
        currentPlayerStats.completionTime = levelTimer;
        currentPlayerStats.enemiesKilled = enemiesKilled;
        currentPlayerStats.knockBacks = knockBacks;
        currentPlayerStats.totalScore = finalScore;
        currentPlayerStats.hasFinished = true;

        // Check if both players finished to determine winner
        CheckForGameCompletion();
    }

    private void CheckForGameCompletion()
    {
        if (gameStats.player1.hasFinished && gameStats.player2.hasFinished)
        {
            // Both players finished, determine winner
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
                // Tie - could be handled differently
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

    private void ReturnToWaitingRoom()
    {
        // Update room status back to 'waiting'
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

        // Set flag for UI_WaitingRoom to detect return from multiplayer
        PlayerPrefs.SetString("ReturnFromMultiplayerRoom", currentRoomId);
        PlayerPrefs.Save();

        Debug.Log($"MultiplayerGameManager: Set return flag for room {currentRoomId}. Loading MainMenu scene.");

        // Load MainMenu scene to return to waiting room
        SceneManager.LoadScene("MainMenu");
    }

    public void RestartLevel()
    {
        // Reset stats
        fruitCollected = 0;
        enemiesKilled = 0;
        knockBacks = 0;
        levelTimer = 0;
        hasPlayerFinished = false;

        // Reset player stats in Firebase
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

        // Reload current scene
        UI_InGame.instance.fadeEffect.ScreenFade(1, .75f, LoadCurrentScene);
    }

    private void LoadCurrentScene() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);

    // Public method to get current game stats (useful for UI display)
    public MultiplayerGameStats GetGameStats()
    {
        return gameStats;
    }

    // Public method to get current player's stats
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