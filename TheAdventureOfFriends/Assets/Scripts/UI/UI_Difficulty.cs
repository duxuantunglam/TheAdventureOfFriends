using UnityEngine;

public class UI_Difficulty : MonoBehaviour
{
    private DifficultyManager difficultyManager;

    private void Start()
    {
        difficultyManager = DifficultyManager.instance;
    }

    public void SetEasyMode()
    {
        difficultyManager.SetDifficulty(DifficultyType.Easy);
        if (Authentication.CurrentUser != null)
            Authentication.CurrentUser.gameProgress.gameDifficulty = (int)DifficultyType.Easy;
    }

    public void SetNormalMode()
    {
        difficultyManager.SetDifficulty(DifficultyType.Normal);
        if (Authentication.CurrentUser != null)
            Authentication.CurrentUser.gameProgress.gameDifficulty = (int)DifficultyType.Normal;
    }

    public void SetHardMode()
    {
        difficultyManager.SetDifficulty(DifficultyType.Hard);
        if (Authentication.CurrentUser != null)
            Authentication.CurrentUser.gameProgress.gameDifficulty = (int)DifficultyType.Hard;
    }
}