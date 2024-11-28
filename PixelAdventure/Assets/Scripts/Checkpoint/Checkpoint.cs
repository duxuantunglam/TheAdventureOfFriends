using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    private Animator anim;
    private bool active;

    [SerializeField] private bool canBeReactive;

    private void Awake()
    {
        anim = GetComponent<Animator>();
    }

    private void Start()
    {
        canBeReactive = GameManager.instance.canReactive;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (active && canBeReactive == false)
            return;

        Player player = collision.GetComponent<Player>();

        if (player != null)
            ActivateCheckpoint();
    }

    private void ActivateCheckpoint()
    {
        active = true;
        anim.SetTrigger("activate");
        PlayerManager.instance.UpdateRespawnPosition(transform);
    }
}
