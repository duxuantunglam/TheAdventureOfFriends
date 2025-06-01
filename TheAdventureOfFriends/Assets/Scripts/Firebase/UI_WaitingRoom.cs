using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Database;
using Firebase.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Action = System.Action;

[System.Serializable]
public class PlayerInRoomInfo
{
    public string userId;
    public string userName;
    public bool isReady;
}

public class UI_WaitingRoom : MonoBehaviour
{
    [SerializeField] private GameObject waitingRoomUIPanel;
    [SerializeField] private UI_PlayersInRoom playerItemPrefab;
    [SerializeField] private Transform contentParent;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button leaveRoomButton;

    [Header("Game Results UI")]
    [SerializeField] private GameObject gameResultsPanel;
    [SerializeField] private TextMeshProUGUI gameResultsText;
    [SerializeField] private Button viewDetailsButton;
    [SerializeField] private Button closeResultsButton;

    private DatabaseReference dbReference;
    private string currentRoomId;
    private string currentUserId;

    private Dictionary<string, UI_PlayersInRoom> playerUIItems = new Dictionary<string, UI_PlayersInRoom>();

    private bool isWaitingRoomUIActive = false;
    private bool isListeningToGameStats = false;

    public event Action OnLeaveRoomCompleted;
    public event Action<string> OnGameStarted;

    private void Awake()
    {
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;

        if (leaveRoomButton != null)
        {
            leaveRoomButton.onClick.AddListener(LeaveRoom);
        }

        if (startGameButton != null)
        {
            startGameButton.interactable = false;
            startGameButton.gameObject.SetActive(false);
            startGameButton.onClick.AddListener(StartGame);
        }

        if (waitingRoomUIPanel != null)
        {
            waitingRoomUIPanel.SetActive(false);
        }

        // Initialize game results UI
        if (gameResultsPanel != null)
        {
            gameResultsPanel.SetActive(false);
        }

        if (viewDetailsButton != null)
        {
            viewDetailsButton.onClick.AddListener(ShowGameDetails);
        }

        if (closeResultsButton != null)
        {
            closeResultsButton.onClick.AddListener(CloseGameResults);
        }
    }

    private void Start()
    {
        // Check if we're returning from a multiplayer game
        CheckForGameReturn();
    }

    private void CheckForGameReturn()
    {
        string returnRoomId = PlayerPrefs.GetString("ReturnFromMultiplayerRoom", "");
        if (!string.IsNullOrEmpty(returnRoomId))
        {
            Debug.Log($"UI_WaitingRoom: Returning from multiplayer game in room {returnRoomId}");

            // Clear the return flag
            PlayerPrefs.DeleteKey("ReturnFromMultiplayerRoom");

            // Rejoin the room and show results
            StartCoroutine(HandleGameReturn(returnRoomId));
        }
    }

    private System.Collections.IEnumerator HandleGameReturn(string roomId)
    {
        yield return new WaitForSeconds(0.5f); // Small delay to ensure Firebase is ready

        // Set current room info
        currentRoomId = roomId;
        currentUserId = FirebaseManager.CurrentUser?.id;

        if (string.IsNullOrEmpty(currentUserId))
        {
            Debug.LogError("UI_WaitingRoom: No current user found when returning from game.");
            yield break;
        }

        // Show waiting room UI
        JoinRoomHandling();

        // Listen to room changes
        ListenToRoomChanges(currentRoomId);

        // Check for game results
        CheckAndShowGameResults();
    }

    private void CheckAndShowGameResults()
    {
        if (string.IsNullOrEmpty(currentRoomId)) return;

        dbReference.Child("Rooms").Child(currentRoomId).Child("gameStats").GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("UI_WaitingRoom: Failed to load game stats: " + task.Exception);
                    return;
                }

