using System;
using System.Collections.Generic;

/// <summary>
/// game_data.json 的根对象。
/// 所有玩法数值都从 JSON 读取，场景 Inspector 不再保存第二份。
/// 使用 LitJson 反序列化，所以字段必须是 public。
/// </summary>
[Serializable]
public class GameData
{
    // 数据版本。当前只支持 1。
    public int version = 1;

    // 单局规则：时长、基地血量。
    public RoundSettings round;

    // 计分规则。
    public ScoreSettings score;

    // 主动技能参数。
    public SkillSettings skill;

    // 炮塔/子弹参数。Tower.towerId 按 id 匹配。
    public List<TowerDefinition> towers;

    // 敌人参数。EnemySpawner/GameManager 通过 id 查找。
    public List<EnemyDefinition> enemies;

    // 刷怪表。
    public List<WaveEntry> waves;
}

[Serializable]
public class RoundSettings
{
    // 一局多少秒，必须大于 0。
    public float duration;

    // 基地最大血量，必须大于 0。
    public int baseHp;
}

[Serializable]
public class ScoreSettings
{
    // 每击杀一个敌人加多少分。
    public int perKill;

    // 结算时每点剩余基地血量加多少分。
    public int perHp;

    // 胜利额外加多少分。
    public int winBonus;
}

[Serializable]
public class SkillSettings
{
    // 技能对范围内每个敌人的伤害。
    public int damage;

    // 技能范围半径，世界单位。
    public float radius;

    // 技能冷却秒数。
    public float cooldown;
}

[Serializable]
public class TowerDefinition
{
    // 炮塔配置 ID，对应场景 Tower.towerId。
    public string id;

    // 索敌半径。
    public float range;

    // 单次伤害。
    public int damage;

    // 攻击间隔，秒。
    public float fireInterval;

    // 子弹速度。
    public float bulletSpeed;

    // 子弹最大存活时间。
    public float bulletLifeTime;
}

[Serializable]
public class EnemyDefinition
{
    // 敌人唯一 ID，刷怪表通过它引用。
    public string id;

    // 显示名，也用于运行时 GameObject 名字。
    public string displayName;

    // 最大生命值。
    public float maxHp;

    // 移动速度，单位/秒。
    public float moveSpeed;

    // 每次攻击基地造成的伤害。
    public int baseDamage;

    // 攻击间隔，秒。
    public float attackInterval;

    // 距离基地多远开始攻击。
    public float attackRange;

    // 是否远程。当前作为标记，后续可生成敌方子弹。
    public bool ranged;

    // SpriteRenderer 颜色，十六进制，例如 #FF4D4D。
    public string color;

    // 视觉缩放，用于同一个 Prefab 区分大小。
    public float scale;
}

[Serializable]
public class WaveEntry
{
    // 这一组从第几秒开始刷。
    public float startTime;

    // 引用 EnemyDefinition.id。
    public string enemyId;

    // 这一组刷多少只。
    public int count;

    // 每只之间的间隔秒数。
    public float interval;
}
