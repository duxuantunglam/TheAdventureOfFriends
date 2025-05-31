using UnityEngine;
using UnityEngine.SceneManagement;

public class MultiplayerGameManager : MonoBehaviour
{
    [SerializeField] private UI_WaitingRoom waitingRoomUI;

    private void Start()
    {
        // Đăng ký event OnGameStarted từ UI_WaitingRoom
        if (waitingRoomUI != null)
        {
            waitingRoomUI.OnGameStarted += HandleGameStarted;
        }
    }

    private void OnDestroy()
    {
        // Hủy đăng ký event để tránh memory leak
        if (waitingRoomUI != null)
        {
            waitingRoomUI.OnGameStarted -= HandleGameStarted;
        }
    }

    private void HandleGameStarted(string roomId)
    {
        Debug.Log($"Game started! Loading MultiplayerScene for room: {roomId}");

        // Lưu roomId và userId để sử dụng trong MultiplayerScene
        PlayerPrefs.SetString("CurrentRoomId", roomId);

        // Đảm bảo CurrentUserId chính xác
        string currentUserId = FirebaseManager.CurrentUser?.id ?? "";
        if (string.IsNullOrEmpty(currentUserId))
        {
            Debug.LogError("Current user ID is null or empty!");
            return;
        }

        PlayerPrefs.SetString("CurrentUserId", currentUserId);
        Debug.Log($"Saved to PlayerPrefs - RoomId: {roomId}, UserId: {currentUserId}");

        // Chuyển scene trực tiếp (bỏ fade effect)
        LoadMultiplayerScene();
    }

    private void LoadMultiplayerScene()
    {
        SceneManager.LoadScene("MultiplayerScene");
    }
}