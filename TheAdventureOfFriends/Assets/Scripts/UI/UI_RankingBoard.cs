using System.Collections.Generic;
using System.Linq;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

public class UI_RankingBoard : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject rankingPanel;
    public Button fruitButton;
    public Button averageTimeButton;
    public Button enemiesKilledButton;
    public Button knockBackButton;
    public Button compatibleFilteringButton;

    [Header("Ranking List")]
    public Transform contentTransform;
    public GameObject rankingItemPrefab;

    private DatabaseReference dbReference;

    public enum RankingCriteria { Fruit, AverageTime, EnemiesKilled, KnockBack, CompatibleFiltering }
    private RankingCriteria currentCriteria = RankingCriteria.Fruit;

    private RankingCriteria compatibilitySubCriteria = RankingCriteria.Fruit;

    private List<UserData> allPlayersData = new List<UserData>();

    private void Awake()
    {
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;

        fruitButton.onClick.AddListener(() => SetCriteriaAndLoadRanking(RankingCriteria.Fruit));
        averageTimeButton.onClick.AddListener(() => SetCriteriaAndLoadRanking(RankingCriteria.AverageTime));
        enemiesKilledButton.onClick.AddListener(() => SetCriteriaAndLoadRanking(RankingCriteria.EnemiesKilled));
        knockBackButton.onClick.AddListener(() => SetCriteriaAndLoadRanking(RankingCriteria.KnockBack));
        compatibleFilteringButton.onClick.AddListener(() => SetCriteriaAndLoadRanking(RankingCriteria.CompatibleFiltering));
    }

    private void OnDestroy()
    {
        if (fruitButton != null) fruitButton.onClick.RemoveAllListeners();
        if (averageTimeButton != null) averageTimeButton.onClick.RemoveAllListeners();
        if (enemiesKilledButton != null) enemiesKilledButton.onClick.RemoveAllListeners();
        if (knockBackButton != null) knockBackButton.onClick.RemoveAllListeners();

        if (compatibleFilteringButton != null) compatibleFilteringButton.onClick.RemoveAllListeners();
    }

    public void ShowRanking()
    {
        rankingPanel.SetActive(true);
        SetCriteriaAndLoadRanking(currentCriteria);
    }

    public void HideRanking()
    {
        rankingPanel.SetActive(false);
        ClearRankingList();
    }

    public void SetCriteriaAndLoadRanking(RankingCriteria criteria)
    {
        currentCriteria = criteria;
        ClearRankingList();

        if (criteria != RankingCriteria.CompatibleFiltering)
        {
            compatibilitySubCriteria = criteria;
        }

        if (allPlayersData != null && allPlayersData.Count > 0)
        {
            Debug.Log($"Sorting and displaying ranking data for criteria: {criteria} (data already loaded)");
            List<UserData> playersToDisplay = new List<UserData>(allPlayersData);
            SortPlayers(playersToDisplay, criteria);
            DisplayPlayersList(playersToDisplay, criteria);
        }
        else
        {
            LoadRankingDataAndDisplay(criteria);
        }
    }

    private void LoadRankingDataAndDisplay(RankingCriteria criteria)
    {
        if (allPlayersData == null || allPlayersData.Count == 0)
        {
            Debug.Log($"Loading ranking data from Firebase for criteria: {criteria}");

            dbReference.Child("PlayerStats").GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("Failed to load ranking data: " + task.Exception);
                    return;
                }

                if (task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;
                    allPlayersData = new List<UserData>();

                    foreach (var userSnapshot in snapshot.Children)
                    {
                        string json = userSnapshot.GetRawJsonValue();
                        UserData userData = JsonConvert.DeserializeObject<UserData>(json);
                        allPlayersData.Add(userData);
                    }

                    List<UserData> playersToDisplay = new List<UserData>(allPlayersData);
                    SortPlayers(playersToDisplay, criteria);
                    DisplayPlayersList(playersToDisplay, criteria);
                }
            });
        }
        else
        {
            Debug.Log("Data already loaded.");
            List<UserData> playersToDisplay = new List<UserData>(allPlayersData);
            SortPlayers(playersToDisplay, criteria);
            DisplayPlayersList(playersToDisplay, criteria);
        }
    }

    private void SortPlayers(List<UserData> players, RankingCriteria criteria)
    {
        switch (criteria)
        {
            case RankingCriteria.Fruit:
                players.Sort((p1, p2) => p2.totalFruitAmount.CompareTo(p1.totalFruitAmount));
                break;
            case RankingCriteria.AverageTime:
                players.Sort((p1, p2) =>
                {
                    float avgTime1 = p1.completedLevelCount > 0 ? p1.averageTime : float.MaxValue;
                    float avgTime2 = p2.completedLevelCount > 0 ? p2.averageTime : float.MaxValue;
                    return avgTime1.CompareTo(avgTime2);
                });
                break;
            case RankingCriteria.EnemiesKilled:
                players.Sort((p1, p2) => p2.enemiesKilled.CompareTo(p1.enemiesKilled));
                break;
            case RankingCriteria.KnockBack:
                players.Sort((p1, p2) => p2.knockBacks.CompareTo(p1.knockBacks));
                break;
            case RankingCriteria.CompatibleFiltering:
                // TODO: 1. Get the data of the currently logged-in user.
                UserData currentUserData = GetCurrentUserData();

                if (currentUserData != null)
                {
                    List<UserData> playersToCompare = players.Where(player => player.userName != currentUserData.userName).ToList();

                    playersToCompare.Sort((p1, p2) =>
                    {
                        // TODO: Calculate compatibility score between p1 and currentUserData
                        float compatibilityScore1 = CalculateCompatibility(p1, currentUserData, compatibilitySubCriteria);
                        float compatibilityScore2 = CalculateCompatibility(p2, currentUserData, compatibilitySubCriteria);

                        return compatibilityScore2.CompareTo(compatibilityScore1);
                    });

                    players.Clear();
                    players.AddRange(playersToCompare);
                }
                else
                {
                    Debug.LogWarning("Current user data not available or found for compatibility ranking.");
                    players.Clear();
                }
                break;
        }
    }

    private void DisplayPlayersList(List<UserData> players, RankingCriteria criteria)
    {
        if (rankingItemPrefab == null || contentTransform == null)
        {
            Debug.LogError("RankingItemPrefab or ContentTransform is not assigned!");
            return;
        }

        int maxItemsToDisplay = players.Count;

        if (criteria == RankingCriteria.CompatibleFiltering)
        {
            maxItemsToDisplay = Mathf.Min(players.Count, 5);
        }

        for (int i = 0; i < maxItemsToDisplay; i++)
        {
            if (i >= players.Count) break;

            GameObject rankingItemGO = Instantiate(rankingItemPrefab, contentTransform);
            UI_PlayersRanking rankingItemScript = rankingItemGO.GetComponent<UI_PlayersRanking>();

            if (rankingItemScript != null)
            {
                UserData player = players[i];
                int rank = i + 1;
                string playerName = player.userName;
                string score;

                switch (criteria)
                {
                    case RankingCriteria.Fruit:
                        score = player.totalFruitAmount.ToString();
                        break;
                    case RankingCriteria.AverageTime:
                        score = player.completedLevelCount > 0 ? player.averageTime.ToString("f2") : "N/A";
                        break;
                    case RankingCriteria.EnemiesKilled:
                        score = player.enemiesKilled.ToString();
                        break;
                    case RankingCriteria.KnockBack:
                        score = player.knockBacks.ToString();
                        break;
                    case RankingCriteria.CompatibleFiltering:
                        UserData currentUserDisplayCheck = GetCurrentUserData();
                        if (currentUserDisplayCheck != null && player.userName != currentUserDisplayCheck.userName)
                        {
                            switch (compatibilitySubCriteria)
                            {
                                case RankingCriteria.Fruit:
                                    score = player.totalFruitAmount.ToString();
                                    break;
                                case RankingCriteria.AverageTime:
                                    score = player.completedLevelCount > 0 ? player.averageTime.ToString("f2") : "N/A";
                                    break;
                                case RankingCriteria.EnemiesKilled:
                                    score = player.enemiesKilled.ToString();
                                    break;
                                case RankingCriteria.KnockBack:
                                    score = player.knockBacks.ToString();
                                    break;
                                default:
                                    score = "N/A";
                                    break;
                            }
                        }
                        else
                        {
                            score = "N/A - Current User";
                        }
                        break;
                    default:
                        score = "N/A";
                        break;
                }

                rankingItemScript.SetRankingData(rank, playerName, score);
            }

            else
            {
                Debug.LogWarning("Ranking item prefab is missing UI_PlayersRanking script!");
            }
        }
    }

    private void ClearRankingList()
    {
        if (contentTransform == null) return;

        foreach (Transform child in contentTransform)
        {
            Destroy(child.gameObject);
        }
    }

    public void OnFruitButtonClick()
    {
        SetCriteriaAndLoadRanking(RankingCriteria.Fruit);
    }

    public void OnAverageTimeButtonClick()
    {
        SetCriteriaAndLoadRanking(RankingCriteria.AverageTime);
    }
    public void OnEnemiesKilledButtonClick()
    {
        SetCriteriaAndLoadRanking(RankingCriteria.EnemiesKilled);
    }

    public void OnKnockBackButtonClick()
    {
        SetCriteriaAndLoadRanking(RankingCriteria.KnockBack);
    }

    private UserData GetCurrentUserData()
    {
        if (FirebaseManager.CurrentUser != null)
        {
            return FirebaseManager.CurrentUser;
        }
        else
        {
            Debug.LogWarning("GetCurrentUserData: FirebaseManager.CurrentUser is null. No user logged in or data not loaded.");
            return null;
        }
    }

    private float CalculateCompatibility(UserData player1, UserData player2, RankingCriteria subCriteria)
    {
        // TODO: Implement your actual compatibility calculation logic here.
        float compatibilityScore = 0;

        switch (subCriteria)
        {
            case RankingCriteria.Fruit:
                compatibilityScore = 1.0f / (1.0f + Mathf.Abs(player1.totalFruitAmount - player2.totalFruitAmount));
                break;
            case RankingCriteria.AverageTime:
                compatibilityScore = 1.0f / (1.0f + Mathf.Abs(player1.averageTime - player2.averageTime));
                break;
            case RankingCriteria.EnemiesKilled:
                compatibilityScore = 1.0f / (1.0f + Mathf.Abs(player1.enemiesKilled - player2.enemiesKilled));
                break;
            case RankingCriteria.KnockBack:
                compatibilityScore = 1.0f / (1.0f + Mathf.Abs(player1.knockBacks - player2.knockBacks));
                break;
            default:
                compatibilityScore = 0;
                break;
        }

        return compatibilityScore;
    }
}