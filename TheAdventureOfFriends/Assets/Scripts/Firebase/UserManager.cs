using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class User
{
    public string userName;
    public float userBestTime;
    public int userBestFruitCollected;
    public string localId;

    public User()
    {
        userName = PlayerStats.playerName;
        userBestTime = PlayerStats.playerScore;
        userBestFruitCollected = PlayerStats.playerScore;
        localId = PlayerStats.localId;
    }
}