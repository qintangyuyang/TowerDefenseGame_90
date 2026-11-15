using UnityEngine;

public class SkillEffect : MonoBehaviour
{
    private LineRenderer ring;
    private float duration;
    private float elapsed;
    private float maxRadius;
    private Color color;

    /// <summary>
    /// 在指定位置生成一个扩散圆环。
    /// </summary>
    public static void Spawn(
        Vector3 center,
        float radius,
        Color effectColor,
        float effectDuration = 0.35f)
    {
        GameObject go = new GameObject("SkillEffect");
        go.transform.position = center;

        SkillEffect effect = go.AddComponent<SkillEffect>();
        effect.Init(radius, effectColor, effectDuration);
    }

    private void Init(float radius, Color effectColor, float effectDuration)
    {
        maxRadius = radius;
        color = effectColor;
        duration = Mathf.Max(0.05f, effectDuration);

        ring = gameObject.AddComponent<LineRenderer>();
        ring.useWorldSpace = true;
        ring.loop = true;
        ring.positionCount = 64;
        ring.startWidth = 0.12f;
        ring.endWidth = 0.02f;
        ring.startColor = color;
        ring.endColor = color;

        // 和范围圈同一层，保证在 Tilemap 上方
        ring.sortingLayerName = "Other";
        ring.sortingOrder = 101;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        if (shader != null)
            ring.material = new Material(shader);

        Draw(0f);
    }

    private void Update()
    {
        elapsed += Time.deltaTime;

        float t = Mathf.Clamp01(elapsed / duration);
        Draw(t);

        // 一边扩散一边淡出
        Color c = color;
        c.a = 1f - t;
        ring.startColor = c;
        ring.endColor = c;

        if (t >= 1f)
            Destroy(gameObject);
    }

    private void Draw(float t)
    {
        float radius = Mathf.Lerp(0f, maxRadius, t);
        Vector3 center = transform.position;

        const int segments = 64;

        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;

            Vector3 offset = new Vector3(
                Mathf.Cos(angle),
                Mathf.Sin(angle),
                0f) * radius;

            ring.SetPosition(i, center + offset);
        }
    }

    private void OnDestroy()
    {
        if (ring != null && ring.material != null)
            Destroy(ring.material);
    }
}