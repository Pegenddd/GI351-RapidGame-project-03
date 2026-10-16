using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Base class for enemies placed directly in the scene.
/// Handles chasing the player, attacking with a pause, health, and hit flash effect using Material.
/// </summary>
public class EnemyBase : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Movement & Combat Settings")]
    public float moveSpeed = 4f;                 // ความเร็วในการวิ่งไล่ตามผู้เล่น
    public float attackRange = 1.8f;              // ระยะประชิดที่ศัตรูจะเริ่มโจมตี
    public float attackPauseDuration = 0.8f;      // ระยะเวลาที่หยุดยืนรอก่อนไล่ต่อหลังโจมตี
    public float attackDamage = 10f;              // ดาเมจที่ทำใส่ผู้เล่น

    private bool isAttackingPause = false;
    private float pauseTimer = 0f;

    [Header("Visual Feedback (Flash)")]
    public Color flashColor = Color.white;        // สีตอนโดนตี (สีกระพริบ)
    public float flashDuration = 0.1f;            // ระยะเวลาที่กระพริบ

    private Renderer meshRenderer;
    private MaterialPropertyBlock propBlock;
    private Transform playerTransform;
    private bool isDead = false;

    // ตัวแปรสำหรับเก็บค่าสโลว์ของศัตรู
    private float originalMoveSpeed = -1f;
    private float slowTimer = 0f;
    private bool isSlowed = false;

    // ตัวแปรเช็กสถานะกำลังโดนแรงดึงดูดของเสาพินบอล
    private bool isBeingPulled = false;

    // Overhead Health UI
    [Header("UI Settings")]
    [Tooltip("แสดงหลอดเลือดบนหัวศัตรู")]
    public bool showHealthBar = true;

    private Slider healthSlider;
    private GameObject healthCanvasObj;

    private void Awake()
    {
        currentHealth = maxHealth;

        meshRenderer = GetComponentInChildren<Renderer>();
        propBlock = new MaterialPropertyBlock();

        if (showHealthBar)
        {
            CreateOverheadHealthBar();
        }
    }

    private void Start()
    {
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        if (pc != null)
        {
            playerTransform = pc.transform;
        }
    }

    private void Update()
    {
        if (isDead) return;

        // เรียกใช้งานระบบจับเวลาสโลว์ในทุกๆ เฟรม
        HandleSlowTimer();

        UpdateMovementAndCombat();
        UpdateHealthBarTransform();
    }

    private void UpdateMovementAndCombat()
    {
        // ถ้ากำลังโดนแรงดึงดูดของเสาอยู่ จะไม่ให้ AI วิ่งเดินเองตามปกติเพื่อไม่ให้ขัดแย้งกัน
        if (isBeingPulled) return;

        if (playerTransform == null) return;

        // ถ้าอยู่ในสถานะหยุดจังหวะหลังโจมตี
        if (isAttackingPause)
        {
            pauseTimer -= Time.deltaTime;
            if (pauseTimer <= 0f)
            {
                isAttackingPause = false; // หมดเวลาหยุด กลับมาวิ่งไล่ต่อ
            }
            return;
        }

        // คำนวณระยะห่างระหว่างศัตรูกับผู้เล่น
        Vector3 toPlayer = playerTransform.position - transform.position;
        toPlayer.y = 0;
        float distToPlayer = toPlayer.magnitude;

        // ถ้ายังไม่ถึงระยะประชิด ให้วิ่งไล่ตาม
        if (distToPlayer > attackRange)
        {
            Vector3 moveDir = toPlayer.normalized;

            // เช็กสิ่งกีดขวางด้านหน้าก่อนขยับตัว (ป้องกันการเดินทะลุวัตถุพิเศษ)
            float raycastDist = moveSpeed * Time.deltaTime + 0.3f;
            if (!Physics.Raycast(transform.position + Vector3.up * 0.5f, moveDir, raycastDist))
            {
                transform.position += moveDir * moveSpeed * Time.deltaTime;
            }

            // หันหน้าเข้าหาผู้เล่น
            Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 12f * Time.deltaTime);
        }
        else
        {
            // ประชิดตัวแล้ว -> ทำการโจมตีผู้เล่น
            AttackPlayer();
        }
    }

    private void AttackPlayer()
    {
        // สั่งหักเลือดผู้เล่น
        if (playerTransform != null)
        {
            PlayerController pc = playerTransform.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.TakeDamage(attackDamage);
            }
        }

        // เข้าสู่สถานะหยุด 1 จังหวะ
        isAttackingPause = true;
        pauseTimer = attackPauseDuration;
    }

    /// <summary>
    /// เรียกใช้งานเมื่อลูกบอลวิ่งมาชนหรือทะลุผ่านศัตรู
    /// </summary>
    public void TakeDamage(float damageAmount, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (isDead) return;

        currentHealth = Mathf.Max(0, currentHealth - damageAmount);

        // Update หลอดเลือดบนหัว
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth / maxHealth;
        }

        // เอฟเฟกต์เลือดสาด
        SpawnBloodSplatter(hitPoint, hitDirection);

        // ตัวเลขดาเมจลอยขึ้นมา
        SpawnDamageNumber(damageAmount, hitPoint);

        // สีกระพริบตอนโดนตี (ใช้ Material สีหลักแต่แฟลชทับชั่วคราว)
        TriggerFlashEffect();

        // เสียงตอนโดนตี
        if (SoundEffects.Instance != null)
        {
            SoundEffects.Instance.PlayEnemyHurt();
        }

        // เช็คว่าเลือดหมดหรือยัง
        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void TriggerFlashEffect()
    {
        if (meshRenderer != null)
        {
            CancelInvoke(nameof(ResetFlash));
            SetRendererColor(flashColor);
            Invoke(nameof(ResetFlash), flashDuration);
        }
    }

    private void SetRendererColor(Color color)
    {
        if (meshRenderer != null)
        {
            meshRenderer.GetPropertyBlock(propBlock);
            propBlock.SetColor("_BaseColor", color); // URP
            propBlock.SetColor("_Color", color);     // Legacy
            meshRenderer.SetPropertyBlock(propBlock);
        }
    }

    private void ResetFlash()
    {
        if (!isDead && meshRenderer != null)
        {
            // ล้าง PropertyBlock เพื่อให้ Material ดั้งเดิมกลับมาแสดงผลตามปกติ
            meshRenderer.SetPropertyBlock(null);
        }
    }

    private void SpawnBloodSplatter(Vector3 point, Vector3 direction)
    {
        GameObject bloodObj = new GameObject("BloodParticles");
        bloodObj.transform.position = point;
        bloodObj.transform.rotation = Quaternion.LookRotation(direction + Vector3.up * 0.5f);

        ParticleSystem ps = bloodObj.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = bloodObj.GetComponent<ParticleSystemRenderer>();
        psr.material = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"));

        var main = ps.main;
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.85f, 0.05f, 0.05f), new Color(0.55f, 0.0f, 0.0f));
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 8f);
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.6f);
        main.gravityModifier = 2.5f;
        main.loop = false;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 25) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 35f;
        shape.radius = 0.2f;

        ps.Play();
        Destroy(bloodObj, 1.2f);
    }

    private void SpawnDamageNumber(float damage, Vector3 worldPos)
    {
        GameObject textObj = new GameObject("DamagePopup");
        textObj.transform.position = worldPos + Vector3.up * 1.5f;

        TextMesh tm = textObj.AddComponent<TextMesh>();
        tm.text = $"-{Mathf.RoundToInt(damage)}";
        tm.fontSize = 28;
        tm.color = new Color(1f, 0.9f, 0.2f);
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.characterSize = 0.12f;

        DamagePopupAnimator anim = textObj.AddComponent<DamagePopupAnimator>();
        anim.Init(tm);
    }

    private void Die()
    {
        isDead = true;

        if (SoundEffects.Instance != null)
        {
            SoundEffects.Instance.PlayEnemyDie();
        }

        SpawnDeathEffect();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnEnemyDefeated(transform.position);
        }

        Destroy(gameObject);
    }

    private void SpawnDeathEffect()
    {
        GameObject explosionObj = new GameObject("DeathExplosion");
        explosionObj.transform.position = transform.position;

        ParticleSystem ps = explosionObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.9f, 0.1f, 0.1f), new Color(0.3f, 0.0f, 0.0f));
        main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 11f);
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
        main.gravityModifier = 2f;
        main.loop = false;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 45) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;

        ps.Play();
        Destroy(explosionObj, 1.5f);
    }

    private void CreateOverheadHealthBar()
    {
        healthCanvasObj = new GameObject("EnemyHealthCanvas");
        healthCanvasObj.transform.SetParent(transform);
        healthCanvasObj.transform.localPosition = new Vector3(0, 2.2f, 0);

        Canvas canvas = healthCanvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        RectTransform canvasRect = healthCanvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(1.2f, 0.2f);
        canvasRect.localScale = Vector3.one;

        // Background
        GameObject bgObj = new GameObject("BarBackground");
        bgObj.transform.SetParent(healthCanvasObj.transform, false);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        // Slider
        GameObject sliderObj = new GameObject("HealthSlider");
        sliderObj.transform.SetParent(healthCanvasObj.transform, false);
        healthSlider = sliderObj.AddComponent<Slider>();
        RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
        sliderRect.anchorMin = Vector2.zero;
        sliderRect.anchorMax = Vector2.one;
        sliderRect.sizeDelta = Vector2.zero;

        // Fill area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = Vector2.zero;

        // Fill
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.9f, 0.2f, 0.2f);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;

        healthSlider.fillRect = fillRect;
        healthSlider.minValue = 0f;
        healthSlider.maxValue = 1f;
        healthSlider.value = 1f;
    }

    private void UpdateHealthBarTransform()
    {
        if (healthCanvasObj != null && Camera.main != null)
        {
            healthCanvasObj.transform.rotation = Camera.main.transform.rotation;
        }
    }

    // ฟังก์ชันรับค่าสโลว์จาก BouncingBall เมื่อลูกบอลน้ำแข็งพุ่งชน
    public void ApplySlowEffect(float slowFactor, float duration)
    {
        if (!isSlowed)
        {
            originalMoveSpeed = moveSpeed; // บันทึกความเร็วเดิมไว้
            moveSpeed *= slowFactor;        // สั่งชะลอความเร็ว
        }

        slowTimer = duration;
        isSlowed = true;
    }

    // ฟังก์ชันจับเวลาถอยหลังเพื่อคืนค่าความเร็วเดิมของศัตรู
    private void HandleSlowTimer()
    {
        if (isSlowed)
        {
            slowTimer -= Time.deltaTime;
            if (slowTimer <= 0f)
            {
                moveSpeed = originalMoveSpeed; // หมดเวลาสโลว์ คืนค่าความเร็วเดิม
                isSlowed = false;
            }
        }
    }

    // ฟังก์ชันสำหรับเปิด-ปิดสถานะถูกแรงดึงดูดของเสาพินบอลครอบงำ
    public void SetPulledState(bool pulled)
    {
        isBeingPulled = pulled;
    }
}