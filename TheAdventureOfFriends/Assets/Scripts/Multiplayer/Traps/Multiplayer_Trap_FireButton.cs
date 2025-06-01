using UnityEngine;

public class Multiplayer_Trap_FireButton : MonoBehaviour
{
    private Animator anim;
    private Multiplayer_Trap_Fire multiplayerTrapFire;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        multiplayerTrapFire = GetComponentInParent<Multiplayer_Trap_Fire>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Player player = collision.gameObject.GetComponent<Player>();

        if (player != null)
        {
            anim.SetTrigger("activate");
            multiplayerTrapFire.SwitchOffMultiplayerFire();
        }

    }
}