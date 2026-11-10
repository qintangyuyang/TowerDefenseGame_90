using System.Collections;
using System.Collections.Generic;
using System.IO;
using LitJson;
using UnityEngine;

/// <summary>
/// 序列化和反序列化Json时，使用的是那种Json
/// </summary>
public enum JsonType
{
    JsonUtility,
    LitJson,
}
/// <summary>
/// Json数据管理类 主要用于进行 Json的序列化和反序列化从硬盘中读取到内存中
/// </summary>
public class JsonMgr 
{
    private static JsonMgr instance;

    public static JsonMgr Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new JsonMgr();
            }

            return instance;
        }
    }
    private JsonMgr(){}

    /// <summary>
    /// 存储Json 序列化
    /// </summary>
    public void SaveData(object data, string fileName, JsonType jsonType = JsonType.LitJson)
    {
        //确定存储路径
        string path = Application.persistentDataPath + "/" + fileName + ".json";
        //序列化 得到Json字符串
        string jsonStr = "";
        switch (jsonType)
        {
            case JsonType.LitJson:
                jsonStr = JsonMapper.ToJson(data);
                break;
            case JsonType.JsonUtility:
                jsonStr = JsonUtility.ToJson(data);
                break;
        }
        //把序列化的Json字符串存储到指定路径的文件中
        File.WriteAllText(path, jsonStr);
    }

    /// <summary>
    /// 用于读取指定文件中的 Json数据 用于反序列化
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="jsonType"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T LoadData<T>(string fileName, JsonType jsonType = JsonType.LitJson) where T :new()
    {
        //确定从哪个路径读取
        //先判断默认数据文件夹中是否有我们想要的数据 如果有 就从中获取
        string path = Application.streamingAssetsPath + "/" + fileName + ".json";
        //先判断是否存在这个文件
        //如果不存在默认文件 就从 读写文件夹中去寻找
        if (!File.Exists(path))
        {
            path = Application.persistentDataPath + "/" + fileName + ".json";
        }
        //如果读写文件夹中还没有 返回一个默认对象
        if (!File.Exists(path))
        {
            return new T();
        }

        //进行反序列化
        string jsonStr = File.ReadAllText(path);

        T data = default(T);
        switch (jsonType)
        {
            case JsonType.LitJson:
                data = JsonMapper.ToObject<T>(jsonStr);
                break;
            case JsonType.JsonUtility:
                data = JsonUtility.FromJson<T>(jsonStr);
                break;
        }

        //把对象返回出去
        return data;
    }
}
