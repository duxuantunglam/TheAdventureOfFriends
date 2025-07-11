using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Multiplayer_Enemy_Trunk : MultiplayerEnemy
{
    [Header("Trunk details")]
    [SerializeField] private Multiplayer_Enemy_Bullet bulletPrefab;
    [SerializeField] private Transform gunPoint;
    [SerializeField] private float bulletSpeed = 7;
    [SerializeField] private float attackCoolDown = 1.5f;
    private float lastTimeAttacked;

    protected override void Update()
    {
        base.Update();

        if (isDead)
            return;

        bool canAttack = Time.time > lastTimeAttacked + attackCoolDown;

        if (isPlayerDetected && canAttack)
            Attack();


        HandleMovement();

        if (isGrounded)
            HandleTurnAround();
    }

    private void Attack()
    {
        idleTimer = idleDuration + attackCoolDown;
        lastTimeAttacked = Time.time;
        anim.SetTrigger("attack");
    }

    private void CreateBullet()
    {
        Multiplayer_Enemy_Bullet newBullet = Instantiate(bulletPrefab, gunPoint.position, Quaternion.identity);

        Vector2 bulletVelocity = new Vector2(facingDir * bulletSpeed, 0);
        newBullet.SetVelocity(bulletVelocity);

        if (facingDir == 1)
            newBullet.FlipSprite();

        Destroy(newBullet.gameObject, 10);
    }

    private void HandleTurnAround()
    {
        if (!isGroundInFrontDetected || isWallDetected)
        {
            Flip();
            idleTimer = idleDuration;
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void HandleMovement()
    {
        if (idleTimer > 0)
            return;

        rb.linearVelocity = new Vector2(moveSpeed * facingDir, rb.linearVelocity.y);
    }
}