using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Multiplayer_InGameUI : MonoBehaviour
{
    public static Multiplayer_InGameUI instance;
    public UI_FadeEffect fadeEffect { get; private set; }

    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI fruitText;

    private void Awake()
    {
        instance = this;

        fadeEffect = GetComponentInChildren<UI_FadeEffect>();
    }

    private void Start()
    {
        fadeEffect.ScreenFade(0, 1);
    }
    public void UpdateMultiplayerFruitUI(int collectedFruit, int totalFruit)
    {
        fruitText.text = collectedFruit + "/" + totalFruit;
    }

    public void UpdateMultiplayerTimerUI(float timer)
    {
        timerText.text = timer.ToString("00") + " s";
    }
}