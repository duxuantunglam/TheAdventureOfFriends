using Firebase.Auth;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UI_MainMenu : MonoBehaviour
{
    private UI_FadeEffect fadeEffect;
    public string FirstLevelName;

    [SerializeField] private GameObject[] uiElements;

    [SerializeField] private GameObject continueButton;

    [Header("Interactive Camera")]
    [SerializeField] private MenuCharacter menuCharacter;
    [SerializeField] private CinemachineCamera cinemachine;
    [SerializeField] private Transform mainMenuPoint;
    [SerializeField] private Transform skinSelectionPoint;

    private void Awake()
    {
        fadeEffect = GetComponentInChildren<UI_FadeEffect>();
    }

    private void Start()
    {
        if (HasLevelProgression())
            continueButton.SetActive(true);

        fadeEffect.ScreenFade(0, 1.5f);
    }

    public void BackToAuthentication()
    {
        SceneManager.LoadScene("Authentication");
    }

    public void SwitchUI(GameObject uiToEnable)
    {
        foreach (GameObject ui in uiElements)
        {
            ui.SetActive(false);
        }

        uiToEnable.SetActive(true);

        AudioManager.instance.PlaySFX(4);
    }

    public void NewGame()
    {
        fadeEffect.ScreenFade(1, 1.5f, LoadLevelScene);

        AudioManager.instance.PlaySFX(4);
    }

    private void LoadLevelScene() => SceneManager.LoadScene(FirstLevelName);

    private bool HasLevelProgression()
    {
        bool hasLevelProgression = Authentication.CurrentUser != null && Authentication.CurrentUser.continueLevelNumber > 0;

        return hasLevelProgression;
    }

    public void ContinueGame()
    {
        if (Authentication.CurrentUser == null)
        {
            Debug.LogWarning("Cannot continue game: CurrentUser is null.");
            return;
        }

        int levelToLoad = Authentication.CurrentUser.continueLevelNumber;
        int lastSavedSkin = Authentication.CurrentUser.lastUsedSkin;

        SkinManager.instance.SetSkinId(lastSavedSkin);

        SceneManager.LoadScene("Level_" + levelToLoad);

        AudioManager.instance.PlaySFX(4);
    }

    public void MoveCameraToMainMenu()
    {
        menuCharacter.MoveTo(mainMenuPoint);
        cinemachine.Follow = mainMenuPoint;
    }

    public void MoveCameraToSkinMenu()
    {
        menuCharacter.MoveTo(skinSelectionPoint);
        cinemachine.Follow = skinSelectionPoint;
    }
}