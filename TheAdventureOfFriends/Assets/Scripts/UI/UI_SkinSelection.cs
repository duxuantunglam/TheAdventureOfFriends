using TMPro;
using UnityEngine;

[System.Serializable]
public struct Skin
{
    public int skinPrice;
    public bool unlocked;
}

public class UI_SkinSelection : MonoBehaviour
{
    private UI_LevelSelection levelSelectionUI;
    private UI_MainMenu mainMenuUI;
    [SerializeField] private Skin[] skinList;

    [Header("UI details")]
    [SerializeField] private int skinIndex;
    [SerializeField] private int maxIndex;
    [SerializeField] private Animator skinDisplay;

    [SerializeField] private TextMeshProUGUI buySelectText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private TextMeshProUGUI bankText;

    private void Start()
    {
        LoadSkinUnlocks();
        UpdateSkinDisplay();

        mainMenuUI = GetComponentInParent<UI_MainMenu>();
        levelSelectionUI = mainMenuUI.GetComponentInChildren<UI_LevelSelection>(true);
    }

    private void LoadSkinUnlocks()
    {
        if (FirebaseManager.CurrentUser == null || FirebaseManager.CurrentUser.skinUnlockedName == null)
        {
            Debug.LogWarning("FirebaseManager.CurrentUser or skinUnlockedName is null. Cannot load skin unlocks.");
            for (int i = 0; i < skinList.Length; i++)
            {
                skinList[i].unlocked = (i == 0);
            }
            return;
        }

        for (int i = 0; i < skinList.Length; i++)
        {
            bool skinUnlockedFromFirebase = FirebaseManager.CurrentUser.skinUnlockedName.ContainsKey(i.ToString()) && FirebaseManager.CurrentUser.skinUnlockedName[i.ToString()];

            if (skinUnlockedFromFirebase || i == 0)
                skinList[i].unlocked = true;
            else
                skinList[i].unlocked = false;
        }
    }

    public void SelectSkin()
    {
        if (skinList[skinIndex].unlocked == false)
            BuySkin(skinIndex);
        else
        {
            SkinManager.instance.SetSkinId(skinIndex);
            mainMenuUI.SwitchUI(levelSelectionUI.gameObject);
        }

        AudioManager.instance.PlaySFX(4);

        UpdateSkinDisplay();
    }

    public void NextSkin()
    {
        skinIndex++;

        if (skinIndex > maxIndex)
            skinIndex = 0;

        AudioManager.instance.PlaySFX(4);

        UpdateSkinDisplay();
    }

    public void PreviousSkin()
    {
        skinIndex--;

        if (skinIndex < 0)
            skinIndex = maxIndex;

        AudioManager.instance.PlaySFX(4);

        UpdateSkinDisplay();
    }

    private void UpdateSkinDisplay()
    {
        bankText.text = "Bank: " + FruitInBank();

        for (int i = 0; i < skinDisplay.layerCount; i++)
        {
            skinDisplay.SetLayerWeight(i, 0);
        }

        skinDisplay.SetLayerWeight(skinIndex, 1);

        if (skinList[skinIndex].unlocked)
        {
            priceText.transform.parent.gameObject.SetActive(false);
            buySelectText.text = "Select";
        }
        else
        {
            priceText.transform.parent.gameObject.SetActive(true);
            priceText.text = "Price: " + skinList[skinIndex].skinPrice;
            buySelectText.text = "Buy";
        }

    }

    private void BuySkin(int index)
    {
        if (HaveEnoughFruit(skinList[index].skinPrice) == false)
        {
            AudioManager.instance.PlaySFX(6);
            Debug.Log("Not enough fruit");
            return;
        }

        AudioManager.instance.PlaySFX(10);
        skinList[index].unlocked = true;

        if (FirebaseManager.CurrentUser != null)
        {
            string skinKey = index.ToString();

            if (!FirebaseManager.CurrentUser.skinUnlockedName.ContainsKey(skinKey))
            {
                FirebaseManager.CurrentUser.skinUnlockedName.Add(skinKey, true);
            }
            else
            {
                FirebaseManager.CurrentUser.skinUnlockedName[skinKey] = true;
            }

            if (FirebaseManager.instance != null)
            {
                FirebaseManager.instance.SaveUserDataToRealtimeDatabase();
            }
        }
    }

    private int FruitInBank()
    {
        if (FirebaseManager.CurrentUser == null)
        {
            Debug.LogWarning("FirebaseManager.CurrentUser is null. Cannot get fruit in bank.");
            return 0;
        }

        return FirebaseManager.CurrentUser.totalFruitAmount;
    }

    private bool HaveEnoughFruit(int price)
    {
        if (FirebaseManager.CurrentUser == null)
        {
            Debug.LogWarning("FirebaseManager.CurrentUser is null. Cannot check/subtract fruit.");
            return false;
        }

        if (FirebaseManager.CurrentUser.totalFruitAmount >= price)
        {
            FirebaseManager.CurrentUser.totalFruitAmount -= price;

            if (FirebaseManager.instance != null)
            {
                FirebaseManager.instance.SaveUserDataToRealtimeDatabase();
            }
            else
            {
                Debug.LogError("Authentication instance is null. Cannot save fruit amount to Firebase.");
            }

            return true;
        }
        return false;
    }
}