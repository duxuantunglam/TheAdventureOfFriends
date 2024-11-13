using UnityEngine;
using UnityEngine.SceneManagement;

public class UI_MainMenu : MonoBehaviour
{
    private UI_FadeEffect fadeEffect;
    // public string FirstLevelName;
    public string sceneName;


    [SerializeField] private GameObject[] uiElements;

    // [SerializeField] private GameObject continueButton;

    private void Awake()
    {
        fadeEffect = GetComponentInChildren<UI_FadeEffect>();
    }

    private void Start()
    {
        //     if (HasLevelProgression())
        //         continueButton.SetActive(true);

        fadeEffect.ScreenFade(0, 1.5f);
    }

    public void SwitchUI(GameObject uiToEnable)
    {
        foreach (GameObject ui in uiElements)
        {
            ui.SetActive(false);
        }

        uiToEnable.SetActive(true);
    }

    public void NewGame()
    {
        fadeEffect.ScreenFade(1, 1.5f, LoadLevelScene);
    }

    // private void LoadLevelScene() => SceneManager.LoadScene(FirstLevelName);
    private void LoadLevelScene() => SceneManager.LoadScene(sceneName);

    // private bool HasLevelProgression()
    // {
    //     bool hasLevelProgression = PlayerPrefs.GetInt("ContinueLevelNumber", 0) > 0;

    //     return hasLevelProgression;
    // }

    // public void ContinueGame()
    // {
    //     int difficultyIndex =  PlayerPrefs.GetInt("GameDifficulty",1);
    //     int levelToLoad = PlayerPrefs.GetInt("ContinueLevelNumber", 0);

    //     DifficultyManager.instance.LoadDifficulty(difficultyIndex);
    //     SceneManager.LoadScene("Level_" + levelToLoad);
    // }
}