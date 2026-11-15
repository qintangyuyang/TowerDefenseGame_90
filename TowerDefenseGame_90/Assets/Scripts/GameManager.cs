using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public enum GameState
    {
        Home,
        Playing,
        Paused,
        Result
    }

    public static GameManager Instance { get; private set; }

    [Header("场景引用")]
    [SerializeField] private Transform bornPointsRoot;
    [SerializeField] private Transform roundPointRoot;
    [SerializeField] private Transform baseTarget;
    [SerializeField] private Enemy enemyPrefab;

    [Header("防御塔")]
    [SerializeField] private Tower[] towers;

    [Header("技能")]
    [SerializeField] private PlayerSkill playerSkill;

    [Header("面板")]
    [SerializeField] private GameObject homePanel;
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject tempPanel;

    [Header("HUD 文本")]
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text baseHpText;
    [SerializeField] private TMP_Text killNumText;
    [SerializeField] private TMP_Text scoreNumText;

    [Header("首页")]
    [SerializeField] private TMP_Text bestScoreText;
    [SerializeField] private Button startButton;

    [Header("暂停")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button pauseRestartButton;

    [Header("结算")]
    [SerializeField] private TMP_Text victoryText;
    [SerializeField] private TMP_Text defeatText;
    [SerializeField] private TMP_Text resultScoreText;
    [SerializeField] private TMP_Text resultBestText;
    [SerializeField] private Button reopenButton;

    public GameState State { get; private set; } = GameState.Home;
    public GameData Data { get; private set; }

    public int BaseHp { get; private set; }
    public int MaxBaseHp { get; private set; }
    public int Kills { get; private set; }
    public int Score { get; private set; }
    public int BestScore { get; private set; }
    public bool IsRunning { get; private set; }

    public IReadOnlyList<Enemy> Enemies => enemies;

    private readonly List<Enemy> enemies = new List<Enemy>();
    private readonly List<Vector3> pathPoints = new List<Vector3>();
    private readonly List<Transform> spawnPoints = new List<Transform>();
    private readonly List<SpawnEvent> schedule = new List<SpawnEvent>();

    private SaveData saveData;
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

        saveData = GameDataService.LoadSave();
        BestScore = saveData.bestScore;

        BindButtons();
        HideResult();
    }

    private void Start()
    {
        ShowHome();
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

        if (homePanel == null ||
            hudPanel == null ||
            pausePanel == null ||
            tempPanel == null ||
            timeText == null ||
            baseHpText == null ||
            killNumText == null ||
            scoreNumText == null ||
            bestScoreText == null ||
            startButton == null ||
            pauseButton == null ||
            resumeButton == null ||
            pauseRestartButton == null ||
            victoryText == null ||
            defeatText == null ||
            resultScoreText == null ||
            resultBestText == null ||
            reopenButton == null)
        {
            Debug.LogWarning("GameManager: UI 引用没有配完整");
        }
    }

    private void InitializeTowers()
    {
        if (towers == null)
            towers = new Tower[0];

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

    private void BindButtons()
    {
        if (startButton != null)
            startButton.onClick.AddListener(StartGame);

        if (pauseButton != null)
            pauseButton.onClick.AddListener(PauseGame);

        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);

        if (pauseRestartButton != null)
            pauseRestartButton.onClick.AddListener(RestartGame);

        if (reopenButton != null)
            reopenButton.onClick.AddListener(RestartGame);
    }

    private void UnbindButtons()
    {
        if (startButton != null)
            startButton.onClick.RemoveListener(StartGame);

        if (pauseButton != null)
            pauseButton.onClick.RemoveListener(PauseGame);

        if (resumeButton != null)
            resumeButton.onClick.RemoveListener(ResumeGame);

        if (pauseRestartButton != null)
            pauseRestartButton.onClick.RemoveListener(RestartGame);

        if (reopenButton != null)
            reopenButton.onClick.RemoveListener(RestartGame);
    }

    private void ShowHome()
    {
        State = GameState.Home;
        IsRunning = false;
        Time.timeScale = 1f;

        homePanel.SetActive(true);
        hudPanel.SetActive(false);
        pausePanel.SetActive(false);

        HideResult();

        if (bestScoreText != null)
            bestScoreText.text = $"Best: {BestScore}";
    }

    public void StartGame()
    {
        if (State == GameState.Playing)
            return;

        State = GameState.Playing;
        Time.timeScale = 1f;

        homePanel.SetActive(false);
        hudPanel.SetActive(true);
        pausePanel.SetActive(false);
        HideResult();

        ClearEnemies();
        ClearBullets();

        foreach (Tower tower in towers)
        {
            if (tower != null)
                tower.ResetTower();
        }

        if (playerSkill != null)
            playerSkill.ResetSkill();

        BaseHp = MaxBaseHp;
        Kills = 0;
        Score = 0;
        UpdateHud();

        elapsed = 0f;
        nextSpawnIndex = 0;
        spawnCursor = 1;
        lastShownSecond = -1;
        IsRunning = true;

        BuildSchedule();

        if (timeText != null)
        {
            timeText.gameObject.SetActive(true);
            timeText.text = Mathf.CeilToInt(Data.round.duration).ToString();
        }

        UpdateTimeText();
        Debug.Log("开始新一局");
    }

    public void PauseGame()
    {
        if (State != GameState.Playing)
            return;
        if (playerSkill != null)
            playerSkill.CancelAim();
        State = GameState.Paused;
        IsRunning = false;
        Time.timeScale = 0f;

        pausePanel.SetActive(true);
    }

    public void ResumeGame()
    {
        if (State != GameState.Paused)
            return;

        State = GameState.Playing;
        IsRunning = true;
        Time.timeScale = 1f;

        pausePanel.SetActive(false);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        StartGame();
    }

    private void Update()
    {
        // Esc 暂停/继续
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (State == GameState.Playing)
            {
                PauseGame();
                return;
            }

            if (State == GameState.Paused)
            {
                ResumeGame();
                return;
            }
        }

        if (State != GameState.Playing)
            return;

        // 限制单帧时间，避免切后台回来后时间跳变
        elapsed += Mathf.Min(Time.deltaTime, 0.05f);

        UpdateTimeText();
        SpawnDueEnemies();

        if (elapsed >= Data.round.duration)
            Win();
    }

    private void UpdateTimeText()
    {
        if (timeText == null)
            return;

        int seconds = Mathf.CeilToInt(
            Mathf.Max(0f, Data.round.duration - elapsed));

        if (seconds == lastShownSecond)
            return;

        lastShownSecond = seconds;
        timeText.text = seconds.ToString();
    }

    private void BuildSchedule()
    {
        schedule.Clear();

        foreach (WaveEntry wave in Data.waves)
        {
            if (wave == null)
                continue;

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
    }

    public void DamageBase(int damage)
    {
        if (State != GameState.Playing)
            return;

        BaseHp = Mathf.Max(0, BaseHp - damage);
        UpdateHud();

        if (BaseHp <= 0)
            Lose();
    }

    public void AddKill()
    {
        if (State != GameState.Playing)
            return;

        Kills++;
        Score += Data.score.perKill;
        UpdateHud();
    }

    public void RemoveEnemy(Enemy enemy)
    {
        enemies.Remove(enemy);
    }

    private void UpdateHud()
    {
        if (baseHpText != null)
            baseHpText.text = $"Base: {BaseHp}/{MaxBaseHp}";

        if (killNumText != null)
            killNumText.text = $"Kill: {Kills}";

        if (scoreNumText != null)
            scoreNumText.text = $"Score: {Score}";
    }

    private void Win()
    {
        if (State != GameState.Playing)
            return;

        State = GameState.Result;
        IsRunning = false;

        ShowResult(true);
    }

    private void Lose()
    {
        if (State != GameState.Playing)
            return;

        State = GameState.Result;
        IsRunning = false;

        ShowResult(false);
    }

    private void ShowResult(bool win)
    {
        if (playerSkill != null)
            playerSkill.CancelAim();

        Time.timeScale = 0f;

        homePanel.SetActive(false);
        hudPanel.SetActive(false);
        pausePanel.SetActive(false);
        tempPanel.SetActive(true);

        if (victoryText != null)
            victoryText.gameObject.SetActive(win);

        if (defeatText != null)
            defeatText.gameObject.SetActive(!win);

        int finalScore = Score
            + BaseHp * Data.score.perHp
            + (win ? Data.score.winBonus : 0);

        if (resultScoreText != null)
        {
            resultScoreText.gameObject.SetActive(true);
            resultScoreText.text = $"Result Score: {finalScore}";
        }

        if (finalScore > BestScore)
        {
            BestScore = finalScore;

            if (saveData == null)
                saveData = new SaveData();

            saveData.bestScore = BestScore;
            saveData.bestKills = Mathf.Max(saveData.bestKills, Kills);
            saveData.lastPlayedUtc = System.DateTime.UtcNow.ToString("o");

            GameDataService.SaveSave(saveData);
        }

        if (resultBestText != null)
        {
            resultBestText.gameObject.SetActive(true);
            resultBestText.text = $"Best: {BestScore}";
        }

        if (bestScoreText != null)
            bestScoreText.text = $"Best: {BestScore}";

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

        if (resultScoreText != null)
            resultScoreText.gameObject.SetActive(false);

        if (resultBestText != null)
            resultBestText.gameObject.SetActive(false);

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

    private void OnApplicationFocus(bool hasFocus)
    {
        if (Application.isEditor)
            return;

        if (!hasFocus)
            PauseGame();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            PauseGame();
    }

    private void OnDestroy()
    {
        UnbindButtons();
        Time.timeScale = 1f;
    }
}