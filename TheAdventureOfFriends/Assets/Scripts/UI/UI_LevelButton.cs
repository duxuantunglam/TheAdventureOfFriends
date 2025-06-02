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

        RefreshButtonData();
    }

    public void RefreshButtonData()
    {
        bestTimeText.text = TimerInfoText();
        fruitText.text = FruitInfoText();
    }

    public void LoadLevel()
    {
        AudioManager.instance.PlaySFX(4);

        if (FirebaseManager.CurrentUser == null)
        {
            Debug.LogWarning("Cannot load level: CurrentUser is null.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    private string FruitInfoText()
    {
        if (FirebaseManager.CurrentUser == null || !FirebaseManager.CurrentUser.levelProgress.ContainsKey(sceneName))
        {
            return "Fruit: 0 / ?";
        }

        LevelStats levelStats = FirebaseManager.CurrentUser.levelProgress[sceneName];
        int fruitCollected = levelStats.bestFruitCollected;
        int totalFruits = levelStats.totalFruitsInLevel;

        if (totalFruits == 0)
        {
            if (fruitCollected == 0)
                return "Fruit: 0 / ?";
            else
                return "Fruit: " + fruitCollected + " / ?";
        }

        return "Fruit: " + fruitCollected + " / " + totalFruits;
    }

    private string TimerInfoText()
    {
        if (FirebaseManager.CurrentUser == null || !FirebaseManager.CurrentUser.levelProgress.ContainsKey(sceneName))
        {
            return "Best Time: ?";
        }

        float timerValue = FirebaseManager.CurrentUser.levelProgress[sceneName].bestTime;

        int minutes = Mathf.FloorToInt(timerValue / 60f);
        int seconds = Mathf.FloorToInt(timerValue % 60f);
        return "Best Time: " + minutes.ToString("00") + ":" + seconds.ToString("00");
    }
}