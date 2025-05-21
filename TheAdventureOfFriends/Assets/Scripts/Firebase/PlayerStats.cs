using Proyecto26;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    private void PostToDatabase()
    {
        var data = new
        {
            bestTime = GameManager.instance.bestTimeStat,
            bestFruitCollected = GameManager.instance.bestFruitCollectedStat,
        };

        RestClient.Post(new RequestHelper
        {
            Uri = "",
            Body = data
        });
    }
}