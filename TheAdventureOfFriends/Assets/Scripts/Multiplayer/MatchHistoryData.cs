using System;
using System.Collections.Generic;
using Firebase;

[Serializable]
public class MatchHistoryData
{
    public GameStatsData gameStats;
    public RatingsData ratings;
    public MatchInfoData matchInfo;

    public MatchHistoryData()
    {
        gameStats = new GameStatsData();
        ratings = new RatingsData();
        matchInfo = new MatchInfoData();
    }
}

[Serializable]
public class GameStatsData
{
    public PlayerStatsData player1;
    public PlayerStatsData player2;
    public string gameStatus;
    public string winnerId;
    public string winnerName;

    public GameStatsData()
    {
        player1 = new PlayerStatsData();
        player2 = new PlayerStatsData();
        gameStatus = "";
        winnerId = "";
        winnerName = "";
    }

    public GameStatsData(MultiplayerGameStats source)
    {
        player1 = new PlayerStatsData(source.player1);
        player2 = new PlayerStatsData(source.player2);
        gameStatus = source.gameStatus;
        winnerId = source.winnerId;
        winnerName = source.winnerName;
    }
}

[Serializable]
public class PlayerStatsData
{
    public string playerId;
    public string playerName;
    public int fruitCollected;
    public float completionTime;
    public int enemiesKilled;
    public int knockBacks;
    public float totalScore;
    public bool hasFinished;

    public PlayerStatsData()
    {
        playerId = "";
        playerName = "";
        fruitCollected = 0;
        completionTime = 0f;
        enemiesKilled = 0;
        knockBacks = 0;
        totalScore = 0f;
        hasFinished = false;
    }

    public PlayerStatsData(MultiplayerPlayerStats source)
    {
        playerId = source.playerId;
        playerName = source.playerName;
        fruitCollected = source.fruitCollected;
        completionTime = source.completionTime;
        enemiesKilled = source.enemiesKilled;
        knockBacks = source.knockBacks;
        totalScore = source.totalScore;
        hasFinished = source.hasFinished;
    }
}

[Serializable]
public class RatingsData
{
    public string player1RatesPlayer2;
    public string player2RatesPlayer1;

    public RatingsData()
    {
        player1RatesPlayer2 = "";
        player2RatesPlayer1 = "";
    }
}

[Serializable]
public class MatchInfoData
{
    public string roomId;
    public long matchDate;
    public List<PlayerInfoData> players;

    public MatchInfoData()
    {
        roomId = "";
        matchDate = 0;
        players = new List<PlayerInfoData>();
    }
}

[Serializable]
public class PlayerInfoData
{
    public string playerId;
    public string playerName;

    public PlayerInfoData()
    {
        playerId = "";
        playerName = "";
    }

    public PlayerInfoData(string id, string name)
    {
        playerId = id;
        playerName = name;
    }
}