using System;

/// <summary>
/// save.json 的结构。
/// 通过 JsonMgr 写到 Application.persistentDataPath。
/// 字段名就是 JSON 字段名，以后改字段要考虑旧存档迁移。
/// </summary>
[Serializable]
public class SaveData
{
    // 存档版本，当前为 1。
    public int version = 1;

    // 历史最高分。
    public int bestScore;

    // 历史最高击杀数，方便以后扩展展示。
    public int bestKills;

    // 最后一次游玩时间，ISO 8601 字符串。
    public string lastPlayedUtc = "";
}
