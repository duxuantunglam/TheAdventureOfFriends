using System; // Cần thiết cho Action
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_PlayersInRoom : MonoBehaviour
{
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private Button readyToggleButton;

    private string userId;
    private bool isReady = false;

    public event Action<string, bool> OnReadyStatusChanged;

    public void SetPlayerData(string playerName, string id, bool initialReadyStatus)
    {
        playerNameText.text = playerName;
        userId = id;
        isReady = initialReadyStatus;

        UpdateReadyButtonUI();

        if (readyToggleButton != null)
        {
            readyToggleButton.onClick.RemoveAllListeners();
            readyToggleButton.onClick.AddListener(OnReadyButtonClick);
        }
    }

    private void OnReadyButtonClick()
    {
        isReady = !isReady;
        UpdateReadyButtonUI();

        OnReadyStatusChanged?.Invoke(userId, isReady);
    }

    private void UpdateReadyButtonUI()
    {
        if (readyToggleButton != null)
        {
            TMP_Text buttonText = readyToggleButton.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.text = isReady ? "Unready" : "Ready";
            }
        }
    }

    public string GetPlayerName()
    {
        return playerNameText.text;
    }

    public void SetButtonInteractable(bool interactable)
    {
        if (readyToggleButton != null)
        {
            readyToggleButton.interactable = interactable;
        }
    }
}