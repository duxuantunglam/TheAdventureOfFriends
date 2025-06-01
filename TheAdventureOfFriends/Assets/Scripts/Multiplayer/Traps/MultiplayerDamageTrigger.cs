using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiplayerDamageTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        Player player = collision.gameObject.GetComponent<Player>();

        if (player != null)
        {
            // Handle damage in multiplayer (in Easy mode, players don't take damage like Normal/Hard)
            // In multiplayer we play in Easy mode equivalent, so we only knockback without damage

            // Count the knockback for score calculation
            if (MultiplayerGameManager.instance != null)
            {
                MultiplayerGameManager.instance.PlayerKnockedBack();
            }

            // Apply knockback effect
            player.KnockBack(transform.position.x);
        }
    }
}