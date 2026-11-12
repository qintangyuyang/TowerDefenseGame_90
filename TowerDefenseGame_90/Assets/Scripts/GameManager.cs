using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("场景用")]
    [SerializeField]
    private Transform bornPointsRoot;
    [SerializeField]
    private Transform roundPointRoot;
    [SerializeField]
    private Transform baseTarget;
    [SerializeField]
    private Enemy enemyPrefab;

    [Header("第二部测试")]
    [SerializeField]
    private bool spawnTestEnemyOnStart = true;

    public GameData Data { get; private set; }
    public int BaseHp { get; private set; }
    public int MaxBaseHp { get; private set; }
    public bool IsRunning { get; private set; }
    public IReadOnlyList<Enemy> Enemies => enemies;

    private readonly List<Enemy> enemies = new List<Enemy>();
    private readonly List<Vector3> pathPoints = new List<Vector3>();
    private readonly List<Transform> spawnPoints = new List<Transform>();
    private int spawnCursor;

    void Awake()
    {
        Instance = this;
        Application.targetFrameRate = 60;

        if (!GameDataService.LoadConfig())
        {
            Debug.LogError(GameDataService.LastError);
            enabled = false;
            return;
        }

        Data = GameDataService.Config;
        MaxBaseHp = Data.round.baseHp;

        BuildSceneRefs();
    }

    void Start()
    {
        ResetTest();
        if (spawnTestEnemyOnStart && IsRunning)
        {
            SpawnEnemy("runner");
        }
    }

    /// <summary>
    /// 从场景里收集：
    /// BornPoints 的两个出生点；
    /// RoundPoint 的 6 个路径点。
    /// </summary>
    private void BuildSceneRefs()
    {
        pathPoints.Clear();
        if (roundPointRoot != null)
        {
            for (int i = 0; i < roundPointRoot.childCount; i++)
                pathPoints.Add(roundPointRoot.GetChild(i).position);
        }

        spawnPoints.Clear();
        if (bornPointsRoot != null)
        {
            for (int i = 0; i < bornPointsRoot.childCount; i++)
                spawnPoints.Add(bornPointsRoot.GetChild(i));
        }

        if (pathPoints.Count == 0 ||
            spawnPoints.Count == 0 ||
            baseTarget == null ||
            enemyPrefab == null)
        {
            Debug.LogError("GameManager: 场景引用不完整");
            enabled = false;
        }
    }

    public void ResetTest()
    {
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            if (enemies[i] != null)
                Destroy(enemies[i].gameObject);
        }

        enemies.Clear();
        BaseHp = MaxBaseHp;

        // BornPoints 第二个是左入口，先从左边测试，
        // 避免右入口单位先横穿顶部道路。
        spawnCursor = 1;

        IsRunning = true;
        Debug.Log($"测试重置，基地血量 {BaseHp}");
    }

    public void SpawnEnemy(string enemyId)
    {
        if (!IsRunning || Data == null || spawnPoints.Count == 0)
            return;

        EnemyDefinition definition = Data.enemies.Find(e => e.id == enemyId);
        if (definition == null)
        {
            Debug.LogError($"找不到敌人配置：{enemyId}");
            return;
        }

        Transform spawn = spawnPoints[spawnCursor % spawnPoints.Count];
        spawnCursor++;

        // 不设父物体，避免 GameRoot 位置影响出生点。
        Enemy enemy = Instantiate(enemyPrefab, spawn.position, Quaternion.identity);
        enemy.Initialize(definition, pathPoints, baseTarget, this);
        enemies.Add(enemy);

        Debug.Log($"刷怪：{enemyId}，出生点：{spawn.name}");
    }

    public void DamageBase(int damage)
    {
        if (!IsRunning) return;

        BaseHp = Mathf.Max(0, BaseHp - damage);
        Debug.Log($"基地受到 {damage} 点伤害，剩余 {BaseHp}");

        if (BaseHp <= 0)
        {
            IsRunning = false;
            Debug.Log("测试失败：基地血量归零");
        }
    }

    public void RemoveEnemy(Enemy enemy)
    {
        enemies.Remove(enemy);
    }

    /// <summary>
    /// 第二步临时调试界面，不改 Canvas。
    /// </summary>
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(20, 20, 280, 220), GUI.skin.box);

        GUILayout.Label($"基地：{BaseHp}/{MaxBaseHp}");
        GUILayout.Label($"敌人：{enemies.Count}");
        GUILayout.Label($"路径点：{pathPoints.Count}");
        GUILayout.Label($"出生点：{spawnPoints.Count}");

        if (GUILayout.Button("重新测试"))
            ResetTest();

        if (GUILayout.Button("刷 Runner"))
            SpawnEnemy("runner");

        if (GUILayout.Button("刷 Tank"))
            SpawnEnemy("tank");

        GUILayout.EndArea();
    }
}
