using System.Collections.Generic;
using Firebase.Database;
using Firebase.Extensions;
using Newtonsoft.Json;
using UnityEngine;

public class MultiplayerSceneManager : MonoBehaviour
{
    [Header("Player Setup")]
    [SerializeField] private GameObject multiplayerPlayerPrefab;
    [SerializeField] private Transform player1SpawnPoint;
    [SerializeField] private Transform player2SpawnPoint;

    [Header("Game Settings")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float cameraFollowSpeed = 2f;

    private DatabaseReference dbReference;
    private string currentRoomId;
    private string currentUserId;
    private List<MultiplayerPlayer> players = new List<MultiplayerPlayer>();
    private List<string> playersInRoom = new List<string>();

    private void Start()
    {
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;
        currentRoomId = PlayerPrefs.GetString("CurrentRoomId", "");
        currentUserId = PlayerPrefs.GetString("CurrentUserId", "");

        if (string.IsNullOrEmpty(currentRoomId) || string.IsNullOrEmpty(currentUserId))
        {
            Debug.LogError("Missing room ID or user ID! Returning to main menu...");
            ReturnToMainMenu();
            return;
        }

        Debug.Log($"Multiplayer scene loaded for room: {currentRoomId}, user: {currentUserId}");

        // Setup camera nếu chưa có
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        // Load players in room và spawn
        LoadPlayersInRoom();
    }

    private async void LoadPlayersInRoom()
    {
        try
        {
            Debug.Log($"Loading players for room: {currentRoomId}");
            var snapshot = await dbReference.Child("Rooms").Child(currentRoomId).Child("players").GetValueAsync();

            if (snapshot.Exists)
            {
                Debug.Log($"Room exists with {snapshot.ChildrenCount} players");

                int playerIndex = 0;
                foreach (var playerChild in snapshot.Children)
                {
                    string playerId = playerChild.Key;
                    string playerName = playerChild.Child("userName").GetValue(true)?.ToString() ?? "Unknown";

                    Debug.Log($"Found player: {playerName} (ID: {playerId})");

                    playersInRoom.Add(playerId);
                    SpawnPlayer(playerId, playerName, playerIndex);
                    playerIndex++;
                }

                Debug.Log($"Spawned {playersInRoom.Count} players in multiplayer scene");

                // Setup camera theo local player
                SetupCameraFollow();

                // Tạo GameRoom data trên Firebase
                await CreateGameRoomData();
            }
            else
            {
                Debug.LogError("Room not found! Returning to main menu...");
                ReturnToMainMenu();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading players: {e.Message}");
            // Không return về main menu ngay, thử lại
            Debug.LogWarning("Will retry loading players in 2 seconds...");
            Invoke(nameof(LoadPlayersInRoom), 2f);
        }
    }

    private void SpawnPlayer(string playerId, string playerName, int playerIndex)
    {
        // Xác định spawn point
        Transform spawnPoint = (playerIndex == 0) ? player1SpawnPoint : player2SpawnPoint;
        if (spawnPoint == null)
        {
            Debug.LogWarning($"Spawn point not found for player {playerIndex}. Using default position.");
            spawnPoint = transform;
        }

        // Spawn player
        GameObject playerGO = Instantiate(multiplayerPlayerPrefab, spawnPoint.position, spawnPoint.rotation);
        MultiplayerPlayer player = playerGO.GetComponent<MultiplayerPlayer>();

        if (player != null)
        {
            // Initialize player với thông tin
            bool isLocal = (playerId == currentUserId);
            player.InitializePlayer(playerId, playerName, isLocal);

            players.Add(player);

            Debug.Log($"Player spawned: {playerName} (ID: {playerId}, Local: {isLocal}) at position {spawnPoint.position}");
        }
        else
        {
            Debug.LogError("MultiplayerPlayer component not found on prefab!");
        }
    }

    private void SetupCameraFollow()
    {
        if (mainCamera == null) return;

        // Tìm local player để camera follow
        MultiplayerPlayer localPlayer = players.Find(p => p.IsLocalPlayer());

        if (localPlayer != null)
        {
            // Thêm script camera follow nếu cần
            CameraFollowMultiplayer cameraFollow = mainCamera.GetComponent<CameraFollowMultiplayer>();
            if (cameraFollow == null)
            {
                cameraFollow = mainCamera.gameObject.AddComponent<CameraFollowMultiplayer>();
            }

            cameraFollow.SetTarget(localPlayer.transform);
            Debug.Log($"Camera following local player: {localPlayer.GetPlayerDisplayName()}");
        }
        else
        {
            Debug.LogWarning("No local player found for camera follow!");
        }
    }

    private void Update()
    {
        // Có thể thêm các check game state ở đây
        CheckGameState();
    }

    private void CheckGameState()
    {
        // Check nếu cần quit game (ESC key)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ShowPauseMenu();
        }
    }

    private void ShowPauseMenu()
    {
        // TODO: Implement pause menu
        Debug.Log("Pause menu requested");
    }

    public void ReturnToMainMenu()
    {
        // Cleanup Firebase listeners trước khi rời scene
        foreach (var player in players)
        {
            if (player != null)
            {
                Destroy(player.gameObject);
            }
        }

        // Clear PlayerPrefs
        PlayerPrefs.DeleteKey("CurrentRoomId");
        PlayerPrefs.DeleteKey("CurrentUserId");

        // Load main menu scene
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    public void HandlePlayerDisconnected(string playerId)
    {
        Debug.Log($"Player {playerId} disconnected");

        // Tìm và xóa player
        MultiplayerPlayer playerToRemove = players.Find(p => p.GetPlayerId() == playerId);

        if (playerToRemove != null)
        {
            players.Remove(playerToRemove);
            Destroy(playerToRemove.gameObject);
            Debug.Log($"Removed player: {playerToRemove.GetPlayerDisplayName()}");
        }

        // Nếu chỉ còn 1 người chơi, có thể show notification hoặc return về menu
        if (players.Count <= 1)
        {
            Debug.Log("Not enough players. Returning to main menu...");
            // Có thể show dialog trước khi return
            Invoke(nameof(ReturnToMainMenu), 3f);
        }
    }

    // Thêm method để tạo GameRoom data
    private async System.Threading.Tasks.Task CreateGameRoomData()
    {
        try
        {
            var gameRoomData = new GameRoomData
            {
                roomId = currentRoomId,
                gameStatus = "playing",
                gameStartTime = System.DateTimeOffset.Now.ToUnixTimeMilliseconds()
            };

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(gameRoomData);
            await dbReference.Child("GameRooms").Child(currentRoomId).SetRawJsonValueAsync(json);

            Debug.Log($"Created GameRoom data for room: {currentRoomId}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to create GameRoom data: {e.Message}");
        }
    }
}

// Camera follow script cho multiplayer
public class CameraFollowMultiplayer : MonoBehaviour
{
    private Transform target;
    [SerializeField] private float followSpeed = 2f;
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10);

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    private void LateUpdate()
    {
        if (target != null)
        {
            Vector3 desiredPosition = target.position + offset;
            transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
        }
    }
}