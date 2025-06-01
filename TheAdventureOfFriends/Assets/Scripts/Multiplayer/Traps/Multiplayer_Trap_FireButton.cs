using UnityEngine;

public class Multiplayer_Trap_FireButton : MonoBehaviour
{
    private Animator anim;
    private Multiplayer_Trap_Fire trapFire;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        trapFire = GetComponentInParent<Multiplayer_Trap_Fire>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Player player = collision.gameObject.GetComponent<Player>();

        if (player != null)
        {
            anim.SetTrigger("activate");
            trapFire.SwitchOffFire();
        }

    }
}