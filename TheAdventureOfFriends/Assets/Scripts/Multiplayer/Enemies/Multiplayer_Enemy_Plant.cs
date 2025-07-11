using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Multiplayer_Enemy_Plant : MultiplayerEnemy
{
    [Header("Plant details")]
    [SerializeField] private Multiplayer_Enemy_Bullet bulletPrefab;
    [SerializeField] private Transform gunPoint;
    [SerializeField] private float bulletSpeed = 7;
    [SerializeField] private float attackCoolDown = 1.5f;
    private float lastTimeAttacked;

    protected override void Update()
    {
        base.Update();

        bool canAttack = Time.time > lastTimeAttacked + attackCoolDown;

        if (isPlayerDetected && canAttack)
            Attack();
    }

    private void Attack()
    {
        lastTimeAttacked = Time.time;
        anim.SetTrigger("attack");
    }

    private void CreateBullet()
    {
        Multiplayer_Enemy_Bullet newBullet = Instantiate(bulletPrefab, gunPoint.position, Quaternion.identity);

        Vector2 bulletVelocity = new Vector2(facingDir * bulletSpeed, 0);
        newBullet.SetVelocity(bulletVelocity);

        Destroy(newBullet.gameObject, 10);
    }

    protected override void HandleAnimator()
    {
        //Keep it empty,unless need to update paratemtrs
    }
}