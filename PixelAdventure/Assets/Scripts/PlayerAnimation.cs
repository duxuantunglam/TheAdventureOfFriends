using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    private Player player;

    private void Awake()
    {
        player = GetComponentInChildren<Player>();
    }

    public void FinishRespawn() => player.RespawnFinished(true);
}
