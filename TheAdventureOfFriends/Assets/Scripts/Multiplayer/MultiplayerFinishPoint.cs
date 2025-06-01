using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiplayerFinishPoint : MonoBehaviour
{
    private Animator anim;
    private bool hasBeenTriggered = false;

    private void Awake()
    {
        anim = GetComponent<Animator>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Player player = collision.GetComponent<Player>();

        if (player != null && !hasBeenTriggered)
        {
            // Mark as triggered to prevent multiple calls
            hasBeenTriggered = true;

            // Play sound effect
            if (AudioManager.instance != null)
            {
                AudioManager.instance.PlaySFX(2);
            }

            // Trigger animation
            if (anim != null)
            {
                anim.SetTrigger("active");
            }

            // Call multiplayer game manager instead of single player game manager
            if (MultiplayerGameManager.instance != null)
            {
                MultiplayerGameManager.instance.LevelFinished();
                Debug.Log("MultiplayerFinishPoint: Player finished the level!");
            }
            else
            {
                Debug.LogError("MultiplayerFinishPoint: MultiplayerGameManager instance not found!");
            }
        }
    }

    // Method to reset the finish point (useful for testing or restarting)
    public void ResetFinishPoint()
    {
        hasBeenTriggered = false;
        Debug.Log("MultiplayerFinishPoint: Reset finish point.");
    }

    private void OnValidate()
    {
        // Ensure we have an Animator component
        if (anim == null)
        {
            anim = GetComponent<Animator>();
        }
    }
}