using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Database;
using Firebase.Extensions;
using TMPro;
using UnityEngine;
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

    private DatabaseReference dbReference;
    private string currentRoomId;
    private string currentUserId;

    private Dictionary<string, UI_PlayersInRoom> playerUIItems = new Dictionary<string, UI_PlayersInRoom>();

    public event Action OnLeaveRoomCompleted;
    public event Action<string> OnGameStarted;

    private void Awake()
    {
        // Lấy tham chiếu Firebase Database (có thể lấy từ Authentication.instance.dbReference nếu đó là public)
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

    public async Task ShowRoom(string userId, string roomId = null, string invitedUserId = null)
    {
        currentUserId = userId;
        if (string.IsNullOrEmpty(currentUserId))
        {
            Debug.LogError("UI_WaitingRoom: Current user ID is not set.");
            return;
        }

        if (waitingRoomUIPanel != null)
        {
            waitingRoomUIPanel.SetActive(true);
        }

        if (string.IsNullOrEmpty(roomId))
        {
            await CreateRoom(currentUserId, invitedUserId);
        }
        else
        {
            currentRoomId = roomId;
            // Cần thêm logic tham gia phòng trên Firebase ở đây
            await JoinRoom(currentUserId, currentRoomId); // Bây giờ JoinRoom cần ghi dữ liệu người chơi vào phòng
            // Sau khi join thành công, bắt đầu lắng nghe
            // Lắng nghe sẽ được gọi tự động trong JoinRoom nếu thành công
            // ListenToRoomChanges(currentRoomId); // Không cần gọi ở đây nữa
        }

        // TODO: 'Loading...' UI
    }

    public void HideRoom()
    {
        if (waitingRoomUIPanel != null)
        {
            waitingRoomUIPanel.SetActive(false);
        }

        StopListeningToRoomChanges();
        ClearPlayerListUI();
        currentRoomId = null;
        currentUserId = null;
        if (startGameButton != null)
        {
            startGameButton.gameObject.SetActive(false);
        }
    }

    private async Task CreateRoom(string creatingUserId, string invitedUserId = null)
    {
        if (dbReference == null) return;

        DatabaseReference newRoomRef = dbReference.Child("Rooms").Push();
        currentRoomId = newRoomRef.Key;

        string creatingUserName = "Unknown";
        if (Authentication.CurrentUser != null) creatingUserName = Authentication.CurrentUser.userName;

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

        // Nếu có người chơi được mời và không phải là chính mình, gửi lời mời
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

        // Tạo một key duy nhất cho lời mời
        DatabaseReference invitationsRef = dbReference.Child("Invitations").Child(invitedId).Push(); // Lưu lời mời dưới node của người được mời
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
                     // Xử lý lỗi gửi lời mời
                 }
                 else if (task.IsCompleted)
                 {
                     Debug.Log($"Invitation {invitationId} sent successfully to user {invitedId}.");
                     // Thông báo cho người mời rằng lời mời đã được gửi
                 }
             });
    }

    private async Task JoinRoom(string userId, string roomId)
    {
        if (dbReference == null || string.IsNullOrEmpty(roomId) || string.IsNullOrEmpty(userId)) return;

        string userName = "Unknown";
        if (Authentication.CurrentUser != null) userName = Authentication.CurrentUser.userName;

        DatabaseReference roomPlayersRef = dbReference.Child("Rooms").Child(roomId).Child("players");

        DataSnapshot roomSnapshot = await dbReference.Child("Rooms").Child(roomId).GetValueAsync();

        if (!roomSnapshot.Exists)
        {
            Debug.LogError($"Attempted to join room {roomId} but it does not exist.");
            HideRoom();
            OnLeaveRoomCompleted?.Invoke(); // thông báo lỗi hoặc phòng không tồn tại
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
                    // thông báo cho người dùng không vào được phòng
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

    // Rời phòng và xóa dữ liệu trên Firebase
    public async void LeaveRoom() // Public để gọi từ nút
    {
        if (dbReference == null || string.IsNullOrEmpty(currentRoomId) || string.IsNullOrEmpty(currentUserId)) return;

        Debug.Log($"User {currentUserId} attempting to leave room {currentRoomId}");

        // Dừng lắng nghe ngay lập tức để tránh xử lý sự kiện sau khi rời phòng
        StopListeningToRoomChanges();

        // Đường dẫn đến người chơi hiện tại trong danh sách người chơi của phòng
        DatabaseReference currentPlayerRef = dbReference.Child("Rooms").Child(currentRoomId).Child("players").Child(currentUserId);

        // Xóa người chơi hiện tại khỏi danh sách
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

                   // thông báo hoàn thành rời phòng
                   OnLeaveRoomCompleted?.Invoke();
                   // HideRoom();
               }
           });
        // HideRoom();
    }

    // Bắt đầu Game (Bất kỳ người chơi nào cũng có thể bấm nút Start nếu điều kiện thỏa mãn)
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
                    await dbReference.Child("Rooms").Child(currentRoomId).Child("status").SetValueAsync("in_game")
                         .ContinueWithOnMainThread(task =>
                         {
                             if (task.IsFaulted)
                             {
                                 Debug.LogError($"Failed to set room status to in_game for room {currentRoomId}: {task.Exception}");
                             }
                             else if (task.IsCompleted)
                             {
                                 Debug.Log($"Room {currentRoomId} status set to in_game. Game should start now.");
                                 OnGameStarted?.Invoke(currentRoomId);
                                 // HideRoom();
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

        UpdatePlayerListUI(playersInRoom);

        CheckAndEnableStartButton(playersInRoom);

        string roomStatus = snapshot.Child("status").GetValue(true)?.ToString();
        if (roomStatus == "in_game")
        {
            Debug.Log($"Room {currentRoomId} status is in_game. Triggering game start.");
            OnGameStarted?.Invoke(currentRoomId);
            HideRoom();
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