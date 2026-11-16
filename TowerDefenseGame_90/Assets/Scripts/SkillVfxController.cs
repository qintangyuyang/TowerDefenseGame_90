using UnityEngine;

public class SkillVfxController : MonoBehaviour
{
    [SerializeField] private Shader ringShader;

    private GameManager manager;
    private Material material;
    private MaterialPropertyBlock block;

    private MeshRenderer castRenderer;
    private MeshRenderer castRenderer2;

    private bool casting;
    private bool highQuality;
    private float castTime;
    private float castDuration;
    private float castRadius;
    private Vector3 castCenter;

    private void Start()
    {
        manager = GameManager.Instance;

        if (ringShader == null)
            ringShader = Shader.Find("TD90/SkillRing");

        if (ringShader == null)
        {
            Debug.LogError("SkillVfxController: 找不到 TD90/SkillRing");
            enabled = false;
            return;
        }

        material = new Material(ringShader);
        block = new MaterialPropertyBlock();

        castRenderer = CreateQuad("SkillCastRing", 100);
        castRenderer2 = CreateQuad("SkillCastRing2", 101);
    }

    private void Update()
    {
        // 方便演示两档效果
        if (Input.GetKeyDown(KeyCode.F1) &&
            manager != null &&
            manager.Data != null &&
            manager.Data.skill.vfx != null)
        {
            manager.Data.skill.vfx.highQuality =
                !manager.Data.skill.vfx.highQuality;

            Debug.Log($"VFX High Quality: {manager.Data.skill.vfx.highQuality}");
        }

        if (!casting || manager == null || manager.Data == null)
            return;

        SkillVfxSettings vfx = manager.Data.skill.vfx;
        if (vfx == null)
            return;

        castTime += Time.deltaTime;

        float t = Mathf.Clamp01(castTime / castDuration);

        SetQuad(castRenderer, t, 1f - t, vfx, 0f);

        if (highQuality)
        {
            SetQuad(
                castRenderer2,
                Mathf.Repeat(t * 1.35f, 1f),
                1f - t,
                vfx,
                45f);
        }

        if (t >= 1f)
        {
            casting = false;
            castRenderer.gameObject.SetActive(false);
            castRenderer2.gameObject.SetActive(false);
        }
    }

    public void PlayCast(Vector3 center, float radius)
    {
        if (manager == null || manager.Data == null)
            return;

        SkillVfxSettings vfx = manager.Data.skill.vfx;
        if (vfx == null)
            return;

        castCenter = center;
        castRadius = radius;
        castDuration = Mathf.Max(0.05f, vfx.duration);
        castTime = 0f;
        casting = true;
        highQuality = vfx.highQuality;

        castRenderer.gameObject.SetActive(true);
        castRenderer2.gameObject.SetActive(highQuality);

        SetQuad(castRenderer, 0f, 1f, vfx, 0f);

        if (highQuality)
            SetQuad(castRenderer2, 0f, 0.7f, vfx, 45f);
    }

    private void SetQuad(
        MeshRenderer renderer,
        float progress,
        float alpha,
        SkillVfxSettings vfx,
        float angleOffset)
    {
        if (renderer == null)
            return;

        Transform tr = renderer.transform;
        tr.position = castCenter;
        tr.rotation = Quaternion.Euler(0f, 0f, angleOffset);
        tr.localScale = Vector3.one * castRadius * 2f;

        Color color;
        if (!ColorUtility.TryParseHtmlString(vfx.color, out color))
            color = new Color(1f, 0.65f, 0.1f, 1f);

        float intensity = highQuality
            ? vfx.intensity
            : vfx.intensity * 0.65f;

        block.Clear();
        block.SetColor("_Color", color);
        block.SetFloat("_Progress", progress);
        block.SetFloat("_RingWidth", vfx.ringWidth);
        block.SetFloat("_Intensity", intensity);
        block.SetFloat("_Alpha", alpha);
        block.SetFloat("_NoiseScale", highQuality ? 8f : 0f);

        renderer.SetPropertyBlock(block);
    }

    private MeshRenderer CreateQuad(string name, int sortingOrder)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = name;
        go.transform.SetParent(transform, false);

        Collider collider = go.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        MeshRenderer renderer = go.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.sortingLayerName = "Other";
        renderer.sortingOrder = sortingOrder;

        go.SetActive(false);
        return renderer;
    }

    private void OnDestroy()
    {
        if (material != null)
            Destroy(material);
    }
}