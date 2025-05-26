using System;
using System.Collections.Generic;
using System.Linq; // Cần thiết cho LINQ
using System.Threading.Tasks;
using Firebase.Database;
using Firebase.Extensions; // Cần thiết cho ContinueWithOnMainThread
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Action = System.Action;

// Class để lưu trữ thông tin đơn giản về người chơi trong phòng
[System.Serializable]
public class PlayerInRoomInfo
{
    public string userId;
    public string userName;
    public bool isReady;
    // Thêm các thông tin khác nếu cần (ví dụ: là chủ phòng)
}

public class UI_WaitingRoom : MonoBehaviour
{
    [SerializeField] private GameObject waitingRoomUIPanel; // GameObject gốc của giao diện phòng chờ
    [SerializeField] private UI_PlayersInRoom playerItemPrefab; // Prefab hiển thị thông tin 1 người chơi trong phòng
    [SerializeField] private Transform contentParent; // GameObject Content chứa danh sách người chơi
    [SerializeField] private Button startGameButton; // Nút Start Game
    [SerializeField] private Button leaveRoomButton; // Nút Leave Room

    private DatabaseReference dbReference; // Tham chiếu đến Firebase Database
    private string currentRoomId; // ID của phòng hiện tại
    private string currentUserId; // ID của người chơi hiện tại

    // Dictionary để lưu trữ các instance UI_PlayersInRoom đang hiển thị
    private Dictionary<string, UI_PlayersInRoom> playerUIItems = new Dictionary<string, UI_PlayersInRoom>();

    // Event để thông báo khi người chơi rời phòng (logic hủy phòng sẽ xử lý bên trong)
    public event Action OnLeaveRoomCompleted;
    // Event để thông báo khi game bắt đầu (để script khác chuyển scene)
    public event Action<string> OnGameStarted; // Truyền room ID hoặc thông tin cần thiết

    private void Awake()
    {
        // Lấy tham chiếu Firebase Database (có thể lấy từ Authentication.instance.dbReference nếu đó là public)
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;

        // Gán listener cho nút Leave Room
        if (leaveRoomButton != null)
        {
            leaveRoomButton.onClick.AddListener(LeaveRoom);
        }

        // Nút Start Game mặc định vô hiệu hóa
        if (startGameButton != null)
        {
            startGameButton.interactable = false;
            // Gán listener cho nút Start Game
            startGameButton.onClick.AddListener(StartGame);
        }

        // Đảm bảo panel ẩn khi bắt đầu
        if (waitingRoomUIPanel != null)
        {
            waitingRoomUIPanel.SetActive(false);
        }
    }

