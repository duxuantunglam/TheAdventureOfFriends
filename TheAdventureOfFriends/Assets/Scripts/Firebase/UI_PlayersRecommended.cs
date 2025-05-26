using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_PlayersRecommended : MonoBehaviour
{
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button invitePlayerButton;

    private string playerId;

    public void SetPlayerData(string playerName, string status, string id, bool isOnline, Action<string> onInviteClicked)
    {
        playerNameText.text = playerName;
        statusText.text = status;
        playerId = id;

        if (isOnline)
        {
            statusText.color = Color.green;
        }
        else
        {
            statusText.color = Color.gray;
        }

        invitePlayerButton.onClick.RemoveAllListeners();

        if (isOnline && onInviteClicked != null)
        {
            invitePlayerButton.onClick.AddListener(() => onInviteClicked.Invoke(playerId));
        }
    }
}