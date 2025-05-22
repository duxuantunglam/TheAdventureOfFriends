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
        enemiesKilled.text = "Enemies Killed: ";
        knockBacks.text = "KnockBacks: ";
    }

    private int TotalFruitCollected() => PlayerPrefs.GetInt("TotalFruitAmount", 0);

    private float AverageTime() => PlayerPrefs.GetFloat("AverageTime", 0);
}
