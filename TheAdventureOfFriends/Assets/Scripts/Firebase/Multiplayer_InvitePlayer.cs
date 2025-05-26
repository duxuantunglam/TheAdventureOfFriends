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
                    (invitedPlayerId) =>
                    {
                        OnPlayerInviteClicked?.Invoke(invitedPlayerId);
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
}