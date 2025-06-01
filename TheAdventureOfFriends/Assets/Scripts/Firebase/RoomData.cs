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
    public string currentScene;
    public Dictionary<string, bool> playersInScene = new Dictionary<string, bool>();
}