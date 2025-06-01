using Firebase.Database;
using Firebase.Extensions;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Multiplayer_InGameUI : MonoBehaviour
{
    public static Multiplayer_InGameUI instance;
    public UI_FadeEffect fadeEffect { get; private set; }

    [Header("In-Game UI")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI fruitText;

    [Header("Results Panel")]
    [SerializeField] private GameObject gameResultsPanel;
    [SerializeField] private TextMeshProUGUI player1ResultsText;
    [SerializeField] private TextMeshProUGUI player2ResultsText;
    [SerializeField] private TextMeshProUGUI winnerText;
    [SerializeField] private Button returnToMenuButton;

    private DatabaseReference roomsRef;
    private string currentRoomId;
    private string currentPlayerId;
    private bool isListeningToGameStats = false;

    private void Awake()
    {
        instance = this;
        fadeEffect = GetComponentInChildren<UI_FadeEffect>();

        roomsRef = FirebaseDatabase.DefaultInstance.GetReference("Rooms");

        if (returnToMenuButton != null)
        {
            returnToMenuButton.onClick.AddListener(ReturnToMainMenu);
        }
    }

    private void Start()
    {
        fadeEffect.ScreenFade(0, 1);

        currentRoomId = PlayerPrefs.GetString("CurrentMultiplayerRoomId", "");
        if (FirebaseManager.CurrentUser != null)
        {
            currentPlayerId = FirebaseManager.CurrentUser.id;
        }

        if (gameResultsPanel != null)
        {
            gameResultsPanel.SetActive(false);
        }

        StartListeningToGameStats();
    }

    private void OnDestroy()
    {
        StopListeningToGameStats();
    }

    public void UpdateMultiplayerFruitUI(int collectedFruit, int totalFruit)
    {
        fruitText.text = collectedFruit + "/" + totalFruit;
    }

    public void UpdateMultiplayerTimerUI(float timer)
    {
        timerText.text = timer.ToString("00") + " s";
    }

    public void ShowResultsPanel()
    {
        if (gameResultsPanel != null)
        {
            gameResultsPanel.SetActive(true);
            Debug.Log("Multiplayer_InGameUI: Results panel shown.");
        }
    }

    private void StartListeningToGameStats()
    {
        if (string.IsNullOrEmpty(currentRoomId) || isListeningToGameStats) return;

        roomsRef.Child(currentRoomId).Child("gameStats").ValueChanged += OnGameStatsChanged;
        isListeningToGameStats = true;
        Debug.Log($"Multiplayer_InGameUI: Started listening to game stats for room {currentRoomId}");
    }

    private void StopListeningToGameStats()
    {
        if (string.IsNullOrEmpty(currentRoomId) || !isListeningToGameStats) return;

        roomsRef.Child(currentRoomId).Child("gameStats").ValueChanged -= OnGameStatsChanged;
        isListeningToGameStats = false;
        Debug.Log($"Multiplayer_InGameUI: Stopped listening to game stats for room {currentRoomId}");
    }

    private void OnGameStatsChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError($"Multiplayer_InGameUI: Game stats listener error: {args.DatabaseError.Message}");
            return;
        }

        DataSnapshot snapshot = args.Snapshot;
        if (!snapshot.Exists) return;

        try
        {
            string json = snapshot.GetRawJsonValue();
            MultiplayerGameStats gameStats = JsonConvert.DeserializeObject<MultiplayerGameStats>(json);

            UpdateResultsUI(gameStats);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Multiplayer_InGameUI: Failed to parse game stats: {e.Message}");
        }
    }

    private void UpdateResultsUI(MultiplayerGameStats gameStats)
    {
        if (gameStats == null) return;

        if (player1ResultsText != null && !string.IsNullOrEmpty(gameStats.player1.playerId))
        {
            if (gameStats.player1.hasFinished)
            {
                string player1Results = $"{gameStats.player1.playerName}\n" +
                                      $"Score: {gameStats.player1.totalScore:F2}\n" +
                                      $"Time: {gameStats.player1.completionTime:F1}s\n" +
                                      $"Fruits: {gameStats.player1.fruitCollected}\n" +
                                      $"Enemies: {gameStats.player1.enemiesKilled}";
                player1ResultsText.text = player1Results;
            }
            else
            {
                player1ResultsText.text = $"{gameStats.player1.playerName}\nPlaying...";
            }
        }

        if (player2ResultsText != null && !string.IsNullOrEmpty(gameStats.player2.playerId))
        {
            if (gameStats.player2.hasFinished)
            {
                string player2Results = $"{gameStats.player2.playerName}\n" +
                                      $"Score: {gameStats.player2.totalScore:F2}\n" +
                                      $"Time: {gameStats.player2.completionTime:F1}s\n" +
                                      $"Fruits: {gameStats.player2.fruitCollected}\n" +
                                      $"Enemies: {gameStats.player2.enemiesKilled}";
                player2ResultsText.text = player2Results;
            }
            else
            {
                player2ResultsText.text = $"{gameStats.player2.playerName}\nPlaying...";
            }
        }

        if (winnerText != null)
        {
            if (gameStats.gameStatus == "finished")
            {
                if (gameStats.winnerName == "Tie")
                {
                    winnerText.text = "It's a Tie!";
                }
                else
                {
                    winnerText.text = $"{gameStats.winnerName} Wins!";
                }
                winnerText.gameObject.SetActive(true);
            }
            else
            {
                winnerText.gameObject.SetActive(false);
            }
        }

        if ((gameStats.player1.playerId == currentPlayerId && gameStats.player1.hasFinished) ||
            (gameStats.player2.playerId == currentPlayerId && gameStats.player2.hasFinished))
        {
            ShowResultsPanel();
        }
    }

    private void ReturnToMainMenu()
    {
        Debug.Log("Multiplayer_InGameUI: Returning to Main Menu...");

        if (!string.IsNullOrEmpty(currentRoomId) && !string.IsNullOrEmpty(currentPlayerId))
        {
            roomsRef.Child(currentRoomId).Child("playersInScene").Child(currentPlayerId).RemoveValueAsync()
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsCompleted)
                    {
                        Debug.Log("Multiplayer_InGameUI: Player presence removed from scene.");
                    }
                });

            CheckAndCleanupRoom();
        }

        PlayerPrefs.DeleteKey("CurrentMultiplayerRoomId");
        PlayerPrefs.Save();

        SceneManager.LoadScene("MainMenu");
    }

    private void CheckAndCleanupRoom()
    {
        roomsRef.Child(currentRoomId).Child("playersInScene").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError($"Multiplayer_InGameUI: Failed to check players in scene: {task.Exception}");
                return;
            }

            if (task.IsCompleted)
            {
                DataSnapshot playersInSceneSnapshot = task.Result;

                if (!playersInSceneSnapshot.Exists || playersInSceneSnapshot.ChildrenCount == 0)
                {
                    Debug.Log($"Multiplayer_InGameUI: No players left in scene. Cleaning up room {currentRoomId}");

                    roomsRef.Child(currentRoomId).RemoveValueAsync().ContinueWithOnMainThread(cleanupTask =>
                    {
                        if (cleanupTask.IsFaulted)
                        {
                            Debug.LogError($"Multiplayer_InGameUI: Failed to cleanup room: {cleanupTask.Exception}");
                        }
                        else if (cleanupTask.IsCompleted)
                        {
                            Debug.Log($"Multiplayer_InGameUI: Room {currentRoomId} cleaned up successfully.");
                        }
                    });
                }
            }
        });
    }
}