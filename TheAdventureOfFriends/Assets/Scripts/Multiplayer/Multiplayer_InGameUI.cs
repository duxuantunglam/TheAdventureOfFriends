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
    [SerializeField] private Button viewDetailsButton;

    [Header("Details Panel")]
    [SerializeField] private GameObject detailsPanel;
    [SerializeField] private TextMeshProUGUI player1NameText;
    [SerializeField] private TextMeshProUGUI player2NameText;
    [SerializeField] private TextMeshProUGUI player1FruitText;
    [SerializeField] private TextMeshProUGUI player2FruitText;
    [SerializeField] private TextMeshProUGUI player1EnemiesKilledText;
    [SerializeField] private TextMeshProUGUI player2EnemiesKilledText;
    [SerializeField] private TextMeshProUGUI player1KnockBacksText;
    [SerializeField] private TextMeshProUGUI player2KnockBacksText;
    [SerializeField] private TextMeshProUGUI player1TimeText;
    [SerializeField] private TextMeshProUGUI player2TimeText;
    [SerializeField] private Button goodButton;
    [SerializeField] private Button mediumButton;
    [SerializeField] private Button badButton;

    // Firebase tracking
    private DatabaseReference roomsRef;
    private string currentRoomId;
    private string currentPlayerId;
    private bool isListeningToGameStats = false;
    private bool hasCurrentPlayerFinished = false;

    // Details panel tracking
    private MultiplayerGameStats currentGameStats;
    private bool bothPlayersFinished = false;

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

        if (viewDetailsButton != null)
        {
            viewDetailsButton.onClick.AddListener(ShowDetailsPanel);
        }

        if (goodButton != null)
        {
            goodButton.onClick.AddListener(() => RateOpponent("Good"));
        }

        if (mediumButton != null)
        {
            mediumButton.onClick.AddListener(() => RateOpponent("Medium"));
        }

        if (badButton != null)
        {
            badButton.onClick.AddListener(() => RateOpponent("Bad"));
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

        if (viewDetailsButton == null)
        {
            GameObject viewDetailsButtonObj = GameObject.Find("ViewDetails_Button");
            if (viewDetailsButtonObj != null)
                viewDetailsButton = viewDetailsButtonObj.GetComponent<Button>();
            else
                Debug.LogError("Multiplayer_InGameUI: ViewDetails_Button not found in scene!");
        }

        if (detailsPanel == null)
        {
            GameObject detailsPanelObj = GameObject.Find("DetailsPanel");
            if (detailsPanelObj != null)
                detailsPanel = detailsPanelObj;
            else
                Debug.LogError("Multiplayer_InGameUI: DetailsPanel not found in scene!");
        }

        if (player1NameText == null)
        {
            GameObject player1NameTextObj = GameObject.Find("Player1Name_Text");
            if (player1NameTextObj != null)
                player1NameText = player1NameTextObj.GetComponent<TextMeshProUGUI>();
            else
                Debug.LogError("Multiplayer_InGameUI: Player1Name_Text not found in scene!");
        }

        if (player2NameText == null)
        {
            GameObject player2NameTextObj = GameObject.Find("Player2Name_Text");
            if (player2NameTextObj != null)
                player2NameText = player2NameTextObj.GetComponent<TextMeshProUGUI>();
            else
                Debug.LogError("Multiplayer_InGameUI: Player2Name_Text not found in scene!");
        }

        if (player1FruitText == null)
        {
            GameObject player1FruitTextObj = GameObject.Find("Player1Fruit_Text");
            if (player1FruitTextObj != null)
                player1FruitText = player1FruitTextObj.GetComponent<TextMeshProUGUI>();
            else
                Debug.LogError("Multiplayer_InGameUI: Player1Fruit_Text not found in scene!");
        }

        if (player2FruitText == null)
        {
            GameObject player2FruitTextObj = GameObject.Find("Player2Fruit_Text");
            if (player2FruitTextObj != null)
                player2FruitText = player2FruitTextObj.GetComponent<TextMeshProUGUI>();
            else
                Debug.LogError("Multiplayer_InGameUI: Player2Fruit_Text not found in scene!");
        }

        if (player1EnemiesKilledText == null)
        {
            GameObject player1EnemiesKilledTextObj = GameObject.Find("Player1EnemiesKilled_Text");
            if (player1EnemiesKilledTextObj != null)
                player1EnemiesKilledText = player1EnemiesKilledTextObj.GetComponent<TextMeshProUGUI>();
            else
                Debug.LogError("Multiplayer_InGameUI: Player1EnemiesKilled_Text not found in scene!");
        }

        if (player2EnemiesKilledText == null)
        {
            GameObject player2EnemiesKilledTextObj = GameObject.Find("Player2EnemiesKilled_Text");
            if (player2EnemiesKilledTextObj != null)
                player2EnemiesKilledText = player2EnemiesKilledTextObj.GetComponent<TextMeshProUGUI>();
            else
                Debug.LogError("Multiplayer_InGameUI: Player2EnemiesKilled_Text not found in scene!");
        }

        if (player1KnockBacksText == null)
        {
            GameObject player1KnockBacksTextObj = GameObject.Find("Player1KnockBacks_Text");
            if (player1KnockBacksTextObj != null)
                player1KnockBacksText = player1KnockBacksTextObj.GetComponent<TextMeshProUGUI>();
            else
                Debug.LogError("Multiplayer_InGameUI: Player1KnockBacks_Text not found in scene!");
        }

        if (player2KnockBacksText == null)
        {
            GameObject player2KnockBacksTextObj = GameObject.Find("Player2KnockBacks_Text");
            if (player2KnockBacksTextObj != null)
                player2KnockBacksText = player2KnockBacksTextObj.GetComponent<TextMeshProUGUI>();
            else
                Debug.LogError("Multiplayer_InGameUI: Player2KnockBacks_Text not found in scene!");
        }

        if (player1TimeText == null)
        {
            GameObject player1TimeTextObj = GameObject.Find("Player1Time_Text");
            if (player1TimeTextObj != null)
                player1TimeText = player1TimeTextObj.GetComponent<TextMeshProUGUI>();
            else
                Debug.LogError("Multiplayer_InGameUI: Player1Time_Text not found in scene!");
        }

        if (player2TimeText == null)
        {
            GameObject player2TimeTextObj = GameObject.Find("Player2Time_Text");
            if (player2TimeTextObj != null)
                player2TimeText = player2TimeTextObj.GetComponent<TextMeshProUGUI>();
            else
                Debug.LogError("Multiplayer_InGameUI: Player2Time_Text not found in scene!");
        }

        if (goodButton == null)
        {
            GameObject goodButtonObj = GameObject.Find("Good_Button");
            if (goodButtonObj != null)
                goodButton = goodButtonObj.GetComponent<Button>();
            else
                Debug.LogError("Multiplayer_InGameUI: Good_Button not found in scene!");
        }

        if (mediumButton == null)
        {
            GameObject mediumButtonObj = GameObject.Find("Medium_Button");
            if (mediumButtonObj != null)
                mediumButton = mediumButtonObj.GetComponent<Button>();
            else
                Debug.LogError("Multiplayer_InGameUI: Medium_Button not found in scene!");
        }

        if (badButton == null)
        {
            GameObject badButtonObj = GameObject.Find("Bad_Button");
            if (badButtonObj != null)
                badButton = badButtonObj.GetComponent<Button>();
            else
                Debug.LogError("Multiplayer_InGameUI: Bad_Button not found in scene!");
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

        if (detailsPanel != null)
        {
            detailsPanel.SetActive(false);
        }

        if (viewDetailsButton != null)
        {
            viewDetailsButton.interactable = false; // Disabled until both players finish
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

        // Store current gameStats for details panel
        currentGameStats = gameStats;

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

        // Check if both players finished
        bothPlayersFinished = gameStats.player1.hasFinished && gameStats.player2.hasFinished;

        // Enable View Details button when both players finished
        if (viewDetailsButton != null)
        {
            viewDetailsButton.interactable = bothPlayersFinished;
            Debug.Log($"🔧 View Details button enabled: {bothPlayersFinished}");
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

    private void ShowDetailsPanel()
    {
        if (currentGameStats == null || !bothPlayersFinished)
        {
            Debug.LogWarning("⚠️ Cannot show details panel: Game not completed or no gameStats available");
            return;
        }

        Debug.Log("🎯 Showing details panel...");

        // Hide results panel
        if (gameResultsPanel != null)
        {
            gameResultsPanel.SetActive(false);
        }

        // Show details panel
        if (detailsPanel != null)
        {
            detailsPanel.SetActive(true);
        }

        // Populate details data
        PopulateDetailsData();
    }

    private void PopulateDetailsData()
    {
        if (currentGameStats == null) return;

        Debug.Log("📊 Populating details data...");

        // Player names
        if (player1NameText != null)
            player1NameText.text = currentGameStats.player1.playerName;

        if (player2NameText != null)
            player2NameText.text = currentGameStats.player2.playerName;

        // Fruit collected
        if (player1FruitText != null)
            player1FruitText.text = currentGameStats.player1.fruitCollected.ToString();

        if (player2FruitText != null)
            player2FruitText.text = currentGameStats.player2.fruitCollected.ToString();

        // Enemies killed
        if (player1EnemiesKilledText != null)
            player1EnemiesKilledText.text = currentGameStats.player1.enemiesKilled.ToString();

        if (player2EnemiesKilledText != null)
            player2EnemiesKilledText.text = currentGameStats.player2.enemiesKilled.ToString();

        // Knockbacks
        if (player1KnockBacksText != null)
            player1KnockBacksText.text = currentGameStats.player1.knockBacks.ToString();

        if (player2KnockBacksText != null)
            player2KnockBacksText.text = currentGameStats.player2.knockBacks.ToString();

        // Time
        if (player1TimeText != null)
            player1TimeText.text = $"{currentGameStats.player1.completionTime:F1}s";

        if (player2TimeText != null)
            player2TimeText.text = $"{currentGameStats.player2.completionTime:F1}s";

        Debug.Log("✅ Details data populated successfully");
        Debug.Log($"Player1: {currentGameStats.player1.playerName} - Fruits: {currentGameStats.player1.fruitCollected}, Time: {currentGameStats.player1.completionTime:F1}s");
        Debug.Log($"Player2: {currentGameStats.player2.playerName} - Fruits: {currentGameStats.player2.fruitCollected}, Time: {currentGameStats.player2.completionTime:F1}s");
    }

    private void RateOpponent(string rating)
    {
        Debug.Log($"🌟 Player {currentPlayerId} rated opponent: {rating}");

        // TODO: Implement in Step 2:
        // 1. Save rating to Match_History
        // 2. Save gameStats to Match_History  
        // 3. Return to MainMenu

        Debug.Log("⚠️ Rating system not implemented yet - will be added in Step 2");

        // For now, just return to menu
        ReturnToMainMenu();
    }
}