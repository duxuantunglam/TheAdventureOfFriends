using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;

public class Player : MonoBehaviour
{
    private Rigidbody2D rb;
    private Animator anim;

    [SerializeField] private float moveSpeed;

    private float xInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        xInput = Input.GetAxis("Horizontal");
        
        HandleAnimations();
        HandleMovement();
    }

    private void HandleAnimations() {
        anim.SetBool("isRunning", rb.velocity.x != 0);
    }

    private void HandleMovement() {
        rb.velocity = new Vector2(xInput * moveSpeed, rb.velocity.y);
    }
}
