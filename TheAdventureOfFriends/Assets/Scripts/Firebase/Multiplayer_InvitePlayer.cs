using System;
using System.Collections.Generic;
using System.Threading.Tasks; // Cần thiết cho async/await
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Action = System.Action;

[System.Serializable]
public class RecommendedPlayerData
{
    public string userId;
    public string userName;
    public string status;
    public bool isOnline;
}

public class Multiplayer_InvitePlayer : MonoBehaviour
{
    [SerializeField] private GameObject invitePlayerUIPanel;
    [SerializeField] private UI_PlayersRecommended playerItemPrefab;
    [SerializeField] private Transform contentParent;
    [SerializeField] private UI_WaitingRoom waitingRoomUI;

    public event Action<string> OnPlayerInviteClicked;

    // Sử dụng OnEnable để tải danh sách khi panel được kích hoạt
    private async void OnEnable()
    {
        // Lấy User ID của người chơi hiện tại thông qua PlayersRecommendationManager
        string currentUserId = PlayersRecommendationManager.Instance.GetCurrentUserId();
        if (string.IsNullOrEmpty(currentUserId))
        {
            Debug.LogError("Cannot get current user ID to load recommended players.");
            // Có thể hiển thị thông báo lỗi hoặc ẩn panel nếu cần
            // if (invitePlayerUIPanel != null) invitePlayerUIPanel.SetActive(false);
            return;
        }

        // Hiển thị trạng thái loading hoặc spinner nếu có
        Debug.Log("Loading recommended players...");

        // Tải và lấy danh sách người chơi được đề xuất một cách bất đồng bộ
        List<RecommendedPlayerData> recommendedPlayers = await PlayersRecommendationManager.Instance.GetRecommendedPlayersAsync(currentUserId);

        // Thêm Debug.Log để kiểm tra danh sách nhận được
        Debug.Log($"Multiplayer_InvitePlayer: Received {recommendedPlayers?.Count ?? 0} recommended players from manager.");
        if (recommendedPlayers != null)
        {
            foreach (var player in recommendedPlayers)
            {
                Debug.Log($"  - Recommended: {player.userName} ({player.userId}), Online: {player.isOnline}");
            }
        }

        // Ẩn trạng thái loading
        Debug.Log("Recommended players loaded.");

        // Điền danh sách vào UI
        PopulatePlayerList(recommendedPlayers);
    }

    public void HidePanel()
    {
        if (invitePlayerUIPanel != null)
        {
            invitePlayerUIPanel.SetActive(false);
            ClearPlayerList();
        }
    }

    public void PopulatePlayerList(List<RecommendedPlayerData> players)
    {
        ClearPlayerList();

        if (players == null)
        {
            Debug.LogWarning("Multiplayer_InvitePlayer: Player list is null.");
            return;
        }

        foreach (var player in players)
        {
            GameObject playerItemGO = Instantiate(playerItemPrefab.gameObject, contentParent);
            UI_PlayersRecommended playerItemUI = playerItemGO.GetComponent<UI_PlayersRecommended>();

            if (playerItemUI != null)
            {
                playerItemUI.SetPlayerData(
                    player.userName,
                    player.status,
                    player.userId,
                    player.isOnline,
                    async (invitedPlayerId) =>
                    {
                        await HandlePlayerInviteClicked(invitedPlayerId);
                    }
                );
            }
        }
    }

    private void ClearPlayerList()
    {
        if (contentParent == null)
        {
            Debug.LogError("Multiplayer_InvitePlayer: Content parent is not assigned!");
            return;
        }

        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }
    }

    // Phương thức xử lý khi nút Invite trên một mục người chơi được bấm
    private async Task HandlePlayerInviteClicked(string invitedUserId)
    {
        Debug.Log($"Invite button clicked for user ID: {invitedUserId}");
        // Lấy ID người chơi hiện tại (người mời)
        string currentUserId = PlayersRecommendationManager.Instance.GetCurrentUserId();

        if (string.IsNullOrEmpty(currentUserId))
        {
            Debug.LogError("Current user ID is not available. Cannot send invitation.");
            return;
        }

        // Kiểm tra xem tham chiếu waitingRoomUI đã được gán chưa
        if (waitingRoomUI != null)
        {
            // Gọi phương thức ShowRoom trên UI_WaitingRoom để tạo phòng và gửi lời mời
            // Truyền currentUserId (người mời), null (tạo phòng mới), và invitedUserId (người được mời)
            await waitingRoomUI.ShowRoom(currentUserId, null, invitedUserId);

            // Sau khi gửi lời mời/tạo phòng, có thể ẩn giao diện danh sách người chơi đề xuất
            // playersRecommendedUIPanel.SetActive(false); // Tùy chọn
        }
        else
        {
            Debug.LogError("UI_WaitingRoom reference is not set in Multiplayer_InvitePlayer script.");
        }

        // Cần thêm logic để xử lý khi lời mời được gửi thành công (ví dụ: hiển thị trạng thái chờ)
    }
}