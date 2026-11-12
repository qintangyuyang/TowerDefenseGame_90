using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 游戏数据层：
/// - 用现有的 JsonMgr + LitJson 读取 game_data.json；
/// - 校验配置是否完整、数值是否合法、id 引用是否正确；
/// - 读写 save.json。
///
/// 这个类不继承 MonoBehaviour，不挂场景，不改场景对象。
/// </summary>
public static class GameDataService
{
    // JsonMgr 会自动补 .json，所以这里不要带扩展名。
    public const string ConfigFileName = "GameData/game_data";
    public const string SaveFileName = "save";

    // 加载成功后的配置；失败时为 null。
    public static GameData Config { get; private set; }

    // 最近一次错误，方便 UI 显示。
    public static string LastError { get; private set; }

    /// <summary>
    /// 读取并校验 game_data.json。
    /// 返回 false 时 Config 为 null，LastError 有原因。
    /// </summary>
    public static bool LoadConfig()
    {
        Config = null;
        LastError = null;

        try
        {
            // LitJson 会把 JSON 字段映射到 GameData 的 public 字段。
            GameData data = JsonMgr.Instance.LoadData<GameData>(
                ConfigFileName,
                JsonType.LitJson);

            // JsonMgr 在文件不存在时会返回 new GameData()，
            // 所以不能只判断 null，还要做完整校验。
            if (data == null)
            {
                LastError = "游戏数据为空：JsonMgr 返回了 null";
                Debug.LogError(LastError);
                return false;
            }

            if (!Validate(data, out string error))
            {
                LastError = error;
                Debug.LogError($"游戏数据校验失败：{error}");
                return false;
            }

            Config = data;
            Debug.Log("游戏数据加载成功：game_data.json");
            return true;
        }
        catch (Exception ex)
        {
            // JSON 语法错误、文件读取错误等都会进入这里。
            LastError = $"读取游戏数据失败：{ex.Message}";
            Debug.LogError(LastError);
            return false;
        }
    }

