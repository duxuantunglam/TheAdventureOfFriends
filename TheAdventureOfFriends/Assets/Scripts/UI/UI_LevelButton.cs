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
            return "Fruit: ? / ?";
        }

        int fruitCollected = FirebaseManager.CurrentUser.levelProgress[sceneName].bestFruitCollected;

        return "Fruit: " + fruitCollected + " / ?";
    }

    private string TimerInfoText()
    {
        if (FirebaseManager.CurrentUser == null || !FirebaseManager.CurrentUser.levelProgress.ContainsKey(sceneName))
        {
            return "Best Time: ?";
        }

        float timerValue = FirebaseManager.CurrentUser.levelProgress[sceneName].bestTime;

        if (timerValue >= 999f)
        {
            return "Best Time: ?";
        }

        return "Best Time: " + timerValue.ToString("00:00");
    }
}