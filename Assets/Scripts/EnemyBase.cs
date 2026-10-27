using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Base class for enemies placed directly in the scene.
/// Handles chasing the player, attacking with a telegraph area, death explosion, and slow/pull states.
/// </summary>
public class EnemyBase : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Movement & Combat Settings")]
    public float moveSpeed = 4f;                 // ความเร็วในการวิ่งไล่ตามผู้เล่น
    public float attackRange = 2.0f;              // ระยะประชิดที่ศัตรูจะเริ่มหยุดเพื่อร่ายสกิลโจมตีพื้น (ปรับให้แคบลงเพื่อจะได้วิ่งเข้าหาก่อน)
    public float attackCooldown = 3.0f;         // คูลดาวน์ระหว่างการโจมตีแต่ละครั้ง
    public float attackDamage = 15f;              // ดาเมจที่ทำใส่ผู้เล่น

    private float lastAttackTime = -999f;
    private bool isAttacking = false;

    [Header("Death Explosion Settings")]
    [Tooltip("เปิดใช้งานระเบิดตัวเองเมื่อตายเพื่อทำดาเมจใส่ศัตรูรอบข้าง")]
    public bool enableDeathExplosion = true;
    public float deathExplosionRadius = 4.0f;     // รัศมีแรงระเบิดตอนตาย
    public float deathExplosionDamage = 50f;      // ดาเมจระเบิดตอนตาย

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
        // ถ้ากำลังโดนแรงดึงดูดของเสา หรือกำลังร่ายสกิลโจมตีอยู่ จะไม่ให้เดินเองปกติ
        if (isBeingPulled || isAttacking) return;

        if (playerTransform == null) return;

        // คำนวณระยะห่างระหว่างศัตรูกับผู้เล่น
        Vector3 toPlayer = playerTransform.position - transform.position;
        toPlayer.y = 0;
        float distToPlayer = toPlayer.magnitude;

        // เช็คว่าอยู่ในระยะโจมตีและพ้นคูลดาวน์หรือยัง
        if (distToPlayer <= attackRange && Time.time >= lastAttackTime + attackCooldown)
        {
            StartCoroutine(PerformTelegraphAttack());
        }
        else if (distToPlayer > attackRange)
        {
            // 🌟 วิ่งไล่ตามผู้เล่นอย่างต่อเนื่องเมื่ออยู่นอกระยะโจมตี
            Vector3 moveDir = toPlayer.normalized;

            float raycastDist = moveSpeed * Time.deltaTime + 0.3f;
            if (!Physics.Raycast(transform.position + Vector3.up * 0.5f, moveDir, raycastDist))
            {
                transform.position += moveDir * moveSpeed * Time.deltaTime;
            }

            // หันหน้าเข้าหาผู้เล่น
            Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 12f * Time.deltaTime);
        }
    }

    /// <summary>
    /// ระบบโจมตีแบบแสดงพื้นที่เตือนสีแดง (Telegraph) ให้ผู้เล่นหลบ
    /// </summary>
    private System.Collections.IEnumerator PerformTelegraphAttack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        // จุดศูนย์กลางพื้นที่โจมตี (ตำแหน่งผู้เล่นขณะเริ่มร่าย)
        Vector3 targetPos = playerTransform != null ? playerTransform.position : transform.position + transform.forward * 3f;
        targetPos.y = 0.05f; // ให้ลอยเหนือพื้นเล็กน้อยกัน Z-fighting

        float telegraphDuration = 1.2f; // เวลารอให้ผู้เล่นหลบ (วินาที)
        float boxSize = 3.5f;           // ขนาดพื้นที่สี่เหลี่ยม

        // สร้างสี่เหลี่ยมสีแดงเตือนบนพื้น (Telegraph Box)
        GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
        indicator.name = "AttackTelegraphBox";
        indicator.transform.position = targetPos;
        indicator.transform.localScale = new Vector3(boxSize, 0.02f, boxSize);

        // ปิด Collider ของตัวกล่องเตือนไม่ให้เกะกะการชน
        Collider col = indicator.GetComponent<Collider>();
        if (col != null) Destroy(col);

        // ตั้งค่า Material ให้เป็นสีแดงกึ่งโปร่งใส
        Renderer indRenderer = indicator.GetComponent<Renderer>();
        Material indMat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"));
        indMat.color = new Color(1f, 0f, 0f, 0.35f);
        indRenderer.material = indMat;

        float elapsed = 0f;
        while (elapsed < telegraphDuration)
        {
            elapsed += Time.deltaTime;
            // ทำให้สีแดงกระพริบเตือนความถี่สูงขึ้นเรื่อยๆ ก่อนระเบิด
            float alpha = Mathf.Lerp(0.2f, 0.7f, Mathf.PingPong(elapsed * 6f, 1f));
            indMat.color = new Color(1f, 0f, 0f, alpha);
            yield return null;
        }

        // --- ทำงานเมื่อครบเวลาเตือน (สร้างความเสียหายในพื้นที่) ---
        Collider[] hits = Physics.OverlapBox(targetPos, new Vector3(boxSize * 0.5f, 2f, boxSize * 0.5f), Quaternion.identity);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player") || hit.GetComponent<PlayerController>() != null)
            {
                PlayerController pc = hit.GetComponent<PlayerController>() ?? hit.GetComponentInParent<PlayerController>();
                if (pc != null)
                {
                    pc.TakeDamage(attackDamage);
                }
            }
        }

        // ลบกล่องเตือนบนพื้นทิ้ง
        Destroy(indicator);

        // หน่วงเวลาหลังโจมตีเสร็จเล็กน้อยก่อนกลับมาเดินไล่ล่าต่อ
        yield return new WaitForSeconds(0.5f);
        isAttacking = false;
    }

    /// <summary>
    /// เรียกใช้งานเมื่อลูกบอลวิ่งมาชนหรือทะลุผ่านศัตรู
    /// </summary>
    public void TakeDamage(float damageAmount, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (isDead) return;

        currentHealth = Mathf.Max(0, currentHealth - damageAmount);

        if (healthSlider != null)
        {
            healthSlider.value = currentHealth / maxHealth;
        }

        SpawnBloodSplatter(hitPoint, hitDirection);
        SpawnDamageNumber(damageAmount, hitPoint);
        TriggerFlashEffect();

        if (SoundEffects.Instance != null)
        {
            SoundEffects.Instance.PlayEnemyHurt();
        }

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
            propBlock.SetColor("_BaseColor", color);
            propBlock.SetColor("_Color", color);
            meshRenderer.SetPropertyBlock(propBlock);
        }
    }

    private void ResetFlash()
    {
        if (!isDead && meshRenderer != null)
        {
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
        if (isDead) return;
        isDead = true;

        // ระบบระเบิดตัวเองตอนตาย เพื่อทำดาเมจใส่ศัตรูรอบข้าง
        if (enableDeathExplosion)
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, deathExplosionRadius);
            foreach (var col in colliders)
            {
                EnemyBase nearbyEnemy = col.GetComponentInParent<EnemyBase>() ?? col.GetComponent<EnemyBase>();
                if (nearbyEnemy != null && nearbyEnemy != this)
                {
                    nearbyEnemy.TakeDamage(deathExplosionDamage, nearbyEnemy.transform.position, (nearbyEnemy.transform.position - transform.position).normalized);
                }
            }
        }

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

        GameObject bgObj = new GameObject("BarBackground");
        bgObj.transform.SetParent(healthCanvasObj.transform, false);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        GameObject sliderObj = new GameObject("HealthSlider");
        sliderObj.transform.SetParent(healthCanvasObj.transform, false);
        healthSlider = sliderObj.AddComponent<Slider>();
        RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
        sliderRect.anchorMin = Vector2.zero;
        sliderRect.anchorMax = Vector2.one;
        sliderRect.sizeDelta = Vector2.zero;

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = Vector2.zero;

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
            originalMoveSpeed = moveSpeed;
            moveSpeed *= slowFactor;
        }

        slowTimer = duration;
        isSlowed = true;
    }

    private void HandleSlowTimer()
    {
        if (isSlowed)
        {
            slowTimer -= Time.deltaTime;
            if (slowTimer <= 0f)
            {
                moveSpeed = originalMoveSpeed;
                isSlowed = false;
            }
        }
    }

    public void SetPulledState(bool pulled)
    {
        isBeingPulled = pulled;
    }

    // วาดเส้นขอบวงกลมแสดงรัศมีระเบิดตอนตายใน Scene View
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, deathExplosionRadius);
    }
}