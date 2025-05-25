using TMPro;
using UnityEngine;

public class UI_HighScores : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI fruitTexts;
    [SerializeField] private TextMeshProUGUI averageTime;
    [SerializeField] private TextMeshProUGUI enemiesKilled;
    [SerializeField] private TextMeshProUGUI knockBacks;

    private void Start()
    {
        SetupHighScores();
    }

    public void SetupHighScores()
    {
        fruitTexts.text = "Fruit: " + TotalFruitCollected().ToString("00");
        averageTime.text = "Average Time: " + AverageTime().ToString("00");
        enemiesKilled.text = "Enemies Killed: " + TotalEnemiesKilled().ToString("00");
        knockBacks.text = "KnockBacks: " + TotalKnockBacks().ToString("00");
    }

    private int TotalFruitCollected()
    {
        if (Authentication.CurrentUser == null)
        {
            Debug.LogWarning("Authentication.CurrentUser is null. Cannot get total fruits.");
            return 0;
        }

        return Authentication.CurrentUser.totalFruitAmount;
    }

    private float AverageTime()
    {
        if (Authentication.CurrentUser == null)
        {
            Debug.LogWarning("Authentication.CurrentUser is null. Cannot get average time.");
            return 0f;
        }
        return Authentication.CurrentUser.averageTime;
    }

    private int TotalEnemiesKilled()
    {
        if (Authentication.CurrentUser == null)
        {
            Debug.LogWarning("Authentication.CurrentUser is null. Cannot get total enemies killed.");
            return 0;
        }
        return Authentication.CurrentUser.enemiesKilled;
    }

    private int TotalKnockBacks()
    {
        if (Authentication.CurrentUser == null)
        {
            Debug.LogWarning("Authentication.CurrentUser is null. Cannot get total knockBacks.");
            return 0;
        }
        return Authentication.CurrentUser.knockBacks;
    }
}