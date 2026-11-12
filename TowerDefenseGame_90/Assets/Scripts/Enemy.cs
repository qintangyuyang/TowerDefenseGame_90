using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    private EnemyDefinition definition;
    private List<Vector3> path;
    private Transform baseTarget;
    private GameManager manager;

    private SpriteRenderer spriteRenderer;
    private float hp;
    private float attackTimer;
    private int pathIndex;
    private bool registered;
    private bool dead;

    public bool IsAlive => !dead && hp > 0f;
    public float DistanceToBase =>
        baseTarget == null ? 999f : Vector2.Distance(transform.position, baseTarget.position);

    /// <summary>
    /// 由 GameManager 生成后调用。
    /// </summary>
    public void Initialize(
        EnemyDefinition def,
        List<Vector3> pathPoints,
        Transform target,
        GameManager gameManager)
    {
        if (def == null ||
            target == null ||
            gameManager == null ||
            pathPoints == null ||
            pathPoints.Count == 0)
        {
            Debug.LogError("Enemy: 初始化参数缺失");
            enabled = false;
            return;
        }

        definition = def;
        path = pathPoints;
        baseTarget = target;
        manager = gameManager;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null &&
            ColorUtility.TryParseHtmlString(def.color, out Color color))
        {
            spriteRenderer.color = color;
        }

        transform.localScale = Vector3.one * def.scale;

        hp = def.maxHp;
        attackTimer = 0f;
        pathIndex = 0;
        dead = false;
        registered = true;

        gameObject.name = $"{def.displayName}_{GetInstanceID()}";
    }

    private void Update()
    {
        if (!IsAlive || manager == null || !manager.IsRunning)
            return;

        // 已经进入攻击距离，停下打基地。
        if (DistanceToBase <= definition.attackRange)
        {
            Attack();
            return;
        }

        MoveAlongPath();
    }

    private void MoveAlongPath()
    {
        Vector3 target = pathIndex < path.Count
            ? path[pathIndex]
            : baseTarget.position;

        Vector2 current = transform.position;
        Vector2 target2 = target;

        // 到达当前路径点，切到下一个。
        if (Vector2.Distance(current, target2) < 0.05f)
        {
            pathIndex++;
            return;
        }

        transform.position = Vector2.MoveTowards(
            current,
            target2,
            definition.moveSpeed * Time.deltaTime);
    }

    private void Attack()
    {
        attackTimer -= Time.deltaTime;

        if (attackTimer <= 0f)
        {
            attackTimer = definition.attackInterval;
            manager.DamageBase(definition.baseDamage);
        }
    }

    public void TakeDamage(int amount)
    {
        if (!IsAlive) return;

        hp -= amount;

        if (hp <= 0f)
            Die();
    }

    private void Die()
    {
        if (dead) return;

        dead = true;

        if (registered && manager != null)
        {
            manager.RemoveEnemy(this);
            registered = false;
        }

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (registered && manager != null)
            manager.RemoveEnemy(this);
    }
}