    /// <summary>
    /// 校验配置是否完整。
    /// 所有错误都通过 error 返回，方便 UI 显示。
    /// </summary>
    public static bool Validate(GameData data, out string error)
    {
        error = null;

        if (data == null)
        {
            error = "GameData 为 null";
            return false;
        }

        if (data.version != 1)
        {
            error = $"不支持的数据版本：{data.version}，当前只支持 1";
            return false;
        }

        if (data.round == null)
        {
            error = "缺少 round 配置";
            return false;
        }

        if (data.round.duration <= 0f)
        {
            error = "round.duration 必须大于 0";
            return false;
        }

        if (data.round.baseHp <= 0)
        {
            error = "round.baseHp 必须大于 0";
            return false;
        }

        if (data.score == null)
        {
            error = "缺少 score 配置";
            return false;
        }

        if (data.score.perKill < 0 || data.score.perHp < 0 || data.score.winBonus < 0)
        {
            error = "score 的分数不能为负数";
            return false;
        }

        if (data.skill == null)
        {
            error = "缺少 skill 配置";
            return false;
        }

        if (data.skill.damage < 0 || data.skill.radius <= 0f || data.skill.cooldown < 0f)
        {
            error = "skill 配置不合法";
            return false;
        }

        if (data.towers == null || data.towers.Count == 0)
        {
            error = "towers 不能为空";
            return false;
        }

        HashSet<string> towerIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (TowerDefinition tower in data.towers)
        {
            if (tower == null)
            {
                error = "towers 中存在空元素";
                return false;
            }

            if (string.IsNullOrWhiteSpace(tower.id))
            {
                error = "炮塔 id 不能为空";
                return false;
            }

            if (!towerIds.Add(tower.id))
            {
                error = $"炮塔 id 重复：{tower.id}";
                return false;
            }

            if (tower.range <= 0f)
            {
                error = $"炮塔 {tower.id} 的 range 必须大于 0";
                return false;
            }

            if (tower.damage < 0)
            {
                error = $"炮塔 {tower.id} 的 damage 不能为负数";
                return false;
            }

            if (tower.fireInterval <= 0f)
            {
                error = $"炮塔 {tower.id} 的 fireInterval 必须大于 0";
                return false;
            }

            if (tower.bulletSpeed <= 0f)
            {
                error = $"炮塔 {tower.id} 的 bulletSpeed 必须大于 0";
                return false;
            }

            if (tower.bulletLifeTime <= 0f)
            {
                error = $"炮塔 {tower.id} 的 bulletLifeTime 必须大于 0";
                return false;
            }
        }

        if (data.enemies == null || data.enemies.Count == 0)
        {
            error = "enemies 不能为空";
            return false;
        }

        HashSet<string> enemyIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (EnemyDefinition enemy in data.enemies)
        {
            if (enemy == null)
            {
                error = "enemies 中存在空元素";
                return false;
            }

            if (string.IsNullOrWhiteSpace(enemy.id))
            {
                error = "敌人 id 不能为空";
                return false;
            }

            if (!enemyIds.Add(enemy.id))
            {
                error = $"敌人 id 重复：{enemy.id}";
                return false;
            }

            if (string.IsNullOrWhiteSpace(enemy.displayName))
            {
                error = $"敌人 {enemy.id} 的 displayName 不能为空";
                return false;
            }

            if (enemy.maxHp <= 0f)
            {
                error = $"敌人 {enemy.id} 的 maxHp 必须大于 0";
                return false;
            }

            if (enemy.moveSpeed <= 0f)
            {
                error = $"敌人 {enemy.id} 的 moveSpeed 必须大于 0";
                return false;
            }

            if (enemy.baseDamage < 0)
            {
                error = $"敌人 {enemy.id} 的 baseDamage 不能为负数";
                return false;
            }

            if (enemy.attackInterval <= 0f)
            {
                error = $"敌人 {enemy.id} 的 attackInterval 必须大于 0";
                return false;
            }

            if (enemy.attackRange < 0f)
            {
                error = $"敌人 {enemy.id} 的 attackRange 不能小于 0";
                return false;
            }

            if (enemy.scale <= 0f)
            {
                error = $"敌人 {enemy.id} 的 scale 必须大于 0";
                return false;
            }

            if (string.IsNullOrWhiteSpace(enemy.color) ||
                !ColorUtility.TryParseHtmlString(enemy.color, out _))
            {
                error = $"敌人 {enemy.id} 的 color 不是合法颜色：{enemy.color}";
                return false;
            }
        }

        if (data.waves == null || data.waves.Count == 0)
        {
            error = "waves 不能为空";
            return false;
        }

        foreach (WaveEntry wave in data.waves)
        {
            if (wave == null)
            {
                error = "waves 中存在空元素";
                return false;
            }

            if (!enemyIds.Contains(wave.enemyId))
            {
                error = $"刷怪表引用了不存在的敌人 id：{wave.enemyId}";
                return false;
            }

            if (wave.startTime < 0f)
            {
                error = $"刷怪表 {wave.enemyId} 的 startTime 不能为负数";
                return false;
            }

            if (wave.count <= 0)
            {
                error = $"刷怪表 {wave.enemyId} 的 count 必须大于 0";
                return false;
            }

            if (wave.interval <= 0f)
            {
                error = $"刷怪表 {wave.enemyId} 的 interval 必须大于 0";
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 读取 save.json。
    /// 没有存档、存档损坏时返回一个可用的默认对象。
    /// </summary>
    public static SaveData LoadSave()
    {
        try
        {
            SaveData data = JsonMgr.Instance.LoadData<SaveData>(
                SaveFileName,
                JsonType.LitJson);

            if (data == null)
                return new SaveData();

            // 简单清洗，防止手改出负数。
            data.version = Mathf.Max(1, data.version);
            data.bestScore = Mathf.Max(0, data.bestScore);
            data.bestKills = Mathf.Max(0, data.bestKills);

            return data;
        }
        catch (Exception ex)
        {
            LastError = $"读取存档失败：{ex.Message}";
            Debug.LogError(LastError);
            return new SaveData();
        }
    }

    /// <summary>
    /// 写入 save.json。
    /// </summary>
    public static bool SaveSave(SaveData data)
    {
        if (data == null)
        {
            LastError = "SaveData 为 null，无法保存";
            Debug.LogError(LastError);
            return false;
        }

        try
        {
            data.version = 1;
            JsonMgr.Instance.SaveData(data, SaveFileName, JsonType.LitJson);
            return true;
        }
        catch (Exception ex)
        {
            LastError = $"写入存档失败：{ex.Message}";
            Debug.LogError(LastError);
            return false;
        }
    }
}
