using System;
using System.Collections.Generic;

[Serializable]
public class RoomPlayerData
{
    public string userName;
    public bool isReady;
}

[Serializable]
public class RoomData
{
    public Dictionary<string, RoomPlayerData> players = new Dictionary<string, RoomPlayerData>();
    public string status;
    public string gameSceneName;
    public long createdAt;
    public long lastActivity;
}


[Serializable]
public class GameRoomData
{
    public string roomId;
    public Dictionary<string, PlayerGameData> players = new Dictionary<string, PlayerGameData>();
    public string gameStatus;
    public long gameStartTime;
    public long lastUpdateTime;

    public GameRoomData()
    {
        gameStartTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        lastUpdateTime = gameStartTime;
        gameStatus = "playing";
    }
}

[Serializable]
public class PlayerGameData
{
    public string playerId;
    public string playerName;
    public PlayerPositionData position;
    public PlayerInputData input;
    public PlayerAnimationData animation;
    public PlayerGameStats stats;
    public bool isConnected;
    public long lastSeen;

    public PlayerGameData()
    {
        isConnected = true;
        lastSeen = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        stats = new PlayerGameStats();
    }
}

[Serializable]
public class PlayerGameStats
{
    public int score;
    public int fruitsCollected;
    public int enemiesKilled;
    public int deaths;
    public float timeAlive;

    public PlayerGameStats()
    {
        score = 0;
        fruitsCollected = 0;
        enemiesKilled = 0;
        deaths = 0;
        timeAlive = 0f;
    }
}