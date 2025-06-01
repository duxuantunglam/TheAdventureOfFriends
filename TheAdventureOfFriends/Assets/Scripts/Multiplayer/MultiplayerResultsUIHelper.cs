using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MultiplayerResultsUIHelper : MonoBehaviour
{
    private void Awake()
    {
        // Find and assign UI elements to Multiplayer_InGameUI
        Multiplayer_InGameUI inGameUI = GetComponent<Multiplayer_InGameUI>();
        if (inGameUI == null)
        {
            Debug.LogError("MultiplayerResultsUIHelper: Multiplayer_InGameUI component not found!");
            return;
        }

        // Find UI elements in the scene by name
        GameObject gameResultsPanel = GameObject.Find("GameResultsPanel");
        TextMeshProUGUI player1ResultsText = GameObject.Find("Player1Results_Text")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI player2ResultsText = GameObject.Find("Player2Results_Text")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI winnerText = GameObject.Find("FinalResult_Text")?.GetComponent<TextMeshProUGUI>();
        Button returnToMenuButton = GameObject.Find("ReturnToMenuButton")?.GetComponent<Button>();

        // Use reflection to assign private fields
        var gameResultsPanelField = typeof(Multiplayer_InGameUI).GetField("gameResultsPanel",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var player1ResultsTextField = typeof(Multiplayer_InGameUI).GetField("player1ResultsText",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var player2ResultsTextField = typeof(Multiplayer_InGameUI).GetField("player2ResultsText",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var winnerTextField = typeof(Multiplayer_InGameUI).GetField("winnerText",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var returnToMenuButtonField = typeof(Multiplayer_InGameUI).GetField("returnToMenuButton",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (gameResultsPanel != null && gameResultsPanelField != null)
        {
            gameResultsPanelField.SetValue(inGameUI, gameResultsPanel);
            Debug.Log("MultiplayerResultsUIHelper: GameResultsPanel assigned.");
        }
        else
        {
            Debug.LogError("MultiplayerResultsUIHelper: GameResultsPanel not found!");
        }

        if (player1ResultsText != null && player1ResultsTextField != null)
        {
            player1ResultsTextField.SetValue(inGameUI, player1ResultsText);
            Debug.Log("MultiplayerResultsUIHelper: Player1Results_Text assigned.");
        }
        else
        {
            Debug.LogError("MultiplayerResultsUIHelper: Player1Results_Text not found!");
        }

        if (player2ResultsText != null && player2ResultsTextField != null)
        {
            player2ResultsTextField.SetValue(inGameUI, player2ResultsText);
            Debug.Log("MultiplayerResultsUIHelper: Player2Results_Text assigned.");
        }
        else
        {
            Debug.LogError("MultiplayerResultsUIHelper: Player2Results_Text not found!");
        }

        if (winnerText != null && winnerTextField != null)
        {
            winnerTextField.SetValue(inGameUI, winnerText);
            Debug.Log("MultiplayerResultsUIHelper: FinalResult_Text assigned.");
        }
        else
        {
            Debug.LogError("MultiplayerResultsUIHelper: FinalResult_Text not found!");
        }

        if (returnToMenuButton != null && returnToMenuButtonField != null)
        {
            returnToMenuButtonField.SetValue(inGameUI, returnToMenuButton);
            Debug.Log("MultiplayerResultsUIHelper: ReturnToMenuButton assigned.");
        }
        else
        {
            Debug.LogError("MultiplayerResultsUIHelper: ReturnToMenuButton not found!");
        }

        Destroy(this);
    }
}