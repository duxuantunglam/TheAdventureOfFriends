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
        if (FirebaseManager.CurrentUser != null)
            FirebaseManager.CurrentUser.gameProgress.gameDifficulty = (int)DifficultyType.Easy;
    }

    public void SetNormalMode()
    {
        difficultyManager.SetDifficulty(DifficultyType.Normal);
        if (FirebaseManager.CurrentUser != null)
            FirebaseManager.CurrentUser.gameProgress.gameDifficulty = (int)DifficultyType.Normal;
    }

    public void SetHardMode()
    {
        difficultyManager.SetDifficulty(DifficultyType.Hard);
        if (FirebaseManager.CurrentUser != null)
            FirebaseManager.CurrentUser.gameProgress.gameDifficulty = (int)DifficultyType.Hard;
    }
}