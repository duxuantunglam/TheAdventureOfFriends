using UnityEngine;

public class PlayerPrefsExample : MonoBehaviour
{
    public int fruit;
    public float seconds;
    public string levelName;

    [ContextMenu("Save value")]
    public void SaveValue()
    {
        PlayerPrefs.SetInt("Level1Unlocked", 1);
    }

    [ContextMenu("Load value")]
    public void LoadValue()
    {
        bool levelUnlocked = PlayerPrefs.GetInt("Level1Unlocked", 0) == 1;

        if (levelUnlocked)
            Debug.Log("Level is unlocked");
    }
}