using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyBase : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Movement & Combat Settings")]
    public float moveSpeed = 4f;
    public float attackRange = 2.0f;
    public float attackCooldown = 3.0f;
    public float attackDamage = 15f;
    [Tooltip("เวลาหยุดยืนนิ่งหลังตีเสร็จ (วงโจมตีหาย) ก่อนจะเริ่มเดินตามต่อ")]
    public float attackRecoveryDelay = 0.5f;

    [Header("Enemy Spacing / Separation Settings")]
    [Tooltip("ระยะห่างที่ศัตรูพยายามรักษาระหว่างกันเพื่อไม่ให้กองรวมกัน")]
    public float separationRadius = 1.5f;
    [Tooltip("ความแรงในการผลักกันออกเมื่อเข้าใกล้กันเกินไป")]
    public float separationWeight = 1.2f;

    [Header("Telegraph Attack Settings")]
    [Tooltip("เวลานับถอยหลังก่อนที่ช่องโจมตีจะระเบิด")]
    public float telegraphDuration = 0.8f;
    [Tooltip("ขนาดพื้นที่สี่เหลี่ยมสีแดงที่เตือนบนพื้น")]
    public float telegraphBoxSize = 3.5f;

    private float lastAttackTime = -999f;
    private bool isAttacking = false;
    private GameObject currentTelegraphIndicator;

    [Header("Death Settings & Timing")]
    [Tooltip("เปิดใช้งานระเบิดตัวเองเมื่อตาย")]
    public bool enableDeathExplosion = true;
    public float deathExplosionRadius = 4.0f;
    public float deathExplosionDamage = 50f;
    [Tooltip("เวลากี่วินาทีก่อนที่ศัตรูจะระเบิดหลังตาย")]
    public float deathDelay = 1.0f;
    [Tooltip("สีของหุ่นตอนตายก่อนระเบิด")]
    public Color preDeathColor = Color.black;
    [Tooltip("ความแรงในการสั่นตอนใกล้ระเบิด")]
    public float shakeMagnitude = 0.08f;
    public GameObject deathExplosionPrefab;

    [Header("Death Audio Settings")]
    public AudioClip customExplosionSound;
    public float explosionVolume = 1.5f;

    [Header("Visual Feedback (Flash)")]
    public Color flashColor = Color.white;
    public float flashDuration = 0.1f;

    private Renderer meshRenderer;
    private MaterialPropertyBlock propBlock;
    private Transform playerTransform;
    private bool isDead = false;

    private float originalMoveSpeed = -1f;
    private float slowTimer = 0f;
    private bool isSlowed = false;

    private bool isBeingPulled = false;

    [Header("UI Settings")]
    [Tooltip("แสดงหลอดเลือดบนหัวศัตรู")]
    public bool showHealthBar = true;

    private Slider healthSlider;
    private GameObject healthCanvasObj;

    [Header("Drone Tilt Animation Settings")]
    [Tooltip("ความแรงในการเอียงซ้ายขวาขณะเคลื่อนที่")]
    public float maxTiltAngle = 12f;
    private float tiltFrequencyX;
    private float tiltFrequencyZ;
    private float tiltRandomSeed;

    private void Awake()
    {
        currentHealth = maxHealth;

        meshRenderer = GetComponentInChildren<Renderer>();
        propBlock = new MaterialPropertyBlock();

        if (showHealthBar)
        {
            CreateOverheadHealthBar();
        }

        tiltRandomSeed = Random.Range(0f, 100f);
        tiltFrequencyX = Random.Range(3.5f, 6.5f);
        tiltFrequencyZ = Random.Range(2.5f, 5.0f);
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

        HandleSlowTimer();
        UpdateMovementAndCombat();
        UpdateHealthBarTransform();
        HandleDroneTiltAnimation();
    }

    private void UpdateMovementAndCombat()
    {
        if (isBeingPulled || isAttacking) return;
        if (playerTransform == null) return;

        Vector3 toPlayer = playerTransform.position - transform.position;
        toPlayer.y = 0;
        float distToPlayer = toPlayer.magnitude;

        if (distToPlayer <= attackRange * 1.5f)
        {
            Quaternion lookRot = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, 12f * Time.deltaTime);
        }

        if (distToPlayer <= attackRange && Time.time >= lastAttackTime + attackCooldown)
        {
            StartCoroutine(PerformTelegraphAttack());
        }
        else if (distToPlayer > attackRange)
        {
            // 🌟 คำนวณทิศทางเดินเข้าหาผู้เล่น
            Vector3 moveDir = toPlayer.normalized;

            // 🌟 เพิ่มระบบ Separation (ตรวจจับและดันออกหากศัตรูตัวอื่นยืนใกล้เกินไป เพื่อไม่ให้กองรวมกัน)
            Vector3 separationForce = Vector3.zero;
            Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, separationRadius);
            int neighborCount = 0;

            foreach (var col in nearbyColliders)
            {
                if (col.gameObject != gameObject && (col.CompareTag("Enemy") || col.GetComponent<EnemyBase>() != null))
                {
                    Vector3 pushDir = transform.position - col.transform.position;
                    pushDir.y = 0;
                    float d = pushDir.magnitude;
                    if (d > 0f && d < separationRadius)
                    {
                        // ยิ่งใกล้ยิ่งผลักแรง
                        separationForce += pushDir.normalized / d;
                        neighborCount++;
                    }
                }
            }

            if (neighborCount > 0)
            {
                // ผสมทิศทางเดินเข้าหาผู้เล่น เข้ากับแรงผลักออกจากเพื่อนร่วมทีม
                moveDir = (moveDir + separationForce * separationWeight).normalized;
            }

            float raycastDist = moveSpeed * Time.deltaTime + 0.3f;
            if (!Physics.Raycast(transform.position + Vector3.up * 0.5f, moveDir, raycastDist))
            {
                transform.position += moveDir * moveSpeed * Time.deltaTime;
            }

            Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 12f * Time.deltaTime);
        }
    }

    private void HandleDroneTiltAnimation()
    {
        float time = Time.time + tiltRandomSeed;

        float tiltFactor = isAttacking ? 0.3f : 1.0f;
        float tiltZ = Mathf.Sin(time * tiltFrequencyX) * (maxTiltAngle * tiltFactor);
        float tiltX = Mathf.Cos(time * tiltFrequencyZ) * (maxTiltAngle * 0.5f * tiltFactor);

        Transform modelTrans = transform;
        if (transform.childCount > 0)
        {
            Transform firstChild = transform.GetChild(0);
            if (firstChild.gameObject != healthCanvasObj && !firstChild.name.Contains("Canvas"))
            {
                modelTrans = firstChild;
            }
        }

        modelTrans.localRotation = Quaternion.Euler(tiltX, 0f, -tiltZ);
    }

    private System.Collections.IEnumerator PerformTelegraphAttack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        Vector3 targetPos = playerTransform != null ? playerTransform.position : transform.position + transform.forward * 3f;
        targetPos.y = 0.05f;

        currentTelegraphIndicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
        currentTelegraphIndicator.name = "AttackTelegraphBox";
        currentTelegraphIndicator.transform.position = targetPos;
        currentTelegraphIndicator.transform.localScale = new Vector3(telegraphBoxSize, 0.02f, telegraphBoxSize);

        Collider col = currentTelegraphIndicator.GetComponent<Collider>();
        if (col != null) Destroy(col);

        Renderer indRenderer = currentTelegraphIndicator.GetComponent<Renderer>();
        Material indMat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"));
        indMat.color = new Color(1f, 0f, 0f, 0.35f);
        indRenderer.material = indMat;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayTelegraphWarning(targetPos);
        }

        float elapsed = 0f;
        while (elapsed < telegraphDuration)
        {
            if (isDead) yield break;

            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0.2f, 0.8f, Mathf.PingPong(elapsed * 10f, 1f));
            indMat.color = new Color(1f, 0f, 0f, alpha);
            yield return null;
        }

        Collider[] hits = Physics.OverlapBox(targetPos, new Vector3(telegraphBoxSize * 0.5f, 2f, telegraphBoxSize * 0.5f), Quaternion.identity);
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

        if (currentTelegraphIndicator != null)
        {
            Destroy(currentTelegraphIndicator);
            currentTelegraphIndicator = null;
        }

        yield return new WaitForSeconds(attackRecoveryDelay);

        isAttacking = false;
    }

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

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayEnemyHurt(hitPoint);
        }

        if (currentHealth <= 0f)
        {
            StartCoroutine(DieSequence());
        }
    }

    private void TriggerFlashEffect()
    {
        if (meshRenderer != null && !isDead)
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

    private System.Collections.IEnumerator DieSequence()
    {
        if (isDead) yield break;
        isDead = true;

        if (currentTelegraphIndicator != null)
        {
            Destroy(currentTelegraphIndicator);
            currentTelegraphIndicator = null;
        }

        if (healthCanvasObj != null)
        {
            healthCanvasObj.SetActive(false);
        }

        SetRendererColor(preDeathColor);

        if (customExplosionSound != null)
        {
            AudioSource.PlayClipAtPoint(customExplosionSound, transform.position, explosionVolume);
        }
        else if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayEnemyDie(transform.position);
        }

        Vector3 originalPos = transform.position;
        float elapsed = 0f;

        while (elapsed < deathDelay)
        {
            elapsed += Time.deltaTime;

            transform.position = originalPos + new Vector3(
                Random.Range(-shakeMagnitude, shakeMagnitude),
                0f,
                Random.Range(-shakeMagnitude, shakeMagnitude)
            );

            yield return null;
        }

        transform.position = originalPos;

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

        SpawnDeathEffect();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnEnemyDefeated(transform.position);
        }

        Destroy(gameObject);
    }

    private void SpawnDeathEffect()
    {
        if (deathExplosionPrefab != null)
        {
            Instantiate(deathExplosionPrefab, transform.position, Quaternion.identity);
            return;
        }

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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, deathExplosionRadius);
    }
}