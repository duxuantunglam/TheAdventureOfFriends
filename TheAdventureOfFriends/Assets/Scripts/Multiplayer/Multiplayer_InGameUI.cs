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

    // Firebase tracking
    private DatabaseReference roomsRef;
    private string currentRoomId;
    private string currentPlayerId;
    private bool isListeningToGameStats = false;
    private bool hasCurrentPlayerFinished = false;

    private void Awake()
    {
        instance = this;
        fadeEffect = GetComponentInChildren<UI_FadeEffect>();

        roomsRef = FirebaseDatabase.DefaultInstance.GetReference("Rooms");

        AssignUIElementsFromScene();

        if (returnToMenuButton != null)
        {
            returnToMenuButton.onClick.AddListener(ReturnToMainMenu);
        }
    }

    private void AssignUIElementsFromScene()
    {
        if (gameResultsPanel == null)
        {
            gameResultsPanel = GameObject.Find("GameResultsPanel");
            if (gameResultsPanel == null)
                Debug.LogError("Multiplayer_InGameUI: GameResultsPanel not found in scene!");
        }

        if (player1ResultsText == null)
        {
            GameObject player1TextObj = GameObject.Find("Player1Results_Text");
            if (player1TextObj != null)
                player1ResultsText = player1TextObj.GetComponent<TextMeshProUGUI>();
            else
                Debug.LogError("Multiplayer_InGameUI: Player1Results_Text not found in scene!");
        }

        if (player2ResultsText == null)
        {
            GameObject player2TextObj = GameObject.Find("Player2Results_Text");
            if (player2TextObj != null)
                player2ResultsText = player2TextObj.GetComponent<TextMeshProUGUI>();
            else
                Debug.LogError("Multiplayer_InGameUI: Player2Results_Text not found in scene!");
        }

        if (winnerText == null)
        {
            GameObject winnerTextObj = GameObject.Find("FinalResult_Text");
            if (winnerTextObj != null)
                winnerText = winnerTextObj.GetComponent<TextMeshProUGUI>();
            else
                Debug.LogError("Multiplayer_InGameUI: FinalResult_Text not found in scene!");
        }

        if (returnToMenuButton == null)
        {
            GameObject returnButtonObj = GameObject.Find("Return_To_Menu_Button");
            if (returnButtonObj != null)
                returnToMenuButton = returnButtonObj.GetComponent<Button>();
            else
                Debug.LogError("Multiplayer_InGameUI: Return_To_Menu_Button not found in scene!");
        }

        Debug.Log("Multiplayer_InGameUI: UI elements auto-assignment completed.");
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

        if (winnerText != null)
        {
            winnerText.gameObject.SetActive(false);
        }

        InitializePlayerResultTexts();

        StartListeningToGameStats();
    }

    private void InitializePlayerResultTexts()
    {
        if (player1ResultsText != null)
        {
            player1ResultsText.text = "Player 1: Waiting...";
        }

        if (player2ResultsText != null)
        {
            player2ResultsText.text = "Player 2: Waiting...";
        }
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
        if (gameResultsPanel != null && !hasCurrentPlayerFinished)
        {
            hasCurrentPlayerFinished = true;
            gameResultsPanel.SetActive(true);
            Debug.Log("Multiplayer_InGameUI: Results panel shown for current player.");
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

        // Update Player 1 results
        UpdatePlayerResultsText(gameStats.player1, player1ResultsText, "Player 1");

        // Update Player 2 results  
        UpdatePlayerResultsText(gameStats.player2, player2ResultsText, "Player 2");

        // Check if current player finished to show results panel
        bool currentPlayerFinished = false;
        if (gameStats.player1.playerId == currentPlayerId && gameStats.player1.hasFinished)
        {
            currentPlayerFinished = true;
        }
        else if (gameStats.player2.playerId == currentPlayerId && gameStats.player2.hasFinished)
        {
            currentPlayerFinished = true;
        }

        if (currentPlayerFinished && !hasCurrentPlayerFinished)
        {
            ShowResultsPanel();
        }

        // Show winner only when both players finished
        UpdateWinnerDisplay(gameStats);
    }

    private void UpdatePlayerResultsText(MultiplayerPlayerStats playerStats, TextMeshProUGUI textComponent, string defaultName)
    {
        if (textComponent == null) return;

        if (string.IsNullOrEmpty(playerStats.playerId))
        {
            textComponent.text = $"{defaultName} : Waiting for player...";
        }
        else if (playerStats.hasFinished)
        {
            string playerResults = $"{playerStats.playerName} : {playerStats.totalScore:F1} points";
            textComponent.text = playerResults;
        }
        else
        {
            textComponent.text = $"{playerStats.playerName} : Waiting...";
        }
    }

    private void UpdateWinnerDisplay(MultiplayerGameStats gameStats)
    {
        if (winnerText == null) return;

        if (gameStats.player1.hasFinished && gameStats.player2.hasFinished)
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
                Debug.Log($"Multiplayer_InGameUI: Winner displayed - {gameStats.winnerName}");
            }
        }
        else
        {
            winnerText.gameObject.SetActive(false);
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