using System;
using System.Collections.Generic;
using System.Linq; // Cần thiết cho LINQ (OrderByDescending)
using System.Threading.Tasks; // Cần thiết cho các thao tác bất đồng bộ
using Firebase.Database; // Cần thiết để tương tác với Firebase Realtime Database
using UnityEngine;

// Cấu trúc dữ liệu để lưu trữ các chỉ số cần cho recommendation
// Giả định UserData trong Authentication.cs có các trường này
[System.Serializable] // Serializable để có thể xem trong Inspector nếu cần
public class PlayerStatsForRecommendation
{
    public string id; // User ID
    public string userName; // User Name
    public float averageFruit;
    public float averageTime;
    public float averageEnemiesKilled;
    public float averageKnockBacks;
    // Thêm các chỉ số khác nếu cần
}

// Class để chứa thông tin người chơi cùng với điểm phù hợp
public class RecommendedPlayerInfo : RecommendedPlayerData // Kế thừa từ RecommendedPlayerData đã dùng trong Multiplayer_InvitePlayer
{
    public float suitabilityScore; // Điểm phù hợp
}

public class PlayersRecommendationManager // Không kế thừa từ MonoBehaviour nếu là class service thuần
{
    // Instance singleton (ví dụ đơn giản)
    private static PlayersRecommendationManager _instance;
    public static PlayersRecommendationManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new PlayersRecommendationManager();
                // Khởi tạo Firebase Database Reference ở đây
                _instance.dbReference = FirebaseDatabase.DefaultInstance.RootReference;
            }
            return _instance;
        }
    }

    private DatabaseReference dbReference; // Tham chiếu đến Firebase Database

    private PlayersRecommendationManager()
    {
        // Constructor private để chỉ Instance mới có thể tạo đối tượng
    }

    // Phương thức tải tất cả dữ liệu stats của người chơi từ Firebase
    private async Task<List<PlayerStatsForRecommendation>> LoadAllPlayerStatsAsync()
    {
        List<PlayerStatsForRecommendation> allPlayerStats = new List<PlayerStatsForRecommendation>();
        try
        {
            DataSnapshot snapshot = await dbReference.Child("PlayerStats").GetValueAsync();

            if (snapshot.Exists && snapshot.ChildrenCount > 0)
            {
                foreach (var childSnapshot in snapshot.Children)
                {
                    // Giả định cấu trúc PlayerStats trên Firebase khớp với PlayerStatsForRecommendation
                    // và có thể Deserialize trực tiếp hoặc ánh xạ thủ công
                    string json = childSnapshot.GetRawJsonValue();
                    PlayerStatsForRecommendation playerStats = JsonUtility.FromJson<PlayerStatsForRecommendation>(json);

                    if (playerStats != null)
                    {
                        // Firebase Realtime Database không tự động điền Key vào object
                        playerStats.id = childSnapshot.Key; // Gán User ID từ Key của node
                        allPlayerStats.Add(playerStats);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load player stats from Firebase: {e}");
        }

        return allPlayerStats;
    }

    // Phương thức tải trạng thái online/offline từ Firebase
    private async Task<Dictionary<string, bool>> LoadPlayerOnlineStatusAsync(List<string> playerIds)
    {
        Dictionary<string, bool> onlineStatus = new Dictionary<string, bool>();
        if (playerIds == null || playerIds.Count == 0)
        {
            return onlineStatus; // Trả về dictionary rỗng nếu không có player IDs
        }

        // Đọc trạng thái online cho từng người chơi
        // Cách này có thể không hiệu quả nếu số lượng playerIds rất lớn.
        // Một cách hiệu quả hơn là đọc toàn bộ node chứa trạng thái và lọc sau.
        // Tuy nhiên, để minh họa đọc trạng thái cụ thể, tôi sẽ làm theo cách này trước.
        // Bạn có thể cân nhắc tối ưu hóa sau.

        foreach (var playerId in playerIds)
        {
            try
            {
                // Giả định trạng thái online được lưu tại Users/{userId}/isOnline (boolean)
                // Hoặc bạn có thể kiểm tra sự tồn tại của node Presence/{userId}
                DataSnapshot snapshot = await dbReference.Child("Users").Child(playerId).Child("isOnline").GetValueAsync();

                if (snapshot.Exists)
                {
                    // Đọc giá trị boolean
                    bool status = (bool)snapshot.Value;
                    onlineStatus[playerId] = status;
                }
                else
                {
                    // Mặc định offline nếu không tìm thấy thông tin trạng thái
                    onlineStatus[playerId] = false;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load online status for player {playerId} from Firebase: {e}");
                onlineStatus[playerId] = false; // Mặc định offline nếu có lỗi
            }
        }

        return onlineStatus;
    }

    // Chuẩn hóa vector đặc điểm (L2 Normalization)
    private float[] NormalizeVector(float[] vector)
    {
        float sumOfSquares = 0;
        for (int i = 0; i < vector.Length; i++)
        {
            sumOfSquares += vector[i] * vector[i];
        }
        float magnitude = Mathf.Sqrt(sumOfSquares);

        if (magnitude == 0) return new float[vector.Length]; // Trả về vector 0 nếu magnitude bằng 0

        float[] normalizedVector = new float[vector.Length];
        for (int i = 0; i < vector.Length; i++)
        {
            normalizedVector[i] = vector[i] / magnitude;
        }
        return normalizedVector;
    }

    // Tính Cosine Similarity giữa hai vector
    private float CalculateCosineSimilarity(float[] vector1, float[] vector2)
    {
        if (vector1.Length != vector2.Length)
        {
            Debug.LogError("Vectors must have the same length for Cosine Similarity.");
            return 0;
        }

        float dotProduct = 0;
        float magnitude1 = 0;
        float magnitude2 = 0;

        for (int i = 0; i < vector1.Length; i++)
        {
            dotProduct += vector1[i] * vector2[i];
            magnitude1 += vector1[i] * vector1[i];
            magnitude2 += vector2[i] * vector2[i];
        }

        magnitude1 = Mathf.Sqrt(magnitude1);
        magnitude2 = Mathf.Sqrt(magnitude2);

        // Tránh chia cho 0
        if (magnitude1 == 0 || magnitude2 == 0) return 0;

        return dotProduct / (magnitude1 * magnitude2);
    }

    // Phương thức chính để lấy danh sách người chơi được đề xuất
    public async Task<List<RecommendedPlayerData>> GetRecommendedPlayersAsync(string currentUserId)
    {
        // 1. Tải tất cả stats người chơi
        List<PlayerStatsForRecommendation> allPlayerStats = await LoadAllPlayerStatsAsync();

        if (allPlayerStats == null || allPlayerStats.Count == 0)
        {
            Debug.LogWarning("No player stats loaded.");
            return new List<RecommendedPlayerData>();
        }

        // Tìm stats của người chơi hiện tại
        PlayerStatsForRecommendation currentUserStats = allPlayerStats.FirstOrDefault(p => p.id == currentUserId);

        if (currentUserStats == null)
        {
            Debug.LogWarning($"Stats not found for current user ID: {currentUserId}. Cannot generate recommendations.");
            // Trả về danh sách rỗng hoặc tất cả người chơi khác không được sắp xếp
            return allPlayerStats.Where(p => p.id != currentUserId)
                                .Select(p => new RecommendedPlayerData { userId = p.id, userName = p.userName, status = "Unknown", isOnline = false }) // Trạng thái và tên cần tải riêng
                                .ToList();
        }

        // Lấy danh sách ID của tất cả người chơi khác
        List<string> otherPlayerIds = allPlayerStats.Where(p => p.id != currentUserId).Select(p => p.id).ToList();

        // 2. Tải trạng thái online/offline của các người chơi khác
        // Bạn cần điều chỉnh phương thức này để tải trạng thái thực tế
        Dictionary<string, bool> onlineStatus = await LoadPlayerOnlineStatusAsync(otherPlayerIds);


        // 3. Chuẩn bị vector đặc điểm cho người chơi hiện tại
        float[] currentUserVector = new float[] {
            currentUserStats.averageFruit,
            currentUserStats.averageTime,
            currentUserStats.averageEnemiesKilled,
            currentUserStats.averageKnockBacks
            // Thêm các chỉ số khác vào đây
        };
        // Chuẩn hóa vector người chơi hiện tại
        float[] normalizedCurrentUserVector = NormalizeVector(currentUserVector);

        List<RecommendedPlayerInfo> recommendedPlayers = new List<RecommendedPlayerInfo>();

        // 4. Tính toán độ phù hợp và tạo danh sách đề xuất
        foreach (var otherPlayerStats in allPlayerStats.Where(p => p.id != currentUserId))
        {
            float[] otherPlayerVector = new float[] {
                otherPlayerStats.averageFruit,
                otherPlayerStats.averageTime,
                otherPlayerStats.averageEnemiesKilled,
                otherPlayerStats.averageKnockBacks
                 // Thêm các chỉ số khác vào đây, đảm bảo thứ tự khớp với vector người chơi hiện tại
            };

            // Chuẩn hóa vector người chơi khác
            float[] normalizedOtherPlayerVector = NormalizeVector(otherPlayerVector);

            // Tính Cosine Similarity
            float suitabilityScore = CalculateCosineSimilarity(normalizedCurrentUserVector, normalizedOtherPlayerVector);

            // Tạo đối tượng RecommendedPlayerInfo
            RecommendedPlayerInfo recommendedPlayer = new RecommendedPlayerInfo
            {
                userId = otherPlayerStats.id,
                userName = otherPlayerStats.userName, // Giả định userName có trong PlayerStatsForRecommendation
                // Lấy trạng thái online từ kết quả tải trạng thái
                isOnline = onlineStatus.ContainsKey(otherPlayerStats.id) ? onlineStatus[otherPlayerStats.id] : false, // Mặc định offline nếu không tìm thấy
                status = (onlineStatus.ContainsKey(otherPlayerStats.id) && onlineStatus[otherPlayerStats.id]) ? "Online" : "Offline", // Đặt text trạng thái
                suitabilityScore = suitabilityScore // Lưu điểm phù hợp
            };

            recommendedPlayers.Add(recommendedPlayer);
        }

        // 5. Sắp xếp danh sách theo điểm phù hợp giảm dần
        recommendedPlayers = recommendedPlayers.OrderByDescending(p => p.suitabilityScore).ToList();

        // **Thêm bước lọc cuối cùng để đảm bảo loại bỏ người chơi hiện tại**
        if (!string.IsNullOrEmpty(currentUserId))
        {
            recommendedPlayers = recommendedPlayers.Where(p => p.userId != currentUserId).ToList();
            Debug.Log($"Filtered out current user {currentUserId} from recommended list. List count: {recommendedPlayers.Count}");
        }

        // Trả về danh sách đã được sắp xếp dưới dạng List<RecommendedPlayerData>
        return recommendedPlayers.Cast<RecommendedPlayerData>().ToList();
    }

    // Hàm để lấy ID người chơi hiện tại
    // Đặt public để có thể gọi từ script khác
    public string GetCurrentUserId()
    {
        // Lấy từ Authentication.CurrentUser
        // Authentication.CurrentUser là static, truy cập trực tiếp qua tên lớp
        if (Authentication.CurrentUser != null)
        {
            return Authentication.CurrentUser.id;
        }
        Debug.LogError("PlayersRecommendationManager: Cannot get current user ID. Authentication.CurrentUser is null.");
        return null; // Trả về null nếu không lấy được ID
    }
}