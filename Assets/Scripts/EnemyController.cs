using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Enemy behavior, health system, blood VFX, and overhead health bar.
/// Handles damage when pierced by the bouncing ball.
/// </summary>
public class EnemyController : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Movement Settings")]
    public float moveSpeed = 2.5f;
    public float wanderRadius = 12f;

    [Header("Visual Feedback")]
    public Color normalColor = new Color(0.9f, 0.25f, 0.25f);
    public Color flashColor = Color.white;
    public float flashDuration = 0.1f;

    private Renderer meshRenderer;
    private MaterialPropertyBlock propBlock;
    private Transform playerTransform;
    private Vector3 targetMovePosition;
    private float wanderTimer = 0f;
    private bool isDead = false;

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

        if (meshRenderer != null)
        {
            meshRenderer.GetPropertyBlock(propBlock);
            propBlock.SetColor("_BaseColor", normalColor);
            propBlock.SetColor("_Color", normalColor);
            meshRenderer.SetPropertyBlock(propBlock);
        }

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

        PickNewWanderPosition();
    }

    private void Update()
    {
        if (isDead) return;

        UpdateMovement();
        UpdateHealthBarTransform();
    }

    private void UpdateMovement()
    {
        // Wander or follow player slightly
        wanderTimer -= Time.deltaTime;
        if (wanderTimer <= 0f)
        {
            PickNewWanderPosition();
        }

        Vector3 moveDir = (targetMovePosition - transform.position);
        moveDir.y = 0;

        if (moveDir.sqrMagnitude > 0.2f)
        {
            moveDir.Normalize();
            transform.position += moveDir * moveSpeed * Time.deltaTime;

            Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 8f * Time.deltaTime);
        }
    }

    private void PickNewWanderPosition()
    {
        wanderTimer = Random.Range(2.5f, 4.5f);

        // 60% chance to steer towards player, 40% random wander
        if (playerTransform != null && Random.value < 0.6f)
        {
            Vector3 offset = Random.insideUnitSphere * 4f;
            offset.y = 0;
            targetMovePosition = playerTransform.position + offset;
        }
        else
        {
            Vector2 randomPoint = Random.insideUnitCircle * wanderRadius;
            targetMovePosition = new Vector3(randomPoint.x, transform.position.y, randomPoint.y);
        }

        // Clamp inside arena bounds
        targetMovePosition.x = Mathf.Clamp(targetMovePosition.x, -14f, 14f);
        targetMovePosition.z = Mathf.Clamp(targetMovePosition.z, -14f, 14f);
    }

    /// <summary>
    /// Called when the bouncing ball pierces through this enemy.
    /// </summary>
    public void TakeDamage(float damageAmount, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (isDead) return;

        currentHealth = Mathf.Max(0, currentHealth - damageAmount);

        // Update health bar
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth / maxHealth;
        }

        // Spawn blood splash effect (เสียเลือด)
        SpawnBloodSplatter(hitPoint, hitDirection);

        // Spawn floating damage text
        SpawnDamageNumber(damageAmount, hitPoint);

        // Sound effect
        if (SoundEffects.Instance != null)
        {
            SoundEffects.Instance.PlayEnemyHurt();
        }

        // Flash visual
        CancelInvoke(nameof(ResetFlash));
        SetRendererColor(flashColor);
        Invoke(nameof(ResetFlash), flashDuration);

        // Check death
        if (currentHealth <= 0f)
        {
            Die();
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
        if (!isDead)
        {
            SetRendererColor(normalColor);
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

        // Auto move up and fade out
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

        // Death blood explosion
        SpawnDeathEffect();

        // Notify GameManager
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
            // Face the overhead health bar towards the camera
            healthCanvasObj.transform.rotation = Camera.main.transform.rotation;
        }
    }
}

/// <summary>
/// Animates floating damage number popup.
/// </summary>
public class DamagePopupAnimator : MonoBehaviour
{
    private TextMesh textMesh;
    private float lifetime = 0.8f;
    private float timer = 0f;
    private Color startColor;

    public void Init(TextMesh tm)
    {
        textMesh = tm;
        startColor = tm.color;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float progress = timer / lifetime;

        transform.position += Vector3.up * (2.2f * Time.deltaTime);

        if (Camera.main != null)
        {
            transform.rotation = Camera.main.transform.rotation;
        }

        if (textMesh != null)
        {
            Color c = startColor;
            c.a = Mathf.Lerp(1f, 0f, progress);
            textMesh.color = c;
        }

        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}