                if (task.IsCompleted && task.Result.Exists)
                {
                    try
                    {
                        string json = task.Result.GetRawJsonValue();
                        var gameStats = Newtonsoft.Json.JsonConvert.DeserializeObject<MultiplayerGameStats>(json);

                        if (gameStats != null && gameStats.gameStatus == "finished")
                        {
                            ShowGameResults(gameStats);
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError("UI_WaitingRoom: Error parsing game stats: " + e.Message);
                    }
                }
            });
    }

    private void ShowGameResults(MultiplayerGameStats gameStats)
    {
        if (gameResultsPanel == null || gameResultsText == null) return;

        string resultsText = "Game Results:\n\n";

        // Player 1 results
        resultsText += $"Player 1: {gameStats.player1.playerName}\n";
        resultsText += $"Score: {gameStats.player1.totalScore:F1}\n";
        resultsText += $"Fruits: {gameStats.player1.fruitCollected}, Time: {gameStats.player1.completionTime:F1}s\n";
        resultsText += $"Enemies: {gameStats.player1.enemiesKilled}, Knockbacks: {gameStats.player1.knockBacks}\n\n";

        // Player 2 results
        resultsText += $"Player 2: {gameStats.player2.playerName}\n";
        resultsText += $"Score: {gameStats.player2.totalScore:F1}\n";
        resultsText += $"Fruits: {gameStats.player2.fruitCollected}, Time: {gameStats.player2.completionTime:F1}s\n";
        resultsText += $"Enemies: {gameStats.player2.enemiesKilled}, Knockbacks: {gameStats.player2.knockBacks}\n\n";

        // Winner
        if (gameStats.winnerName == "Tie")
        {
            resultsText += "Result: It's a Tie!";
        }
        else
        {
            resultsText += $"Winner: {gameStats.winnerName}!";
        }

        gameResultsText.text = resultsText;
        gameResultsPanel.SetActive(true);

        Debug.Log("UI_WaitingRoom: Game results displayed.");
    }

    private void ShowGameDetails()
    {
        // You can implement a more detailed view here
        Debug.Log("UI_WaitingRoom: Show game details button clicked.");
    }

    private void CloseGameResults()
    {
        if (gameResultsPanel != null)
        {
            gameResultsPanel.SetActive(false);
        }

        // Clear game stats after viewing results
        ClearGameStats();
    }

    private void ClearGameStats()
    {
        if (string.IsNullOrEmpty(currentRoomId)) return;

        dbReference.Child("Rooms").Child(currentRoomId).Child("gameStats").RemoveValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("UI_WaitingRoom: Failed to clear game stats: " + task.Exception);
                }
                else if (task.IsCompleted)
                {
                    Debug.Log("UI_WaitingRoom: Game stats cleared successfully.");
                }
            });
    }

    public void HideRoom()
    {
        if (waitingRoomUIPanel != null)
        {
            waitingRoomUIPanel.SetActive(false);
        }

        if (gameResultsPanel != null)
        {
            gameResultsPanel.SetActive(false);
        }

        isWaitingRoomUIActive = false;

        StopListeningToRoomChanges();
        ClearPlayerListUI();
        currentRoomId = null;
        currentUserId = null;
        if (startGameButton != null)
        {
            startGameButton.gameObject.SetActive(false);
        }
    }

    public async Task ShowRoom(string userId, string roomId = null, string invitedUserId = null)
    {
        currentUserId = userId;
        if (string.IsNullOrEmpty(currentUserId))
        {
            Debug.LogError("UI_WaitingRoom: Current user ID is not set.");
            return;
        }

        Debug.Log($"ShowRoom called. UserId: {userId}, RoomId: {roomId}, InvitedUserId: {invitedUserId}");

        JoinRoomHandling();

        if (string.IsNullOrEmpty(roomId))
        {
            await CreateRoom(currentUserId, invitedUserId);

            if (!string.IsNullOrEmpty(currentRoomId))
            {
                await JoinRoom(currentUserId, currentRoomId);
            }
            else
            {
                Debug.LogError("Failed to get RoomId after creating room.");
                HideRoom();
                OnLeaveRoomCompleted?.Invoke();
            }
        }
        else
        {
            currentRoomId = roomId;
            Debug.Log($"Joining existing room: {roomId}");
            await JoinRoom(currentUserId, currentRoomId);
        }

        // TODO: 'Loading...' UI
    }

    private async Task CreateRoom(string creatingUserId, string invitedUserId = null)
    {
        if (dbReference == null) return;

        DatabaseReference newRoomRef = dbReference.Child("Rooms").Push();
        currentRoomId = newRoomRef.Key;

        string creatingUserName = "Unknown";
        if (FirebaseManager.CurrentUser != null) creatingUserName = FirebaseManager.CurrentUser.userName;

        Debug.Log($"Attempting to create room {currentRoomId} by {creatingUserId}");

        RoomData roomData = new RoomData
        {
            players = new Dictionary<string, RoomPlayerData>
            {
                { creatingUserId, new RoomPlayerData { userName = creatingUserName, isReady = false } }
            },
            status = "waiting"
        };

        await newRoomRef.SetRawJsonValueAsync(JsonUtility.ToJson(roomData));

        Debug.Log($"Room {currentRoomId} created successfully by {creatingUserId}");

        ListenToRoomChanges(currentRoomId);

        if (!string.IsNullOrEmpty(invitedUserId) && invitedUserId != creatingUserId)
        {
            Debug.Log($"Calling SendInvitationAsync for room {currentRoomId}");
            await SendInvitationAsync(currentRoomId, creatingUserId, invitedUserId, creatingUserName);
        }
        else if (invitedUserId == creatingUserId)
        {
            Debug.LogWarning("Attempted to invite self, skipping SendInvitationAsync.");
        }
    }

    private async Task SendInvitationAsync(string roomId, string inviterId, string invitedId, string inviterName)
    {
        if (dbReference == null || string.IsNullOrEmpty(roomId) || string.IsNullOrEmpty(inviterId) || string.IsNullOrEmpty(invitedId)) return;

        Debug.Log($"Sending invitation from {inviterId} to {invitedId} for room {roomId}");

        DatabaseReference invitationsRef = dbReference.Child("Invitations").Child(invitedId).Push();
        string invitationId = invitationsRef.Key;

        InvitationData invitationData = new InvitationData
        {
            roomId = roomId,
            inviterId = inviterId,
            invitedId = invitedId,
            inviterName = inviterName,
            timestamp = ServerValue.Timestamp
        };

        await invitationsRef.SetRawJsonValueAsync(JsonUtility.ToJson(invitationData))
             .ContinueWithOnMainThread(task =>
             {
                 if (task.IsFaulted)
                 {
                     Debug.LogError($"Failed to send invitation {invitationId} to user {invitedId}: {task.Exception}");
                 }
                 else if (task.IsCompleted)
                 {
                     Debug.Log($"Invitation {invitationId} sent successfully to user {invitedId}.");
                 }
             });
    }

    private async Task JoinRoom(string userId, string roomId)
    {
        if (dbReference == null || string.IsNullOrEmpty(roomId) || string.IsNullOrEmpty(userId)) return;

        string userName = "Unknown";
        if (FirebaseManager.CurrentUser != null) userName = FirebaseManager.CurrentUser.userName;

        DatabaseReference roomPlayersRef = dbReference.Child("Rooms").Child(roomId).Child("players");

        DataSnapshot roomSnapshot = await dbReference.Child("Rooms").Child(roomId).GetValueAsync();

        if (!roomSnapshot.Exists)
        {
            Debug.LogError($"Room {roomId} does not exist.");
            HideRoom();
            OnLeaveRoomCompleted?.Invoke();
            return;
        }

        RoomPlayerData playerData = new RoomPlayerData { userName = userName, isReady = false };
        await roomPlayersRef.Child(userId).SetRawJsonValueAsync(JsonUtility.ToJson(playerData));

        Debug.Log($"Player {userId} joined room {roomId}");

        ListenToRoomChanges(roomId);
    }

    public async void LeaveRoom()
    {
        if (dbReference == null || string.IsNullOrEmpty(currentRoomId) || string.IsNullOrEmpty(currentUserId)) return;

        Debug.Log($"Player {currentUserId} is leaving room {currentRoomId}");

        try
        {
            await dbReference.Child("Rooms").Child(currentRoomId).Child("players").Child(currentUserId).RemoveValueAsync();

            DataSnapshot roomSnapshot = await dbReference.Child("Rooms").Child(currentRoomId).GetValueAsync();
            if (roomSnapshot.Exists)
            {
                DataSnapshot playersSnapshot = roomSnapshot.Child("players");
                if (!playersSnapshot.Exists || playersSnapshot.ChildrenCount == 0)
                {
                    Debug.Log($"Room {currentRoomId} is now empty. Deleting room.");
                    await dbReference.Child("Rooms").Child(currentRoomId).RemoveValueAsync();
                }
            }

            Debug.Log($"Player {currentUserId} successfully left room {currentRoomId}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error leaving room {currentRoomId}: {ex.Message}");
        }

        StopListeningToRoomChanges();
        ClearPlayerListUI();

        OnLeaveRoomCompleted?.Invoke();
        HideRoom();
    }

    private void JoinRoomHandling()
    {
        isWaitingRoomUIActive = true;

        if (waitingRoomUIPanel != null)
        {
            waitingRoomUIPanel.SetActive(true);
        }

        ClearPlayerListUI();

        if (startGameButton != null)
        {
            startGameButton.interactable = false;
            startGameButton.gameObject.SetActive(false);
        }

        Debug.Log("UI_WaitingRoom: Waiting room UI activated.");
    }

    public async void StartGame()
    {
        if (dbReference == null || string.IsNullOrEmpty(currentRoomId) || string.IsNullOrEmpty(currentUserId)) return;

        Debug.Log($"User {currentUserId} attempting to start game in room {currentRoomId}");

        DataSnapshot roomSnapshot = await dbReference.Child("Rooms").Child(currentRoomId).GetValueAsync();
        if (roomSnapshot.Exists)
        {
            List<PlayerInRoomInfo> playersInRoom = new List<PlayerInRoomInfo>();
            DataSnapshot playersSnapshot = roomSnapshot.Child("players");

            if (playersSnapshot.Exists && playersSnapshot.ChildrenCount == 2)
            {
                bool allReady = true;
                foreach (var playerChild in playersSnapshot.Children)
                {
                    bool isReady = playerChild.Child("isReady").GetValue(true) as bool? ?? false;
                    if (!isReady)
                    {
                        allReady = false;
                        break;
                    }
                }

                if (allReady)
                {
                    Debug.Log($"Starting game in room {currentRoomId}. Both players are Ready.");

                    // Set room status to in_game
                    await dbReference.Child("Rooms").Child(currentRoomId).Child("status").SetValueAsync("in_game");

                    // Store room ID for the multiplayer scene
                    PlayerPrefs.SetString("CurrentRoomId", currentRoomId);
                    PlayerPrefs.Save();

                    Debug.Log($"Room {currentRoomId} status set to in_game. Loading Multiplayer scene.");

                    // Load the Multiplayer scene
                    SceneManager.LoadScene("Multiplayer");
                }
                else
                {
                    Debug.LogWarning("Cannot start game: Not all players are ready.");
                }
            }
            else
            {
                Debug.LogWarning($"Cannot start game: Room {currentRoomId} does not have exactly 2 players.");
            }
        }
        else
        {
            Debug.LogWarning($"Attempted to start game in room {currentRoomId} but it no longer exists.");
            HideRoom();
            OnLeaveRoomCompleted?.Invoke();
        }
    }

    private void ListenToRoomChanges(string roomId)
    {
        if (dbReference == null || string.IsNullOrEmpty(roomId)) return;

        Debug.Log($"Start listening to changes for room {roomId}");

        StopListeningToRoomChanges();
        dbReference.Child("Rooms").Child(roomId).ValueChanged += HandleRoomValueChanged;
        currentRoomId = roomId;
    }

    private void StopListeningToRoomChanges()
    {
        if (dbReference != null && !string.IsNullOrEmpty(currentRoomId))
        {
            Debug.Log($"Stop listening to changes for room {currentRoomId}");
            dbReference.Child("Rooms").Child(currentRoomId).ValueChanged -= HandleRoomValueChanged;
        }
    }

    private void HandleRoomValueChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        DataSnapshot snapshot = args.Snapshot;

        Debug.Log($"HandleRoomValueChanged triggered for room {currentRoomId}. Snapshot Exists: {snapshot.Exists}. Snapshot Value: {snapshot.GetRawJsonValue()}");

        if (!snapshot.Exists)
        {
            Debug.Log($"Room {currentRoomId} no longer exists.");
            StopListeningToRoomChanges();
            ClearPlayerListUI();

            OnLeaveRoomCompleted?.Invoke();
            HideRoom();
            return;
        }

        List<PlayerInRoomInfo> playersInRoom = new List<PlayerInRoomInfo>();
        DataSnapshot playersSnapshot = snapshot.Child("players");

        if (playersSnapshot.Exists && playersSnapshot.ChildrenCount > 0)
        {
            foreach (var playerChild in playersSnapshot.Children)
            {
                string playerId = playerChild.Key;
                string playerName = playerChild.Child("userName").GetValue(true)?.ToString() ?? "Unknown";
                bool isReady = playerChild.Child("isReady").GetValue(true) as bool? ?? false;

                playersInRoom.Add(new PlayerInRoomInfo { userId = playerId, userName = playerName, isReady = isReady });
            }
        }

        Debug.Log($"Players in room: {playersInRoom.Count}");
        foreach (var player in playersInRoom)
        {
            Debug.Log($"  - {player.userName} (ID: {player.userId}, Ready: {player.isReady})");
        }

        if (playersInRoom.Count > 0 && !waitingRoomUIPanel.activeSelf)
        {
            Debug.Log("Room data exists but UI is not active. Re-activating UI.");
            JoinRoomHandling();
        }

        UpdatePlayerListUI(playersInRoom);

        CheckAndEnableStartButton(playersInRoom);

        string roomStatus = snapshot.Child("status").GetValue(true)?.ToString();
        if (roomStatus == "waiting" && isWaitingRoomUIActive)
        {
            // Check if there are game stats (players returning from multiplayer)
            DataSnapshot gameStatsSnapshot = snapshot.Child("gameStats");
            if (gameStatsSnapshot.Exists)
            {
                Debug.Log($"Room {currentRoomId} status returned to waiting with game stats available.");
                CheckAndShowGameResults();
            }
        }
        else if (roomStatus == "in_game")
        {
            Debug.Log($"Room {currentRoomId} status is in_game. Game should be starting/running.");
            // Don't hide room here since we might already be in the multiplayer scene
        }
    }

    private void UpdatePlayerListUI(List<PlayerInRoomInfo> players)
    {
        if (contentParent == null) return;

        List<string> currentUIUserIds = playerUIItems.Keys.ToList();
        foreach (var userId in currentUIUserIds)
        {
            if (!players.Any(p => p.userId == userId))
            {
                if (playerUIItems.TryGetValue(userId, out UI_PlayersInRoom uiItem))
                {
                    Destroy(uiItem.gameObject);
                    playerUIItems.Remove(userId);
                }
            }
        }

        foreach (var player in players)
        {
            if (playerUIItems.TryGetValue(player.userId, out UI_PlayersInRoom uiItem))
            {
                uiItem.SetPlayerData(player.userName, player.userId, player.isReady);

                bool isCurrentUser = player.userId == currentUserId;
                uiItem.SetButtonInteractable(isCurrentUser);
                uiItem.OnReadyStatusChanged -= HandlePlayerReadyStatusChanged;
                uiItem.OnReadyStatusChanged += HandlePlayerReadyStatusChanged;
            }
            else
            {
                GameObject playerItemGO = Instantiate(playerItemPrefab.gameObject, contentParent);
                UI_PlayersInRoom newUIItem = playerItemGO.GetComponent<UI_PlayersInRoom>();
                if (newUIItem != null)
                {
                    newUIItem.SetPlayerData(player.userName, player.userId, player.isReady);

                    bool isCurrentUser = player.userId == currentUserId;
                    newUIItem.SetButtonInteractable(isCurrentUser);
                    newUIItem.OnReadyStatusChanged += HandlePlayerReadyStatusChanged;

                    playerUIItems[player.userId] = newUIItem;
                }
            }
        }

        SortPlayerListUI();
    }

    private void SortPlayerListUI()
    {
        if (contentParent == null || playerUIItems.Count <= 1) return;

        var sortedPlayers = playerUIItems.Values.OrderBy(item => item.GetPlayerName()).ToList();

        for (int i = 0; i < sortedPlayers.Count; i++)
        {
            sortedPlayers[i].transform.SetSiblingIndex(i);
        }
    }

    private void ClearPlayerListUI()
    {
        if (contentParent == null) return;

        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }
        playerUIItems.Clear();
    }

    private void HandlePlayerReadyStatusChanged(string userId, bool isReady)
    {
        Debug.Log($"Player {userId} changed ready status to {isReady}");
        UpdatePlayerReadyStatusOnFirebase(userId, isReady);
    }

    private void UpdatePlayerReadyStatusOnFirebase(string userId, bool isReady)
    {
        if (dbReference == null || string.IsNullOrEmpty(currentRoomId) || string.IsNullOrEmpty(userId)) return;

        dbReference.Child("Rooms").Child(currentRoomId).Child("players").Child(userId).Child("isReady").SetValueAsync(isReady)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError($"Failed to update ready status for user {userId} in room {currentRoomId}: {task.Exception}");
                }
                else if (task.IsCompleted)
                {
                    Debug.Log($"User {userId} ready status updated to {isReady} on Firebase.");
                }
            });
    }

    private void CheckAndEnableStartButton(List<PlayerInRoomInfo> players)
    {
        if (startGameButton == null) return;

        bool hasTwoPlayers = players != null && players.Count == 2;
        bool allReady = hasTwoPlayers && players.All(p => p.isReady);

        startGameButton.gameObject.SetActive(hasTwoPlayers);

        startGameButton.interactable = allReady;
    }
}