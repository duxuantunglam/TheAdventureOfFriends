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
            hasBeenTriggered = true;

            if (AudioManager.instance != null)
            {
                AudioManager.instance.PlaySFX(2);
            }

            if (anim != null)
            {
                anim.SetTrigger("active");
            }

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

    public void ResetFinishPoint()
    {
        hasBeenTriggered = false;
        Debug.Log("MultiplayerFinishPoint: Reset finish point.");
    }

    private void OnValidate()
    {
        if (anim == null)
        {
            anim = GetComponent<Animator>();
        }
    }
}