using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_NotificationInGame : MonoBehaviour
{
    [SerializeField] private GameObject notificationPanel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button declineButton;
    [SerializeField] private Button closeButton;

    public void ShowNotification(string title, string message, Action onAccept, Action onDecline, Action onClose)
    {
        if (notificationPanel != null)
        {
            notificationPanel.SetActive(true);

            if (titleText != null) titleText.text = title;
            if (messageText != null) messageText.text = message;

            if (acceptButton != null) acceptButton.onClick.RemoveAllListeners();
            if (declineButton != null) declineButton.onClick.RemoveAllListeners();
            if (closeButton != null) closeButton.onClick.RemoveAllListeners();

            if (acceptButton != null)
            {
                acceptButton.onClick.AddListener(() =>
                {
                    onAccept?.Invoke();
                    HideNotification();
                });
            }

            if (declineButton != null)
            {
                declineButton.onClick.AddListener(() =>
                {
                    onDecline?.Invoke();
                    HideNotification();
                });
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(() =>
                {
                    onClose?.Invoke();
                    HideNotification();
                });
            }
        }
    }

    public void HideNotification()
    {
        if (notificationPanel != null)
        {
            notificationPanel.SetActive(false);

            if (acceptButton != null) acceptButton.onClick.RemoveAllListeners();
            if (declineButton != null) declineButton.onClick.RemoveAllListeners();
            if (closeButton != null) closeButton.onClick.RemoveAllListeners();
        }
    }
}