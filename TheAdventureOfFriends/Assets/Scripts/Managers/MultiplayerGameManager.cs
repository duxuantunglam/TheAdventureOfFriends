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
                return;
            }

            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                if (snapshot.Exists)
                {
                    string json = snapshot.GetRawJsonValue();
                    gameStats = JsonConvert.DeserializeObject<MultiplayerGameStats>(json);
                    Debug.Log("✅ MultiplayerGameManager: Game stats loaded successfully from Firebase.");
                    Debug.Log($"Player1: {gameStats.player1.playerName} ({gameStats.player1.playerId})");
                    Debug.Log($"Player2: {gameStats.player2.playerName} ({gameStats.player2.playerId})");
                }
                else
                {
                    Debug.LogError("❌ MultiplayerGameManager: GameStats should exist but not found! This indicates a problem with game initialization.");
                    // Không tạo mới nữa, vì gameStats phải được tạo trong UI_WaitingRoom
                }
            }
        });
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

        Debug.Log($"MultiplayerGameManager: Level finished! Score: {finalScore:F2}");

        // Show results panel via InGameUI
        if (inGameUI != null)
        {
            inGameUI.ShowResultsPanel();
            Debug.Log("MultiplayerGameManager: Results panel triggered!");
        }
        else
        {
            Debug.LogError("MultiplayerGameManager: InGameUI is null, cannot show results panel!");
        }
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
        if (gameStats == null)
        {
            Debug.LogError("MultiplayerGameManager: gameStats is null! Cannot update player stats.");
            return;
        }

        Debug.Log($"🔄 Loading latest gameStats before updating for player {currentPlayerId}...");

        // Load gameStats mới nhất từ Firebase trước khi cập nhật
        roomsRef.Child(currentRoomId).Child("gameStats").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError($"❌ Failed to load latest gameStats: {task.Exception}");
                // Fallback: sử dụng gameStats local hiện tại
                UpdatePlayerStatsWithLatestData(gameStats, finalScore);
                return;
            }

            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                if (snapshot.Exists)
                {
                    try
                    {
                        string json = snapshot.GetRawJsonValue();
                        MultiplayerGameStats latestGameStats = JsonConvert.DeserializeObject<MultiplayerGameStats>(json);

                        Debug.Log($"✅ Latest gameStats loaded successfully!");
                        Debug.Log($"Latest Player1 finished: {latestGameStats.player1.hasFinished}, score: {latestGameStats.player1.totalScore}");
                        Debug.Log($"Latest Player2 finished: {latestGameStats.player2.hasFinished}, score: {latestGameStats.player2.totalScore}");

                        // Cập nhật gameStats local với dữ liệu mới nhất
                        gameStats = latestGameStats;

                        // Cập nhật stats với dữ liệu mới nhất
                        UpdatePlayerStatsWithLatestData(latestGameStats, finalScore);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"❌ Failed to deserialize latest gameStats: {e.Message}");
                        // Fallback: sử dụng gameStats local hiện tại
                        UpdatePlayerStatsWithLatestData(gameStats, finalScore);
                    }
                }
                else
                {
                    Debug.LogError("❌ Latest gameStats not found on Firebase!");
                    // Fallback: sử dụng gameStats local hiện tại
                    UpdatePlayerStatsWithLatestData(gameStats, finalScore);
                }
            }
        });
    }

    private void UpdatePlayerStatsWithLatestData(MultiplayerGameStats latestGameStats, float finalScore)
    {
        MultiplayerPlayerStats currentPlayerStats = null;

        if (latestGameStats.player1.playerId == currentPlayerId)
        {
            currentPlayerStats = latestGameStats.player1;
            Debug.Log($"✅ Updating stats for Player1: {latestGameStats.player1.playerName}");
        }
        else if (latestGameStats.player2.playerId == currentPlayerId)
        {
            currentPlayerStats = latestGameStats.player2;
            Debug.Log($"✅ Updating stats for Player2: {latestGameStats.player2.playerName}");
        }
        else
        {
            Debug.LogError($"❌ MultiplayerGameManager: Current player {currentPlayerId} not found in latest gameStats! Player1: {latestGameStats.player1.playerId}, Player2: {latestGameStats.player2.playerId}");
            return;
        }

        // Kiểm tra nếu player này đã hoàn thành trước đó
        if (currentPlayerStats.hasFinished)
        {
            Debug.LogWarning($"⚠️ Player {currentPlayerStats.playerName} has already finished! Score: {currentPlayerStats.totalScore}. Skipping update to prevent overwrite.");
            return;
        }

        // Cập nhật stats cho player hiện tại
        currentPlayerStats.fruitCollected = fruitCollected;
        currentPlayerStats.completionTime = gameTimer;
        currentPlayerStats.enemiesKilled = enemiesKilled;
        currentPlayerStats.knockBacks = knockBacks;
        currentPlayerStats.totalScore = finalScore;
        currentPlayerStats.hasFinished = true;

        Debug.Log($"🎯 Final stats for {currentPlayerStats.playerName}: Fruits={fruitCollected}, Time={gameTimer:F1}s, Enemies={enemiesKilled}, Knockbacks={knockBacks}, Score={finalScore:F1}");

        // Cập nhật gameStats local với dữ liệu mới nhất
        gameStats = latestGameStats;

        CheckForGameCompletion();

        // Lưu với dữ liệu đã được cập nhật
        SaveGameStatsToFirebase();
    }

    private void CheckForGameCompletion()
    {
        Debug.Log($"🔍 Checking game completion...");
        Debug.Log($"Player1 ({gameStats.player1.playerName}) finished: {gameStats.player1.hasFinished}, score: {gameStats.player1.totalScore}");
        Debug.Log($"Player2 ({gameStats.player2.playerName}) finished: {gameStats.player2.hasFinished}, score: {gameStats.player2.totalScore}");

        if (gameStats.player1.hasFinished && gameStats.player2.hasFinished)
        {
            Debug.Log("🎉 Both players have finished! Determining winner...");

            if (gameStats.player1.totalScore > gameStats.player2.totalScore)
            {
                gameStats.winnerId = gameStats.player1.playerId;
                gameStats.winnerName = gameStats.player1.playerName;
                Debug.Log($"🏆 Player1 ({gameStats.player1.playerName}) wins with score {gameStats.player1.totalScore} vs {gameStats.player2.totalScore}");
            }
            else if (gameStats.player2.totalScore > gameStats.player1.totalScore)
            {
                gameStats.winnerId = gameStats.player2.playerId;
                gameStats.winnerName = gameStats.player2.playerName;
                Debug.Log($"🏆 Player2 ({gameStats.player2.playerName}) wins with score {gameStats.player2.totalScore} vs {gameStats.player1.totalScore}");
            }
            else
            {
                gameStats.winnerName = "Tie";
                Debug.Log($"🤝 It's a tie! Both players scored {gameStats.player1.totalScore}");
            }

            gameStats.gameStatus = "finished";
            Debug.Log($"✅ MultiplayerGameManager: Game completed! Winner: {gameStats.winnerName}");
        }
        else
        {
            Debug.Log("⏳ Game not completed yet - waiting for other player...");
        }
    }

    private void SaveGameStatsToFirebase()
    {
        if (gameStats == null || string.IsNullOrEmpty(currentRoomId)) return;

        Debug.Log($"💾 Saving gameStats to Firebase for room {currentRoomId}...");
        Debug.Log($"Player1: {gameStats.player1.playerName} - Finished: {gameStats.player1.hasFinished}, Score: {gameStats.player1.totalScore}");
        Debug.Log($"Player2: {gameStats.player2.playerName} - Finished: {gameStats.player2.hasFinished}, Score: {gameStats.player2.totalScore}");
        Debug.Log($"Game Status: {gameStats.gameStatus}, Winner: {gameStats.winnerName}");

        string json = JsonConvert.SerializeObject(gameStats);
        roomsRef.Child(currentRoomId).Child("gameStats").SetRawJsonValueAsync(json)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError($"❌ MultiplayerGameManager: Failed to save game stats: {task.Exception}");
                }
                else if (task.IsCompleted)
                {
                    Debug.Log("✅ MultiplayerGameManager: Game stats saved successfully to Firebase.");
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