    // Phương thức hiển thị giao diện phòng chờ và tạo/tham gia phòng
    // Nếu roomId là null hoặc rỗng, sẽ tạo phòng mới. Ngược lại sẽ cố gắng tham gia phòng đó.
    // Khi tạo phòng mới (roomId = null), có thể truyền invitedUserId để gửi lời mời.
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
            // Tạo phòng mới và gửi lời mời (nếu có invitedUserId)
            await CreateRoom(currentUserId, invitedUserId); // Truyền invitedUserId vào CreateRoom
        }
        else
        {
            // Tham gia phòng đã có (ví dụ: từ lời mời)
            currentRoomId = roomId;
            // Cần thêm logic tham gia phòng trên Firebase ở đây
            await JoinRoom(currentUserId, currentRoomId); // Bây giờ JoinRoom cần ghi dữ liệu người chơi vào phòng
            // Sau khi join thành công, bắt đầu lắng nghe
            ListenToRoomChanges(currentRoomId);
        }

        // Cần hiển thị trạng thái loading trong khi xử lý Firebase
    }

    // Phương thức ẩn giao diện phòng chờ
    public void HideRoom()
    {
        if (waitingRoomUIPanel != null)
        {
            waitingRoomUIPanel.SetActive(false);
        }
        // Dọn dẹp listeners Firebase khi ẩn phòng
        StopListeningToRoomChanges();
        ClearPlayerListUI();
        currentRoomId = null;
        currentUserId = null;
    }

    // --- Logic Firebase ---

    // Tạo phòng mới trên Firebase
    // Thêm tham số invitedUserId để gửi lời mời sau khi tạo phòng
    private async Task CreateRoom(string hostUserId, string invitedUserId = null)
    {
        if (dbReference == null) return;

        // Tạo một key duy nhất cho phòng mới
        DatabaseReference newRoomRef = dbReference.Child("Rooms").Push();
        currentRoomId = newRoomRef.Key;

        // Lấy tên người chơi hiện tại (chủ phòng)
        string hostUserName = "Unknown"; // Cần lấy tên thật từ Authentication.CurrentUser hoặc data khác
        if (Authentication.CurrentUser != null) hostUserName = Authentication.CurrentUser.userName;


        // Tạo dữ liệu phòng ban đầu
        // Dữ liệu phòng cần chứa danh sách người chơi và trạng thái của họ
        var roomData = new
        {
            hostId = hostUserId,
            players = new Dictionary<string, object>()
        };

        // Thêm chủ phòng vào danh sách người chơi trong phòng
        roomData.players[hostUserId] = new
        {
            userName = hostUserName,
            isReady = false // Chủ phòng mặc định Unready khi tạo phòng
            // Thêm các thông tin khác về người chơi trong phòng nếu cần
        };

        Debug.Log($"Attempting to create room {currentRoomId} for {hostUserId}");

        // Ghi dữ liệu phòng lên Firebase và chờ hoàn thành
        await newRoomRef.SetRawJsonValueAsync(JsonUtility.ToJson(roomData)); // <-- await trực tiếp ở đây, không dùng ContinueWith cho logic chính

        // Sau khi tạo phòng thành công, xử lý các bước tiếp theo
        Debug.Log($"Room {currentRoomId} created successfully by {hostUserId}");

        // Bắt đầu lắng nghe sự thay đổi của phòng
        ListenToRoomChanges(currentRoomId);

        // Nếu có người chơi được mời và không phải là chính mình, gửi lời mời
        if (!string.IsNullOrEmpty(invitedUserId) && invitedUserId != hostUserId)
        {
            Debug.Log($"Calling SendInvitationAsync for room {currentRoomId}");
            await SendInvitationAsync(currentRoomId, hostUserId, invitedUserId, hostUserName); // <-- await gọi SendInvitationAsync
        }
        else if (invitedUserId == hostUserId)
        {
            Debug.LogWarning("Attempted to invite self, skipping SendInvitationAsync.");
        }


        // Cập nhật UI với thông tin chủ phòng ban đầu (có thể gọi lại UpdatePlayerListUI sau khi JoinRoom)
        // Logic hiển thị player UI sẽ được HandleRoomValueChanged xử lý khi có snapshot đầu tiên

        // Check nếu JoinRoom không được gọi tự động khi tạo phòng, thì thêm chủ phòng vào danh sách players UI ban đầu
        // UpdatePlayerListUI(new List<PlayerInRoomInfo>() { new PlayerInRoomInfo { userId = hostUserId, userName = hostUserName, isReady = false } });

        // Nếu không có người được mời, có thể hiển thị trạng thái chờ người khác tham gia
    }

    // Thêm phương thức gửi lời mời lên Firebase
    private async Task SendInvitationAsync(string roomId, string inviterId, string invitedId, string inviterName)
    {
        if (dbReference == null || string.IsNullOrEmpty(roomId) || string.IsNullOrEmpty(inviterId) || string.IsNullOrEmpty(invitedId)) return;

        Debug.Log($"Sending invitation from {inviterId} to {invitedId} for room {roomId}");

        // Tạo một key duy nhất cho lời mời
        DatabaseReference invitationsRef = dbReference.Child("Invitations").Child(invitedId).Push(); // Lưu lời mời dưới node của người được mời
        string invitationId = invitationsRef.Key;

        var invitationData = new
        {
            roomId = roomId,
            inviterId = inviterId,
            inviterName = inviterName,
            timestamp = ServerValue.Timestamp // Thời gian tạo lời mời
            // Có thể thêm thời gian hết hạn lời mời nếu cần
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

    // Phương thức JoinRoom cần ghi thông tin người chơi tham gia vào phòng
    private async Task JoinRoom(string userId, string roomId)
    {
        if (dbReference == null || string.IsNullOrEmpty(roomId) || string.IsNullOrEmpty(userId)) return;

        // Lấy tên người chơi tham gia
        string userName = "Unknown";
        if (Authentication.CurrentUser != null) userName = Authentication.CurrentUser.userName;

        // Đường dẫn đến danh sách người chơi trong phòng
        DatabaseReference roomPlayersRef = dbReference.Child("Rooms").Child(roomId).Child("players");

        // Kiểm tra xem người chơi đã có trong phòng chưa để tránh ghi đè hoặc lỗi
        DataSnapshot playerSnapshot = await roomPlayersRef.Child(userId).GetValueAsync();
        if (playerSnapshot.Exists)
        {
            Debug.LogWarning($"User {userId} is already in room {roomId}. Skipping join operation.");
            return;
        }


        // Thêm người chơi tham gia vào danh sách
        await roomPlayersRef.Child(userId).SetRawJsonValueAsync(JsonUtility.ToJson(new { userName = userName, isReady = false }))
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError($"Failed to join room {roomId} for user {userId}: {task.Exception}");
                    // Xử lý lỗi: thông báo cho người dùng, không vào được phòng
                    HideRoom(); // Tạm thời ẩn UI nếu join lỗi
                }
                else if (task.IsCompleted)
                {
                    Debug.Log($"User {userId} joined room {roomId}");
                    // Bắt đầu lắng nghe sự thay đổi của phòng
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

        // Kiểm tra xem người chơi hiện tại có phải là chủ phòng không
        DataSnapshot roomSnapshot = await dbReference.Child("Rooms").Child(currentRoomId).GetValueAsync();
        if (roomSnapshot.Exists)
        {
            string hostId = roomSnapshot.Child("hostId").GetValue(true)?.ToString();

            if (hostId == currentUserId)
            {
                // Nếu là chủ phòng, xóa toàn bộ node phòng
                await dbReference.Child("Rooms").Child(currentRoomId).RemoveValueAsync()
                    .ContinueWithOnMainThread(task =>
                    {
                        if (task.IsFaulted)
                        {
                            Debug.LogError($"Failed to delete room {currentRoomId}: {task.Exception}");
                            // Xử lý lỗi: thông báo
                        }
                        else if (task.IsCompleted)
                        {
                            Debug.Log($"Room {currentRoomId} deleted by host {currentUserId}");
                            // Thông báo hoàn thành rời phòng (cho cả người chơi này)
                            OnLeaveRoomCompleted?.Invoke();
                            // HideRoom(); // HideRoom sẽ được gọi bởi OnLeaveRoomCompleted listener
                        }
                    });
            }
            else
            {
                // Nếu không phải chủ phòng, chỉ xóa người chơi khỏi danh sách người chơi trong phòng
                await dbReference.Child("Rooms").Child(currentRoomId).Child("players").Child(currentUserId).RemoveValueAsync()
                   .ContinueWithOnMainThread(task =>
                   {
                       if (task.IsFaulted)
                       {
                           Debug.LogError($"Failed to remove user {currentUserId} from room {currentRoomId}: {task.Exception}");
                           // Xử lý lỗi
                       }
                       else if (task.IsCompleted)
                       {
                           Debug.Log($"User {currentUserId} left room {currentRoomId}");
                           // Thông báo hoàn thành rời phòng
                           OnLeaveRoomCompleted?.Invoke();
                           // HideRoom(); // HideRoom sẽ được gọi bởi OnLeaveRoomCompleted listener
                       }
                   });
            }
        }
        else
        {
            // Trường hợp phòng đã bị xóa trước đó
            Debug.LogWarning($"Attempted to leave room {currentRoomId} but it no longer exists.");
            OnLeaveRoomCompleted?.Invoke(); // Vẫn thông báo hoàn thành
                                            // HideRoom(); // HideRoom sẽ được gọi bởi OnLeaveRoomCompleted listener
        }
        // HideRoom(); // HideRoom sẽ được gọi bởi OnLeaveRoomCompleted listener
    }

    // Bắt đầu Game (Chỉ chủ phòng mới có thể bấm nút Start)
    public async void StartGame() // Public để gọi từ nút
    {
        if (dbReference == null || string.IsNullOrEmpty(currentRoomId) || string.IsNullOrEmpty(currentUserId)) return;

        // Kiểm tra xem người chơi hiện tại có phải là chủ phòng không
        DataSnapshot roomSnapshot = await dbReference.Child("Rooms").Child(currentRoomId).GetValueAsync();
        if (roomSnapshot.Exists)
        {
            string hostId = roomSnapshot.Child("hostId").GetValue(true)?.ToString();

            if (hostId == currentUserId)
            {
                // Kiểm tra xem cả 2 người chơi đã Ready chưa (cần đọc lại trạng thái từ Firebase)
                // Tạm thời bỏ qua check Ready để test
                //bool allReady = CheckAllPlayersReady(roomSnapshot); // Cần implement hàm này

                // Nếu cả 2 Ready (hoặc bỏ qua check để test)
                Debug.Log($"Host {currentUserId} is starting game in room {currentRoomId}");
                // Cập nhật trạng thái phòng trên Firebase để thông báo game bắt đầu
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
                             // Kích hoạt event báo game bắt đầu (để script khác chuyển scene)
                             OnGameStarted?.Invoke(currentRoomId);
                             // HideRoom(); // HideRoom sẽ được gọi bởi OnGameStarted listener
                         }
                     });
            }
            else
            {
                Debug.LogWarning("Only the host can start the game.");
            }
        }
        else
        {
            Debug.LogWarning($"Attempted to start game in room {currentRoomId} but it no longer exists.");
        }
    }


    // --- Lắng nghe sự thay đổi của phòng trên Firebase ---

    private void ListenToRoomChanges(string roomId)
    {
        if (dbReference == null || string.IsNullOrEmpty(roomId)) return;

        Debug.Log($"Start listening to changes for room {roomId}");

        // Lắng nghe sự thay đổi toàn bộ node phòng
        dbReference.Child("Rooms").Child(roomId).ValueChanged += HandleRoomValueChanged;
    }

    private void StopListeningToRoomChanges()
    {
        if (dbReference == null || string.IsNullOrEmpty(currentRoomId)) return;

        Debug.Log($"Stop listening to changes for room {currentRoomId}");

        // Gỡ bỏ listener
        dbReference.Child("Rooms").Child(currentRoomId).ValueChanged -= HandleRoomValueChanged;
    }

    // Xử lý khi dữ liệu phòng thay đổi trên Firebase
    private void HandleRoomValueChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        DataSnapshot snapshot = args.Snapshot;

        // Thêm log chi tiết snapshot
        Debug.Log($"HandleRoomValueChanged triggered for room {currentRoomId}. Snapshot Exists: {snapshot.Exists}. Snapshot Value: {snapshot.GetRawJsonValue()}");

        if (!snapshot.Exists) // Phòng đã bị xóa (ví dụ: chủ phòng rời đi)
        {
            Debug.Log($"Room {currentRoomId} no longer exists.");
            StopListeningToRoomChanges();
            ClearPlayerListUI();
            // Thông báo rằng phòng đã kết thúc/bị hủy
            OnLeaveRoomCompleted?.Invoke(); // Sử dụng lại event OnLeaveRoomCompleted
            // HideRoom(); // HideRoom sẽ được gọi bởi OnLeaveRoomCompleted listener
            return;
        }

        // Xử lý dữ liệu phòng từ snapshot
        string hostId = snapshot.Child("hostId").GetValue(true)?.ToString();
        // string roomStatus = snapshot.Child("status").GetValue(true)?.ToString(); // Nếu có status

        // Lấy danh sách người chơi và trạng thái Ready của họ
        List<PlayerInRoomInfo> playersInRoom = new List<PlayerInRoomInfo>();
        DataSnapshot playersSnapshot = snapshot.Child("players");

        if (playersSnapshot.Exists && playersSnapshot.ChildrenCount > 0)
        {
            foreach (var playerChild in playersSnapshot.Children)
            {
                string playerId = playerChild.Key;
                string playerName = playerChild.Child("userName").GetValue(true)?.ToString() ?? "Unknown";
                bool isReady = playerChild.Child("isReady").GetValue(true) as bool? ?? false; // Đọc boolean, mặc định false

                playersInRoom.Add(new PlayerInRoomInfo { userId = playerId, userName = playerName, isReady = isReady });
            }
        }

        // Cập nhật UI danh sách người chơi
        UpdatePlayerListUI(playersInRoom);

        // Kiểm tra trạng thái sẵn sàng để bật/tắt nút Start
        CheckAndEnableStartButton(playersInRoom, hostId);

        // Kiểm tra trạng thái phòng (nếu có node status) để chuyển scene
        // if (roomStatus == "in_game")
        // {
        //      OnGameStarted?.Invoke(currentRoomId);
        //      HideRoom();
        // }
    }


    // --- Logic cập nhật UI ---

    private void UpdatePlayerListUI(List<PlayerInRoomInfo> players)
    {
        if (contentParent == null) return;

        // Xóa các UI cũ không còn trong danh sách players
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

        // Cập nhật hoặc thêm mới UI cho từng người chơi trong danh sách
        foreach (var player in players)
        {
            if (playerUIItems.TryGetValue(player.userId, out UI_PlayersInRoom uiItem))
            {
                // Cập nhật UI Item hiện có
                uiItem.SetPlayerData(player.userName, player.userId, player.isReady);
                // Bật/tắt tương tác nút Ready chỉ cho người chơi hiện tại
                bool isCurrentUser = player.userId == currentUserId;
                uiItem.SetButtonInteractable(isCurrentUser);
                // Gỡ bỏ listener cũ trước khi thêm lại (đảm bảo chỉ có 1 listener)
                uiItem.OnReadyStatusChanged -= HandlePlayerReadyStatusChanged;
                uiItem.OnReadyStatusChanged += HandlePlayerReadyStatusChanged;

            }
            else
            {
                // Tạo UI Item mới nếu chưa có
                GameObject playerItemGO = Instantiate(playerItemPrefab.gameObject, contentParent);
                UI_PlayersInRoom newUIItem = playerItemGO.GetComponent<UI_PlayersInRoom>();
                if (newUIItem != null)
                {
                    newUIItem.SetPlayerData(player.userName, player.userId, player.isReady);
                    // Bật/tắt tương tác nút Ready chỉ cho người chơi hiện tại
                    bool isCurrentUser = player.userId == currentUserId;
                    newUIItem.SetButtonInteractable(isCurrentUser);
                    // Đăng ký lắng nghe event ReadyStatusChanged của UI Item mới
                    newUIItem.OnReadyStatusChanged += HandlePlayerReadyStatusChanged;

                    playerUIItems[player.userId] = newUIItem; // Thêm vào dictionary quản lý
                }
            }
        }
    }

    // Xóa tất cả các mục UI người chơi
    private void ClearPlayerListUI()
    {
        if (contentParent == null) return;

        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }
        playerUIItems.Clear(); // Xóa hết khỏi dictionary quản lý
    }


    // --- Logic xử lý sự kiện ---

    // Xử lý khi trạng thái Ready của người chơi thay đổi trên UI Item
    private void HandlePlayerReadyStatusChanged(string userId, bool isReady)
    {
        Debug.Log($"Player {userId} changed ready status to {isReady}");
        // Ghi trạng thái Ready mới lên Firebase
        UpdatePlayerReadyStatusOnFirebase(userId, isReady);
    }

    // Cập nhật trạng thái Ready của người chơi trên Firebase
    private void UpdatePlayerReadyStatusOnFirebase(string userId, bool isReady)
    {
        if (dbReference == null || string.IsNullOrEmpty(currentRoomId) || string.IsNullOrEmpty(userId)) return;

        // Ghi trạng thái Ready vào Rooms/{roomId}/players/{userId}/isReady
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

    // Kiểm tra và bật/tắt nút Start
    private void CheckAndEnableStartButton(List<PlayerInRoomInfo> players, string hostId)
    {
        if (startGameButton == null) return;

        // Nút Start chỉ hiển thị và tương tác được cho chủ phòng
        if (currentUserId == hostId)
        {
            startGameButton.gameObject.SetActive(true); // Đảm bảo nút Start hiển thị cho host

            // Kiểm tra xem có đủ 2 người chơi và cả 2 đã Ready chưa
            bool allPlayersReady = players != null && players.Count == 2 && players.All(p => p.isReady);
            startGameButton.interactable = allPlayersReady; // Bật nút nếu cả 2 Ready
        }
        else
        {
            // Nút Start không hiển thị cho người chơi không phải chủ phòng
            startGameButton.gameObject.SetActive(false);
        }
    }

    // Cần thêm logic xử lý khi game bắt đầu (chuyển scene)
    // Logic này sẽ được kích hoạt khi event OnGameStarted được gọi từ Firebase listener
}