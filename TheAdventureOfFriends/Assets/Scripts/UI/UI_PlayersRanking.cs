using TMPro;
using UnityEngine;

public class UI_PlayersRanking : MonoBehaviour
{
    public TextMeshProUGUI rankText;
    public TextMeshProUGUI playerNameText;
    public TextMeshProUGUI scoreText;

    /// <summary>
    /// Sets the ranking data for this UI element.
    /// </summary>
    /// <param name="rank">The rank of the player.</param>
    /// <param name="playerName">The name of the player.</param>
    /// <param name="score">The score of the player (total fruit or average time).</param>
    public void SetRankingData(int rank, string playerName, string score)
    {
        if (rankText != null)
        {
            rankText.text = rank.ToString();
        }
        if (playerNameText != null)
        {
            playerNameText.text = playerName;
        }
        if (scoreText != null)
        {
            scoreText.text = score;
        }
    }

    public void SetRankingData(int rank, string playerName, float score, string scoreFormat = "F2")
    {
        if (rankText != null)
        {
            rankText.text = rank.ToString();
        }
        if (playerNameText != null)
        {
            playerNameText.text = playerName;
        }
        if (scoreText != null)
        {
            scoreText.text = score.ToString(scoreFormat);
        }
    }
}