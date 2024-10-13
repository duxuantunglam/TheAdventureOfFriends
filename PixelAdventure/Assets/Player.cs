using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;

public class Player : MonoBehaviour
{
    private Rigidbody2D rb;
    private Animator anim;

    [Header("Movement")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float jumpForce;
    [SerializeField] private float doubleJumpForce;
    private bool canDoubleJump;

    [Header("Buffer jump")]
    [SerializeField] private float bufferJumpWindow = .25f;
    private float bufferJumpActivated = -1;

    [Header("Wall interactions")]
    [SerializeField] private float wallJumpDuration = .6f;
    [SerializeField] private Vector2 wallJumpForce;
    private bool isWallJumping;

    [Header("Knock back")]
    [SerializeField] private float knockBackDuration = 1;
    [SerializeField] private Vector2 knockBackPower;
    private bool isKnocked;
    private bool canBeKnocked;

    [Header("Collision")]
    [SerializeField] private float groundCheckDistance;
    [SerializeField] private float wallCheckDistance;
    [SerializeField] private LayerMask whatIsGround;
    private bool isGrounded;
    private bool isAirBorne;
    private bool isWallDetected;

    private float xInput;
    private float yInput;

    private bool facingRight = true;

    private int facingDir = 1;          //facingDirection

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        UpdateAirborneStatus();

        if (isKnocked) {
            return;
        }

        HandleInput();
        HandleWallSlide();
        HandleMovement();
        HandleFlip();
        HandleCollision();
        HandleAnimations();
    }

    public void KnockBack() {
        if (isKnocked) {
            return;
        }

        StartCoroutine(KnockBackRoutine());
        anim.SetTrigger("knockback");
        rb.velocity = new Vector2(knockBackPower.x * -facingDir, knockBackPower.y);
    }

    private void HandleWallSlide() {
        bool canWallSlide = isWallDetected && rb.velocity.y < 0;
        float yModifier = yInput < 0 ? 1 : .05f;

        if (canWallSlide == false) {
            return;
        }

        rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * yModifier);
    }

    private void UpdateAirborneStatus() {
        if (isGrounded && isAirBorne) {
            HandleLanding();
        }
        else if (isGrounded && !isAirBorne) {
            BecomeAirborne();
        }
    }

    private void BecomeAirborne() {
        isAirBorne = true;
    }

    private void HandleLanding() {
        isAirBorne = false;
        canDoubleJump = true;

        AttemptBufferJump();
    }

    private void HandleInput()
    {
        xInput = Input.GetAxis("Horizontal");
        yInput = Input.GetAxis("Vertical");

        if (Input.GetKeyDown(KeyCode.Space))
        {
            JumpButton();
            RequestBufferJump();
        }
    }

    private void RequestBufferJump() {
        if (isAirBorne) {
            bufferJumpActivated = Time.time;
        }
    }

    private void AttemptBufferJump() {
        if (Time.time < bufferJumpActivated + bufferJumpWindow) {
            bufferJumpActivated = 0;
            Jump();
        }
    }

    private void JumpButton() {
        if (isGrounded) {
            Jump();
        }
        else if (isWallDetected && !isGrounded) {
            WallJump();
        }
        else if (canDoubleJump) {
            DoubleJump();
        }
    }

    private void Jump() => rb.velocity = new Vector2(rb.velocity.x, jumpForce);

    private void DoubleJump() {
        isWallJumping = false;
        canDoubleJump = false;
        rb.velocity = new Vector2(rb.velocity.x, doubleJumpForce);
    }

    private void WallJump() {
        canDoubleJump = true;
        rb.velocity = new Vector2(wallJumpForce.x * -facingDir, wallJumpForce.y);

        Flip();

        StopAllCoroutines();
        StartCoroutine(WallJumpRoutine());
    }

    private IEnumerator WallJumpRoutine() {
        isWallJumping = true;

        yield return new WaitForSeconds(wallJumpDuration);

        isWallJumping = false;
    }

    private IEnumerator KnockBackRoutine() {
        canBeKnocked = false;
        isKnocked = true;

        yield return new WaitForSeconds(knockBackDuration);

        canBeKnocked = true;
        isKnocked = false;
    }

    private void HandleCollision()
    {
        isGrounded = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, whatIsGround);
        isWallDetected = Physics2D.Raycast(transform.position, Vector2.right * facingDir, wallCheckDistance, whatIsGround);
    }

    private void HandleAnimations() {
        anim.SetFloat("xVelocity", rb.velocity.x);
        anim.SetFloat("yVelocity", rb.velocity.y);
        anim.SetBool("isGrounded", isGrounded);
        anim.SetBool("isWallDetected", isWallDetected);
    }

    private void HandleMovement() {
        if (isWallDetected) {
            return;
        }
        if (isWallJumping) {
            return;
        }

        rb.velocity = new Vector2(xInput * moveSpeed, rb.velocity.y);
    }

    private void HandleFlip() {
        if (xInput < 0 && facingRight || xInput > 0 && !facingRight) {
            Flip();
        }
    }

    private void Flip() {
        facingDir = facingDir * (-1);
        transform.Rotate(0, 180, 0);
        facingRight = !facingRight;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawLine(transform.position, new Vector2(transform.position.x, transform.position.y - groundCheckDistance));
        Gizmos.DrawLine(transform.position, new Vector2(transform.position.x + (wallCheckDistance * facingDir) , transform.position.y));
    }
}
