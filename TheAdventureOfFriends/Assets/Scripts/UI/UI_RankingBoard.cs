using System.Collections.Generic;
using System.Linq;
using Firebase.Database;
using Firebase.Extensions;
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

    [Header("Ranking List")]
    public Transform contentTransform;
    public GameObject rankingItemPrefab;

    private DatabaseReference dbReference;

    public enum RankingCriteria { Fruit, AverageTime, EnemiesKilled, KnockBack }
    private RankingCriteria currentCriteria = RankingCriteria.Fruit;

    private List<UserData> allPlayersData = new List<UserData>();

    private void Awake()
    {
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;

        fruitButton.onClick.AddListener(() => SetCriteriaAndLoadRanking(RankingCriteria.Fruit));
        averageTimeButton.onClick.AddListener(() => SetCriteriaAndLoadRanking(RankingCriteria.AverageTime));
        enemiesKilledButton.onClick.AddListener(() => SetCriteriaAndLoadRanking(RankingCriteria.EnemiesKilled));
        knockBackButton.onClick.AddListener(() => SetCriteriaAndLoadRanking(RankingCriteria.KnockBack));
    }

    private void OnDestroy()
    {
        if (fruitButton != null) fruitButton.onClick.RemoveAllListeners();
        if (averageTimeButton != null) averageTimeButton.onClick.RemoveAllListeners();
        if (enemiesKilledButton != null) enemiesKilledButton.onClick.RemoveAllListeners();
        if (knockBackButton != null) knockBackButton.onClick.RemoveAllListeners();
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

        if (allPlayersData != null && allPlayersData.Count > 0)
        {
            Debug.Log($"Sorting and displaying ranking data for criteria: {criteria} (data already loaded)");
            SortPlayers(allPlayersData, criteria);
            DisplayRanking(allPlayersData, criteria);
        }
        else
        {
            LoadRankingData(criteria);
        }
    }

    private void LoadRankingData(RankingCriteria criteria)
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
                        UserData userData = JsonUtility.FromJson<UserData>(json);
                        allPlayersData.Add(userData);
                    }

                    SortPlayers(allPlayersData, criteria);
                    DisplayRanking(allPlayersData, criteria);
                }
            });
        }
        else
        {
            Debug.Log("LoadRankingData called but data already exists.");
            SortPlayers(allPlayersData, criteria);
            DisplayRanking(allPlayersData, criteria);
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
        }
    }

    private void DisplayRanking(List<UserData> players, RankingCriteria criteria)
    {
        if (rankingItemPrefab == null || contentTransform == null)
        {
            Debug.LogError("RankingItemPrefab or ContentTransform is not assigned!");
            return;
        }

        for (int i = 0; i < players.Count; i++)
        {
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
}