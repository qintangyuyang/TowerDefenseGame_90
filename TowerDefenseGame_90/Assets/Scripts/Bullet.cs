using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    private Enemy target;
    private int damage;
    private float speed;
    private float lifeTime;
    private float life;

    /// <summary>
    /// 由 Tower.Fire 调用。
    /// 伤害、速度、寿命全部来自 JSON。
    /// </summary>
    public void Initialize(
        Enemy enemy,
        int amount,
        float bulletSpeed,
        float bulletLifeTime)
    {
        target = enemy;
        damage = amount;
        speed = Mathf.Max(0.01f, bulletSpeed);
        lifeTime = Mathf.Max(0.01f, bulletLifeTime);
        life = lifeTime;
    }

    private void Update()
    {
        life -= Time.deltaTime;

        if (life <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        if (target == null || !target.IsAlive)
        {
            Destroy(gameObject);
            return;
        }

        Vector2 current = transform.position;
        Vector2 targetPos = target.transform.position;

        transform.position = Vector2.MoveTowards(
            current,
            targetPos,
            speed * Time.deltaTime);

        if (Vector2.Distance(transform.position, targetPos) < 0.15f)
        {
            target.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}
