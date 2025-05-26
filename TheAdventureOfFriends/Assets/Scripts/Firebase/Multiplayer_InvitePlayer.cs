using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

[Serializable]
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

    private async void OnEnable()
    {
        string currentUserId = PlayersRecommendationManager.Instance.GetCurrentUserId();
        if (string.IsNullOrEmpty(currentUserId))
        {
            Debug.LogError("Cannot get current user ID to load recommended players.");
            return;
        }

        Debug.Log("Loading recommended players...");

        List<RecommendedPlayerData> recommendedPlayers = await PlayersRecommendationManager.Instance.GetRecommendedPlayersAsync(currentUserId);

        Debug.Log($"Multiplayer_InvitePlayer: Received {recommendedPlayers?.Count ?? 0} recommended players from manager.");
        if (recommendedPlayers != null)
        {
            foreach (var player in recommendedPlayers)
            {
                Debug.Log($"  - Recommended: {player.userName} ({player.userId}), Online: {player.isOnline}");
            }
        }

        Debug.Log("Recommended players loaded.");

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
            GameObject playerItem = Instantiate(playerItemPrefab.gameObject, contentParent);
            UI_PlayersRecommended playerItemUI = playerItem.GetComponent<UI_PlayersRecommended>();

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

    private async Task HandlePlayerInviteClicked(string invitedUserId)
    {
        Debug.Log($"Invite button clicked for user ID: {invitedUserId}");
        string currentUserId = PlayersRecommendationManager.Instance.GetCurrentUserId();

        if (string.IsNullOrEmpty(currentUserId))
        {
            Debug.LogError("Current user ID is not available. Cannot send invitation.");
            return;
        }

        if (waitingRoomUI != null)
        {
            await waitingRoomUI.ShowRoom(currentUserId, null, invitedUserId);
            invitePlayerUIPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("UI_WaitingRoom reference is not set in Multiplayer_InvitePlayer script.");
        }
    }
}