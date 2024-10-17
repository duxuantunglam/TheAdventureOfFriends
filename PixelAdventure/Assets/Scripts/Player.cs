using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

public class Player : MonoBehaviour
{
    private Rigidbody2D rb;
    private Animator anim;
    private CapsuleCollider2D cd;

    private bool canBeController = false;

    [Header("Movement")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float jumpForce;
    [SerializeField] private float doubleJumpForce;
    private float defaultGravityScale;
    private bool canDoubleJump;

    [Header("Buffer & Coyote jump")]
    [SerializeField] private float bufferJumpWindow = .25f;
    private float bufferJumpActivated = -1;
    [SerializeField] private float coyoteJumpWindow = .5f;
    private float coyoteJumpActivated = -1;

    [Header("Wall interactions")]
    [SerializeField] private float wallJumpDuration = .6f;
    [SerializeField] private Vector2 wallJumpForce;
    private bool isWallJumping;

    [Header("Knock back")]
    [SerializeField] private float knockBackDuration = 1;
    [SerializeField] private Vector2 knockBackPower;
    private bool isKnocked;

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

    [Header("VFX")]
    [SerializeField] private GameObject deathVFX;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<Animator>();
        cd = GetComponent<CapsuleCollider2D>();
    }

    private void Start()
    {
        defaultGravityScale = rb.gravityScale;
        RespawnFinished(false);
    }

    private void Update()
    {
        UpdateAirborneStatus();

        if (canBeController == false)
            return;

        if (isKnocked)
            return;

        HandleInput();
        HandleWallSlide();
        HandleMovement();
        HandleFlip();
        HandleCollision();
        HandleAnimations();
    }

    public void RespawnFinished(bool finished)
    {

        if (finished)
        {
            rb.gravityScale = defaultGravityScale;
            canBeController = true;
            cd.enabled = true;
        }
        else
        {
            rb.gravityScale = 0;
            canBeController = false;
            cd.enabled = false;
        }
    }

    public void KnockBack()
    {
        if (isKnocked)
            return;

        StartCoroutine(KnockBackRoutine());
        anim.SetTrigger("knockback");
        rb.velocity = new Vector2(knockBackPower.x * -facingDir, knockBackPower.y);
    }

    private IEnumerator KnockBackRoutine()
    {
        isKnocked = true;

        yield return new WaitForSeconds(knockBackDuration);

        isKnocked = false;
    }

    private void UpdateAirborneStatus()
    {
        if (isGrounded && isAirBorne)
            HandleLanding();
        else if (isGrounded && !isAirBorne)
            BecomeAirborne();
    }

    private void BecomeAirborne()
    {
        isAirBorne = true;

        if (rb.velocity.y < 0)
            ActiveCoyoteJump();
    }

    private void HandleLanding()
    {
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

#region Buffer & Coyote jump
    private void RequestBufferJump()
    {
        if (isAirBorne)
            bufferJumpActivated = Time.time;
    }

    private void AttemptBufferJump()
    {
        if (Time.time < bufferJumpActivated + bufferJumpWindow)
        {
            bufferJumpActivated = Time.time - 1;
            Jump();
        }
    }

    private void ActiveCoyoteJump() => coyoteJumpActivated = Time.time;

    private void CancelCoyoteJump() => coyoteJumpActivated = Time.time - 1;
#endregion

    private void JumpButton()
    {
        bool coyoteJumpAvailable = Time.time < coyoteJumpActivated + coyoteJumpWindow;

        if (isGrounded || coyoteJumpAvailable)
            Jump();
        else if (isWallDetected && !isGrounded)
            WallJump();
        else if (canDoubleJump)
            DoubleJump();

        CancelCoyoteJump();
    }

    private void Jump() => rb.velocity = new Vector2(rb.velocity.x, jumpForce);

    private void DoubleJump()
    {
        isWallJumping = false;
        canDoubleJump = false;
        rb.velocity = new Vector2(rb.velocity.x, doubleJumpForce);
    }

    private void WallJump()
    {
        canDoubleJump = true;
        rb.velocity = new Vector2(wallJumpForce.x * -facingDir, wallJumpForce.y);

        Flip();

        StopAllCoroutines();
        StartCoroutine(WallJumpRoutine());
    }

    private IEnumerator WallJumpRoutine()
    {
        isWallJumping = true;

        yield return new WaitForSeconds(wallJumpDuration);

        isWallJumping = false;
    }

    public void Die()
    {
        GameObject newDeathVFX = Instantiate(deathVFX, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    private void HandleWallSlide()
    {
        bool canWallSlide = isWallDetected && rb.velocity.y < 0;
        float yModifier = yInput < 0 ? 1 : .05f;

        if (canWallSlide == false)
            return;

        rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * yModifier);
    }

    private void HandleCollision()
    {
        isGrounded = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, whatIsGround);
        isWallDetected = Physics2D.Raycast(transform.position, Vector2.right * facingDir, wallCheckDistance, whatIsGround);
    }

    private void HandleAnimations()
    {
        anim.SetFloat("xVelocity", rb.velocity.x);
        anim.SetFloat("yVelocity", rb.velocity.y);
        anim.SetBool("isGrounded", isGrounded);
        anim.SetBool("isWallDetected", isWallDetected);
    }

    private void HandleMovement()
    {
        if (isWallDetected)
            return;
        if (isWallJumping)
            return;

        rb.velocity = new Vector2(xInput * moveSpeed, rb.velocity.y);
    }

    private void HandleFlip()
    {
        if (xInput < 0 && facingRight || xInput > 0 && !facingRight)
            Flip();
    }

    private void Flip()
    {
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