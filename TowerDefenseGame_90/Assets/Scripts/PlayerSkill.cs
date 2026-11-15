using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayerSkill : MonoBehaviour
{
    [Header("场景引用")]
    [SerializeField] private Camera cam;
    [SerializeField] private Button skillButton;
    [SerializeField] private Image cooldownMask;
    [SerializeField] private TMP_Text skillText;

    private GameManager manager;
    private LineRenderer rangeRenderer;
    private GameObject rangeObject;

    private bool aiming;
    private float nextReadyTime;

    private void Start()
    {
        manager = GameManager.Instance;

        if (cam == null)
            cam = Camera.main;

        CreateRangeRing();

        if (skillButton != null)
            skillButton.onClick.AddListener(BeginAim);

        UpdateSkillUI();
    }

    private void Update()
    {
        if (manager == null || manager.Data == null)
            return;

        UpdateSkillUI();

        if (manager.State != GameManager.GameState.Playing)
        {
            if (aiming)
                CancelAim();

            return;
        }

        // 键盘测试用，UI 接好后可以保留也可以删
        if (Input.GetKeyDown(KeyCode.Q))
            BeginAim();

        if (!aiming)
            return;

        if (Input.GetMouseButtonDown(1))
        {
            CancelAim();
            return;
        }

        // 鼠标在 UI 上时，不当作战场点击
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
            return;

        if (TryGetMouseWorld(out Vector3 world))
        {
            ShowRange(world, manager.Data.skill.radius);

            if (Input.GetMouseButtonDown(0))
                Cast(world);
        }
    }

    public void BeginAim()
    {
        if (manager == null || manager.Data == null)
            return;

        if (manager.State != GameManager.GameState.Playing)
            return;

        if (Time.time < nextReadyTime)
            return;

        aiming = true;
        UpdateSkillUI();
    }

    public void CancelAim()
    {
        aiming = false;
        HideRange();
        UpdateSkillUI();
    }

    public void ResetSkill()
    {
        aiming = false;
        nextReadyTime = 0f;
        HideRange();
        UpdateSkillUI();
    }

    private void Cast(Vector3 center)
    {
        SkillSettings skill = manager.Data.skill;
        int hitCount = 0;

        // 倒序遍历，因为敌人死亡会从列表移除
        for (int i = manager.Enemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = manager.Enemies[i];
            if (enemy == null || !enemy.IsAlive)
                continue;

            Vector2 diff =
                (Vector2)enemy.transform.position -
                (Vector2)center;

            if (diff.sqrMagnitude <= skill.radius * skill.radius)
            {
                // 先记录位置，因为敌人可能被这一下打死
                Vector3 hitPos = enemy.transform.position;

                enemy.TakeDamage(skill.damage);

                // 命中反馈：敌人位置出现白色小圆环
                SkillEffect.Spawn(
                    hitPos,
                    0.55f,
                    Color.white,
                    0.15f);

                hitCount++;
            }
        }

        nextReadyTime = Time.time + skill.cooldown;
        aiming = false;

        HideRange();

        // 释放反馈：以技能中心扩散的橙色圆环
        SkillEffect.Spawn(
            center,
            skill.radius,
            new Color(1f, 0.65f, 0.1f, 1f),
            0.35f);

        UpdateSkillUI();

        Debug.Log($"Skill cast, hits: {hitCount}");
    }

    private void UpdateSkillUI()
    {
        if (manager == null || manager.Data == null)
            return;

        float cooldown = manager.Data.skill.cooldown;
        float remaining = Mathf.Max(0f, nextReadyTime - Time.time);

        bool ready =
            remaining <= 0f &&
            !aiming &&
            manager.State == GameManager.GameState.Playing;

        if (cooldownMask != null)
        {
            cooldownMask.fillAmount =
                cooldown > 0f ? remaining / cooldown : 0f;
        }

        if (skillButton != null)
            skillButton.interactable = ready;

        if (skillText != null)
            skillText.text = remaining > 0f
                ? Mathf.CeilToInt(remaining).ToString()
                : "Skill";
    }

    private bool TryGetMouseWorld(out Vector3 world)
    {
        world = Vector3.zero;

        if (cam == null)
            return false;

        world = cam.ScreenToWorldPoint(Input.mousePosition);
        world.z = 0f;

        // 限制在相机视野内，避免点到画面外
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        world.x = Mathf.Clamp(world.x, -halfWidth + 0.2f, halfWidth - 0.2f);
        world.y = Mathf.Clamp(world.y, -halfHeight + 0.2f, halfHeight - 0.2f);

        return true;
    }

    private void CreateRangeRing()
    {
        rangeObject = new GameObject("SkillRange");
        rangeObject.transform.SetParent(transform, false);

        rangeRenderer = rangeObject.AddComponent<LineRenderer>();
        rangeRenderer.useWorldSpace = true;
        rangeRenderer.loop = true;
        rangeRenderer.positionCount = 64;
        rangeRenderer.startWidth = 0.08f;
        rangeRenderer.endWidth = 0.08f;
        rangeRenderer.startColor = new Color(1f, 0.65f, 0.1f, 0.9f);
        rangeRenderer.endColor = new Color(1f, 0.65f, 0.1f, 0.9f);

        rangeRenderer.sortingLayerName = "Other";
        rangeRenderer.sortingOrder = 100;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        if (shader != null)
            rangeRenderer.material = new Material(shader);

        rangeRenderer.enabled = false;
    }

    private void ShowRange(Vector3 center, float radius)
    {
        if (rangeRenderer == null)
            return;

        rangeRenderer.enabled = true;

        const int segments = 64;

        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;

            Vector3 offset = new Vector3(
                Mathf.Cos(angle),
                Mathf.Sin(angle),
                0f) * radius;

            rangeRenderer.SetPosition(i, center + offset);
        }
    }

    private void HideRange()
    {
        if (rangeRenderer != null)
            rangeRenderer.enabled = false;
    }

    private void OnDestroy()
    {
        if (skillButton != null)
            skillButton.onClick.RemoveListener(BeginAim);

        if (rangeObject != null)
            Destroy(rangeObject);
    }
}