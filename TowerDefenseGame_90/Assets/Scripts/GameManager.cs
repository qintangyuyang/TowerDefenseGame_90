using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("场景引用")]
    [SerializeField] private Transform bornPointsRoot;
    [SerializeField] private Transform roundPointRoot;
    [SerializeField] private Transform baseTarget;
    [SerializeField] private Enemy enemyPrefab;

    [Header("防御塔")]
    [SerializeField] private Tower[] towers;

    [Header("UI 引用")]
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private GameObject tempPanel;
    [SerializeField] private TMP_Text victoryText;
    [SerializeField] private TMP_Text defeatText;
    [SerializeField] private Button reopenButton;
    [SerializeField] private TMP_Text killNumText;
    [SerializeField] private TMP_Text scoreNumText;

    public GameData Data { get; private set; }
    public int BaseHp { get; private set; }
    public int MaxBaseHp { get; private set; }
    public int Kills { get; private set; }
    public int Score { get; private set; }
    public bool IsRunning { get; private set; }
    public IReadOnlyList<Enemy> Enemies => enemies;

    private readonly List<Enemy> enemies = new List<Enemy>();
    private readonly List<Vector3> pathPoints = new List<Vector3>();
    private readonly List<Transform> spawnPoints = new List<Transform>();
    private readonly List<SpawnEvent> schedule = new List<SpawnEvent>();

    private float elapsed;
    private int nextSpawnIndex;
    private int spawnCursor;
    private int lastShownSecond = -1;

    private struct SpawnEvent
    {
        public float time;
        public string enemyId;
    }

    private void Awake()
    {
        Instance = this;
        Application.targetFrameRate = 60;
        Time.timeScale = 1f;

        if (!GameDataService.LoadConfig())
        {
            Debug.LogError(GameDataService.LastError);
            enabled = false;
            return;
        }

        Data = GameDataService.Config;
        MaxBaseHp = Data.round.baseHp;

        BuildSceneRefs();
        InitializeTowers();
        HideResult();

        if (reopenButton != null)
            reopenButton.onClick.AddListener(RestartGame);
    }

    private void Start()
    {
        StartGame();
    }

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

        if (timeText == null ||
            tempPanel == null ||
            victoryText == null ||
            defeatText == null ||
            reopenButton == null ||
            killNumText == null ||
            scoreNumText == null)
        {
            Debug.LogWarning("GameManager: UI 引用没有配完整");
        }
    }

    /// <summary>
    /// 按 Tower.towerId 把 JSON 里的炮塔参数注入场景炮塔。
    /// </summary>
    private void InitializeTowers()
    {
        if (towers == null)
            towers = new Tower[0];

        if (towers.Length == 0)
        {
            Debug.LogWarning("GameManager: 没有配置炮塔");
            return;
        }

        foreach (Tower tower in towers)
        {
            if (tower == null)
                continue;

            TowerDefinition def = Data.towers.Find(t => t.id == tower.TowerId);

            if (def == null)
            {
                Debug.LogError($"找不到炮塔配置：{tower.TowerId}");
                tower.gameObject.SetActive(false);
                continue;
            }

            tower.Initialize(def, this);
        }
    }

    /// <summary>
    /// 开始新一局，也是重开调用的入口。
    /// </summary>
    private void StartGame()
    {
        Time.timeScale = 1f;

        ClearEnemies();

        ClearBullets();

        foreach (Tower tower in towers)
        {
            if (tower != null)
                tower.ResetTower();
        }

        BaseHp = MaxBaseHp;
        Kills = 0;
        Score = 0;
        UpdateHud();
        elapsed = 0f;
        nextSpawnIndex = 0;
        spawnCursor = 1; // 先从左入口刷
        lastShownSecond = -1;
        IsRunning = true;

        BuildSchedule();
        HideResult();

        if (timeText != null)
        {
            timeText.gameObject.SetActive(true);
            timeText.text = Mathf.CeilToInt(Data.round.duration).ToString();
        }

        UpdateTimeText();
        Debug.Log("开始新一局");
    }

    public void RestartGame()
    {
        StartGame();
    }

    private void BuildSchedule()
    {
        schedule.Clear();

        foreach (WaveEntry wave in Data.waves)
        {
            if (wave == null) continue;

            float interval = Mathf.Max(0.05f, wave.interval);

            for (int i = 0; i < wave.count; i++)
            {
                schedule.Add(new SpawnEvent
                {
                    time = wave.startTime + i * interval,
                    enemyId = wave.enemyId
                });
            }
        }

        schedule.Sort((a, b) => a.time.CompareTo(b.time));
    }

    private void Update()
    {
        if (!IsRunning) return;

        elapsed += Time.deltaTime;

        UpdateTimeText();
        SpawnDueEnemies();

        if (elapsed >= Data.round.duration)
            Win();
    }

    private void UpdateTimeText()
    {
        if (timeText == null) return;

        int seconds = Mathf.CeilToInt(
            Mathf.Max(0f, Data.round.duration - elapsed));

        if (seconds == lastShownSecond) return;

        lastShownSecond = seconds;
        timeText.text = seconds.ToString();
    }

    private void SpawnDueEnemies()
    {
        while (nextSpawnIndex < schedule.Count &&
               elapsed >= schedule[nextSpawnIndex].time)
        {
            SpawnEnemy(schedule[nextSpawnIndex].enemyId);
            nextSpawnIndex++;
        }
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
            Lose();
    }

    public void RemoveEnemy(Enemy enemy)
    {
        enemies.Remove(enemy);
    }

    /// <summary>
    /// 敌人死亡时由 Enemy 调用。
    /// </summary>
    public void AddKill()
    {
        if (!IsRunning) return;

        Kills++;

        // 这一步先按每击杀加分；
        // 基地血量和胜利奖励留到结算/最高分时再加。
        Score += Data.score.perKill;

        UpdateHud();
    }

    private void UpdateHud()
    {
        if (killNumText != null)
            killNumText.text = $"Kill:{Kills}";

        if (scoreNumText != null)
            scoreNumText.text = $"Score:{Score}";
    }

    private void Win()
    {
        if (!IsRunning) return;

        IsRunning = false;
        ShowResult(true);
    }

    private void Lose()
    {
        if (!IsRunning) return;

        IsRunning = false;
        ShowResult(false);
    }

    private void ShowResult(bool win)
    {
        Time.timeScale = 0f;

        if (timeText != null)
            timeText.text = "0";

        if (tempPanel != null)
            tempPanel.SetActive(true);

        if (victoryText != null)
            victoryText.gameObject.SetActive(win);

        if (defeatText != null)
            defeatText.gameObject.SetActive(!win);

        if (reopenButton != null)
            reopenButton.gameObject.SetActive(true);

        Debug.Log(win ? "胜利" : "失败");
    }

    private void HideResult()
    {
        if (tempPanel != null)
            tempPanel.SetActive(false);

        if (victoryText != null)
            victoryText.gameObject.SetActive(false);

        if (defeatText != null)
            defeatText.gameObject.SetActive(false);

        if (reopenButton != null)
            reopenButton.gameObject.SetActive(false);
    }

    private void ClearEnemies()
    {
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            if (enemies[i] != null)
                Destroy(enemies[i].gameObject);
        }

        enemies.Clear();
    }

    private void ClearBullets()
    {
        foreach (Bullet bullet in FindObjectsOfType<Bullet>())
        {
            if (bullet != null)
                Destroy(bullet.gameObject);
        }
    }

    private void OnDestroy()
    {
        if (reopenButton != null)
            reopenButton.onClick.RemoveListener(RestartGame);

        Time.timeScale = 1f;
    }
}
