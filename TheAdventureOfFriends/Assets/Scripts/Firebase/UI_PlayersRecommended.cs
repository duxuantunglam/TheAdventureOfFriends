using System; // Cần thiết cho Action
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_PlayersRecommended : MonoBehaviour
{
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button inviteButton;

    private string userId;
    private bool isOnline;

    private Action<string> onInviteClickCallback;

    public void SetPlayerData(string playerName, string status, string id, bool initialOnlineStatus, Action<string> onInviteClickAction)
    {
        playerNameText.text = playerName;
        statusText.text = status;
        userId = id;
        isOnline = initialOnlineStatus;
        onInviteClickCallback = onInviteClickAction;

        UpdateUI();

        if (inviteButton != null)
        {
            inviteButton.onClick.RemoveAllListeners();
            inviteButton.onClick.AddListener(HandleInviteButtonClick);
        }
    }

    private void HandleInviteButtonClick()
    {
        Debug.Log($"Invite button clicked for user ID: {userId}");
        onInviteClickCallback?.Invoke(userId);
    }

    private void UpdateUI()
    {
        if (statusText != null)
        {
            statusText.color = isOnline ? Color.green : Color.gray;
        }

        if (inviteButton != null)
        {
            inviteButton.interactable = isOnline;
        }
    }
}