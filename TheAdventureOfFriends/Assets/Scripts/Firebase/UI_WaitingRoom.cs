using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Database;
using Firebase.Extensions;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Action = System.Action;

[Serializable]
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

    private DatabaseReference dbReference;
    private string currentRoomId;
    private string currentUserId;

    private Dictionary<string, UI_PlayersInRoom> playerUIItems = new Dictionary<string, UI_PlayersInRoom>();

    private bool isWaitingRoomUIActive = false;

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
    }

    public void HideRoom()
    {
        if (waitingRoomUIPanel != null)
        {
            waitingRoomUIPanel.SetActive(false);
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
            Debug.LogError($"Attempted to join room {roomId} but it does not exist.");
            HideRoom();
            OnLeaveRoomCompleted?.Invoke();
            return;
        }

        DataSnapshot playerSnapshot = await roomPlayersRef.Child(userId).GetValueAsync();
        if (playerSnapshot.Exists)
        {
            Debug.LogWarning($"User {userId} is already in room {roomId}. Assuming reconnection or duplicate join attempt. Proceeding to listen.");
            currentRoomId = roomId;
            ListenToRoomChanges(roomId);
            return;
        }

        long currentPlayerCount = roomSnapshot.Child("players").ChildrenCount;
        if (currentPlayerCount >= 2)
        {
            Debug.LogWarning($"Room {roomId} is already full ({currentPlayerCount} players). User {userId} cannot join.");
            HideRoom();
            return;
        }

        RoomPlayerData newPlayerData = new RoomPlayerData { userName = userName, isReady = false };

        await roomPlayersRef.Child(userId).SetRawJsonValueAsync(JsonUtility.ToJson(newPlayerData))
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError($"Failed to join room {roomId} for user {userId}: {task.Exception}");

                    HideRoom();
                }
                else if (task.IsCompleted)
                {
                    Debug.Log($"User {userId} joined room {roomId}");
                    currentRoomId = roomId;

                    ListenToRoomChanges(roomId);
                }
            });
    }

    public async void LeaveRoom()
    {
        if (dbReference == null || string.IsNullOrEmpty(currentRoomId) || string.IsNullOrEmpty(currentUserId)) return;

        Debug.Log($"User {currentUserId} attempting to leave room {currentRoomId}");

        StopListeningToRoomChanges();

        DatabaseReference currentPlayerRef = dbReference.Child("Rooms").Child(currentRoomId).Child("players").Child(currentUserId);

        await currentPlayerRef.RemoveValueAsync()
           .ContinueWithOnMainThread(async task =>
           {
               if (task.IsFaulted)
               {
                   Debug.LogError($"Failed to remove user {currentUserId} from room {currentRoomId}: {task.Exception}");
               }
               else if (task.IsCompleted)
               {
                   Debug.Log($"User {currentUserId} left room {currentRoomId}");

                   DataSnapshot roomSnapshotAfterLeave = await dbReference.Child("Rooms").Child(currentRoomId).GetValueAsync();

                   if (roomSnapshotAfterLeave.Exists && roomSnapshotAfterLeave.Child("players").ChildrenCount == 0)
                   {
                       Debug.Log($"Room {currentRoomId} is empty, deleting room.");
                       await dbReference.Child("Rooms").Child(currentRoomId).RemoveValueAsync()
                           .ContinueWithOnMainThread(deleteRoomTask =>
                           {
                               if (deleteRoomTask.IsFaulted)
                               {
                                   Debug.LogError($"Failed to delete empty room {currentRoomId}: {deleteRoomTask.Exception}");
                               }
                               else if (deleteRoomTask.IsCompleted)
                               {
                                   Debug.Log($"Empty room {currentRoomId} deleted.");
                               }
                           });
                   }
                   else if (!roomSnapshotAfterLeave.Exists)
                   {
                       Debug.LogWarning($"Room {currentRoomId} was already deleted by another player.");
                   }
                   else
                   {
                       Debug.Log($"Room {currentRoomId} still has players ({roomSnapshotAfterLeave.Child("players").ChildrenCount}), not deleting.");
                   }

                   OnLeaveRoomCompleted?.Invoke();
               }
           });
    }

    private void JoinRoomHandling()
    {
        Debug.Log($"JoinRoomHandling called. isWaitingRoomUIActive: {isWaitingRoomUIActive}");

        if (waitingRoomUIPanel != null && waitingRoomUIPanel.transform.parent != null)
        {
            bool isCurrentlyActive = waitingRoomUIPanel.activeSelf;
            Debug.Log($"WaitingRoom UI current active state: {isCurrentlyActive}");

            if (!isCurrentlyActive || !isWaitingRoomUIActive)
            {
                Transform parentCanvas = waitingRoomUIPanel.transform.parent;
                for (int i = 0; i < parentCanvas.childCount; i++)
                {
                    GameObject child = parentCanvas.GetChild(i).gameObject;
                    if (child != waitingRoomUIPanel)
                    {
                        child.SetActive(false);
                    }
                }

                waitingRoomUIPanel.SetActive(true);
                isWaitingRoomUIActive = true;
                Debug.Log("Waiting room UI activated");
            }
            else
            {
                Debug.Log("Waiting room UI is already active and flag is set, ensuring it's visible");

                waitingRoomUIPanel.SetActive(true);
            }
        }
        else
        {
            Debug.LogError("WaitingRoomUIPanel or its parent is null!");
        }
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
                    string playerId = playerChild.Key;
                    string playerName = playerChild.Child("userName").GetValue(true)?.ToString() ?? "Unknown";
                    bool isReady = playerChild.Child("isReady").GetValue(true) as bool? ?? false;

                    playersInRoom.Add(new PlayerInRoomInfo { userId = playerId, userName = playerName, isReady = isReady });

                    if (!isReady)
                    {
                        allReady = false;
                    }
                }

                if (allReady)
                {
                    Debug.Log($"Starting game in room {currentRoomId}. Both players are Ready.");

                    await CreateInitialGameStats(playersInRoom);

                    var updates = new Dictionary<string, object>
                    {
                        { "status", "in_game" },
                        { "currentScene", "Multiplayer" }
                    };

                    await dbReference.Child("Rooms").Child(currentRoomId).UpdateChildrenAsync(updates)
                         .ContinueWithOnMainThread(task =>
                         {
                             if (task.IsFaulted)
                             {
                                 Debug.LogError($"Failed to start game for room {currentRoomId}: {task.Exception}");
                             }
                             else if (task.IsCompleted)
                             {
                                 Debug.Log($"Room {currentRoomId} game started successfully. Loading Multiplayer scene.");

                                 // Store room ID for Multiplayer scene to access
                                 PlayerPrefs.SetString("CurrentMultiplayerRoomId", currentRoomId);
                                 PlayerPrefs.Save();

                                 // Load Multiplayer scene
                                 SceneManager.LoadScene("Multiplayer");
                             }
                         });
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

    /// <summary>
    /// Tạo gameStats ban đầu với thông tin đầy đủ của cả 2 players
    /// </summary>
    private async Task CreateInitialGameStats(List<PlayerInRoomInfo> players)
    {
        if (players.Count != 2)
        {
            Debug.LogError($"CreateInitialGameStats: Expected 2 players, got {players.Count}");
            return;
        }

        // Tạo gameStats object với thông tin đầy đủ
        var gameStats = new
        {
            player1 = new
            {
                playerId = players[0].userId,
                playerName = players[0].userName,
                fruitCollected = 0,
                completionTime = 0f,
                enemiesKilled = 0,
                knockBacks = 0,
                totalScore = 0f,
                hasFinished = false
            },
            player2 = new
            {
                playerId = players[1].userId,
                playerName = players[1].userName,
                fruitCollected = 0,
                completionTime = 0f,
                enemiesKilled = 0,
                knockBacks = 0,
                totalScore = 0f,
                hasFinished = false
            },
            gameStatus = "playing",
            winnerId = "",
            winnerName = ""
        };

        try
        {
            string gameStatsJson = JsonConvert.SerializeObject(gameStats);
            await dbReference.Child("Rooms").Child(currentRoomId).Child("gameStats").SetRawJsonValueAsync(gameStatsJson);
            Debug.Log($"✅ GameStats created successfully for room {currentRoomId}");
            Debug.Log($"Player1: {players[0].userName} ({players[0].userId})");
            Debug.Log($"Player2: {players[1].userName} ({players[1].userId})");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Failed to create initial gameStats: {e.Message}");
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
        string currentScene = snapshot.Child("currentScene").GetValue(true)?.ToString();

        if (roomStatus == "in_game" && currentScene == "Multiplayer")
        {
            Debug.Log($"Room {currentRoomId} status is in_game with currentScene = Multiplayer. Loading Multiplayer scene.");

            PlayerPrefs.SetString("CurrentMultiplayerRoomId", currentRoomId);
            PlayerPrefs.Save();

            SceneManager.LoadScene("Multiplayer");
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