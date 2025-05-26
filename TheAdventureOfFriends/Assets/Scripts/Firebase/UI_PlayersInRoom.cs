using System; // Cần thiết cho Action
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_PlayersInRoom : MonoBehaviour
{
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private Button readyToggleButton; // Nút Ready/Unready

    private string userId; // ID của người chơi mà mục UI này đại diện
    private bool isReady = false; // Trạng thái sẵn sàng hiện tại

    // Event để thông báo ra ngoài khi trạng thái Ready thay đổi
    // Script quản lý phòng chờ sẽ lắng nghe event này để cập nhật trên Firebase
    public event Action<string, bool> OnReadyStatusChanged;

    // Phương thức để thiết lập dữ liệu cho mục người chơi trong phòng
    public void SetPlayerData(string playerName, string id, bool initialReadyStatus)
    {
        playerNameText.text = playerName;
        userId = id;
        isReady = initialReadyStatus;

        // Cập nhật trạng thái hiển thị ban đầu của nút
        UpdateReadyButtonUI();

        // Gỡ bỏ các listener cũ để tránh nhân đôi sự kiện
        if (readyToggleButton != null)
        {
            readyToggleButton.onClick.RemoveAllListeners();
            // Thêm listener mới gọi phương thức xử lý khi nút được bấm
            readyToggleButton.onClick.AddListener(OnReadyButtonClick);
        }
    }

    // Phương thức xử lý khi nút Ready/Unready được bấm
    private void OnReadyButtonClick()
    {
        // Chỉ cho phép tương tác nếu đây là mục của người chơi hiện tại (cần check)
        // Tạm thời cho phép bấm để test, logic check người chơi hiện tại sẽ ở script quản lý phòng
        // if (userId == PlayersRecommendationManager.Instance.GetCurrentUserId())
        // {
        isReady = !isReady; // Đảo ngược trạng thái sẵn sàng
        UpdateReadyButtonUI(); // Cập nhật giao diện nút

        // Kích hoạt event để script quản lý phòng biết trạng thái đã thay đổi
        OnReadyStatusChanged?.Invoke(userId, isReady);
        // }
        // else
        // {
        // Debug.Log("Cannot change ready status for other players.");
        // }
    }

    // Cập nhật Text của nút Ready/Unready dựa trên trạng thái hiện tại
    private void UpdateReadyButtonUI()
    {
        if (readyToggleButton != null)
        {
            // Giả định nút có Text child là TMP_Text
            TMP_Text buttonText = readyToggleButton.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.text = isReady ? "Unready" : "Ready";
                // Tùy chọn: thay đổi màu nút/text
                // buttonText.color = isReady ? Color.red : Color.green;
            }
        }
    }

    // Thêm phương thức để bật/tắt tương tác của nút (ví dụ: chỉ chủ phòng mới có nút Start/Leave)
    public void SetButtonInteractable(bool interactable)
    {
        if (readyToggleButton != null)
        {
            readyToggleButton.interactable = interactable;
        }
    }
}