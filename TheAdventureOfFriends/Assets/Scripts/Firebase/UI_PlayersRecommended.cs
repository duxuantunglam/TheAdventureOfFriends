using System; // Cần thiết cho Action
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_PlayersRecommended : MonoBehaviour
{
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button inviteButton;

    private string userId; // ID của người chơi mà mục UI này đại diện
    private bool isOnline; // Trạng thái online/offline

    private Action<string> onInviteClickCallback; // Lưu lại Action từ bên ngoài

    // Phương thức để thiết lập dữ liệu cho mục người chơi
    public void SetPlayerData(string playerName, string status, string id, bool initialOnlineStatus, Action<string> onInviteClickAction)
    {
        playerNameText.text = playerName;
        statusText.text = status;
        userId = id;
        isOnline = initialOnlineStatus;
        onInviteClickCallback = onInviteClickAction; // Lưu lại Action

        // Cập nhật giao diện trạng thái và nút Invite
        UpdateUI();

        // Gỡ bỏ các listener cũ để tránh nhân đôi sự kiện
        if (inviteButton != null)
        {
            inviteButton.onClick.RemoveAllListeners();
            // Thêm listener mới gọi phương thức xử lý khi nút được bấm
            inviteButton.onClick.AddListener(HandleInviteButtonClick); // Gọi phương thức xử lý mới
        }
    }

    // Phương thức xử lý khi nút Invite được bấm
    private void HandleInviteButtonClick() // Đổi tên cho rõ ràng
    {
        Debug.Log($"Invite button clicked for user ID: {userId}");
        // Kích hoạt Action/callback đã nhận từ script quản lý (Multiplayer_InvitePlayer)
        onInviteClickCallback?.Invoke(userId); // <-- Gọi Action ở đây
    }

    // Cập nhật trạng thái hiển thị và tương tác của nút Invite
    private void UpdateUI()
    {
        if (statusText != null)
        {
            // Cập nhật màu chữ trạng thái
            statusText.color = isOnline ? Color.green : Color.gray;
        }

        if (inviteButton != null)
        {
            // Nút Invite chỉ tương tác được khi người chơi online
            inviteButton.interactable = isOnline;
            // Có thể thay đổi màu nút hoặc text của nút dựa trên trạng thái interactable nếu cần
        }
    }

    // Bạn có thể thêm các phương thức khác ở đây nếu cần,
    // ví dụ: cập nhật trạng thái động, thay đổi màu sắc dựa trên trạng thái, v.v.
}