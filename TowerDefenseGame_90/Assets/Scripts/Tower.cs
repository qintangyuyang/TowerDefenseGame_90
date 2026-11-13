using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tower : MonoBehaviour
{
    [Header("场景绑定")]
    [SerializeField] private string towerId = "tower_basic";
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private Transform muzzle;

    private TowerDefinition definition;
    private GameManager manager;
    private float cooldown;

    public string TowerId => towerId;

    /// <summary>
    /// 由 GameManager 在 JSON 加载后调用。
    /// </summary>
    public void Initialize(TowerDefinition def, GameManager gameManager)
    {
        if (def == null || gameManager == null)
        {
            Debug.LogError($"Tower {name}: 初始化参数缺失");
            enabled = false;
            return;
        }

        definition = def;
        manager = gameManager;
        cooldown = 0f;

        Debug.Log($"炮塔 {name} 初始化：id={towerId}, range={def.range}");
    }

    public void ResetTower()
    {
        cooldown = 0f;
    }

    private void Update()
    {
        if (definition == null || manager == null || !manager.IsRunning)
            return;

        cooldown -= Time.deltaTime;
        if (cooldown > 0f)
            return;

        Enemy target = FindTarget();
        if (target == null)
            return;

        cooldown = Mathf.Max(0.05f, definition.fireInterval);
        Fire(target);
    }

    /// <summary>
    /// 射程内优先打“离基地最近”的敌人。
    /// </summary>
    private Enemy FindTarget()
    {
        Enemy best = null;
        float bestDistanceToBase = float.MaxValue;
        float sqrRange = definition.range * definition.range;

        for (int i = 0; i < manager.Enemies.Count; i++)
        {
            Enemy enemy = manager.Enemies[i];
            if (enemy == null || !enemy.IsAlive)
                continue;

            Vector2 diff =
                (Vector2)enemy.transform.position -
                (Vector2)transform.position;

            if (diff.sqrMagnitude > sqrRange)
                continue;

            float distanceToBase = enemy.DistanceToBase;
            if (distanceToBase < bestDistanceToBase)
            {
                bestDistanceToBase = distanceToBase;
                best = enemy;
            }
        }

        return best;
    }

    private void Fire(Enemy target)
    {
        // 没有拖子弹 Prefab 时，退化为直接扣血。
        if (bulletPrefab == null)
        {
            target.TakeDamage(definition.damage);
            return;
        }

        Vector3 spawnPos = muzzle != null
            ? muzzle.position
            : transform.position + Vector3.up * 0.9f;

        Bullet bullet = Instantiate(
            bulletPrefab,
            spawnPos,
            Quaternion.identity);

        bullet.Initialize(
            target,
            definition.damage,
            definition.bulletSpeed,
            definition.bulletLifeTime);
    }
}
