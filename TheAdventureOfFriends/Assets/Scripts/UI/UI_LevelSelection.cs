using UnityEngine;
using UnityEngine.SceneManagement;

public class UI_LevelSelection : MonoBehaviour
{
    [SerializeField] private UI_LevelButton buttonPrefab;
    [SerializeField] private Transform buttonsParent;

    [SerializeField] private bool[] levelsUnlocked;

    private void Start()
    {
        LoadLevelsInfo();
        CreateLevelButtons();
    }

    private void CreateLevelButtons()
    {
        int lastLevelIndex = SceneManager.sceneCountInBuildSettings - 3;
        lastLevelIndex = Mathf.Max(1, lastLevelIndex);

        for (int i = 1; i <= lastLevelIndex; i++)
        {
            if (IsLevelUnlocked(i) == false)
                return;

            UI_LevelButton newButton = Instantiate(buttonPrefab, buttonsParent);
            newButton.SetupButton(i);
        }
    }

    private bool IsLevelUnlocked(int levelIndex) => levelsUnlocked[levelIndex];

    private void LoadLevelsInfo()
    {
        int lastLevelIndex = SceneManager.sceneCountInBuildSettings - 3;

        lastLevelIndex = Mathf.Max(1, lastLevelIndex);

        levelsUnlocked = new bool[lastLevelIndex + 1];

        levelsUnlocked[1] = true;

        if (Authentication.CurrentUser == null || Authentication.CurrentUser.levelProgress == null)
        {
            Debug.LogWarning("Authentication.CurrentUser or levelProgress is null. Loading only Level 1.");
            return;
        }

        for (int i = 2; i <= lastLevelIndex; i++)
        {
            string levelKey = "Level" + i;

            bool levelUnlockedFromFirebase = Authentication.CurrentUser.levelProgress.ContainsKey(levelKey) && Authentication.CurrentUser.levelProgress[levelKey].unlocked;

            if (levelUnlockedFromFirebase)
            {
                levelsUnlocked[i] = true;
            }
            else
            {
                levelsUnlocked[i] = false;
            }
        }
    }
}