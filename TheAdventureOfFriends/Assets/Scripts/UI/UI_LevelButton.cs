using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UI_LevelButton : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI levelNumberText;

    [SerializeField] private TextMeshProUGUI bestTimeText;
    [SerializeField] private TextMeshProUGUI fruitText;

    private int levelIndex;
    private string sceneName;

    public void SetupButton(int newLevelIndex)
    {
        levelIndex = newLevelIndex;

        levelNumberText.text = "Level " + levelIndex;
        sceneName = "Level_" + levelIndex;

        bestTimeText.text = TimerInfoText();
        fruitText.text = FruitInfoText();
    }

    public void LoadLevel()
    {
        AudioManager.instance.PlaySFX(4);

        int difficultyIndex = ((int)DifficultyManager.instance.difficulty);
        PlayerPrefs.SetInt("GameDifficulty", difficultyIndex);
        SceneManager.LoadScene(sceneName);
    }

    private string FruitInfoText()
    {
        int totalFruit = PlayerPrefs.GetInt("Level" + levelIndex + "TotalFruit", 0);
        string totalFruitText = totalFruit == 0 ? "?" : totalFruit.ToString();

        int fruitCollected = PlayerPrefs.GetInt("Level" + levelIndex + "FruitCollected");

        return "Fruit: " + fruitCollected + " / " + totalFruitText;

    }

    private string TimerInfoText()
    {
        float timerValue = PlayerPrefs.GetFloat("Level" + levelIndex + "BestTime", 99);

        return "Best Time: " + timerValue.ToString("00");
    }
}