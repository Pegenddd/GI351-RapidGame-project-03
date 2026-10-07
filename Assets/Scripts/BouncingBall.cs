using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls the bouncing ball:
/// - Bounces off arena walls with speed retention.
/// - Pierces through enemies (บอลทะลุ) without deflecting or stopping.
/// - Inflicts damage and triggers blood VFX on enemies.
/// - Features dynamic trail and impact VFX.
/// </summary>
public class BouncingBall : MonoBehaviour
{
    [Header("Speed Settings")]
    public float baseSpeed = 16f;
    public float boostedSpeed = 30f;
    public float speedDecayRate = 3f;
    public float currentSpeed;

    [Header("Damage Settings")]
    public float normalDamage = 35f;
    public float boostedDamage = 60f;

    [Header("Visual Effects")]
    public Color normalColor = new Color(0.2f, 0.8f, 1f);
    public Color boostedColor = new Color(1f, 0.5f, 0.1f);

    private Vector3 moveDirection;
    private TrailRenderer trailRenderer;
    private Renderer ballRenderer;
    private MaterialPropertyBlock propBlock;
    private Rigidbody rb;

    // Cooldown per enemy to prevent multi-hit on consecutive frames while piercing through
    private readonly Dictionary<EnemyController, float> hitCooldowns = new Dictionary<EnemyController, float>();

    private void Awake()
    {
        currentSpeed = baseSpeed;
        moveDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
        if (moveDirection == Vector3.zero) moveDirection = Vector3.forward;

        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true; // Code-driven physics for crisp, reliable bounces
        }

        ballRenderer = GetComponent<Renderer>();
        propBlock = new MaterialPropertyBlock();

        SetupTrailRenderer();
    }

    private void Start()
    {
        UpdateVisuals();
    }

    private void SetupTrailRenderer()
    {
        trailRenderer = GetComponent<TrailRenderer>();
        if (trailRenderer == null)
        {
            trailRenderer = gameObject.AddComponent<TrailRenderer>();
        }

        trailRenderer.time = 0.35f;
        trailRenderer.startWidth = 0.7f;
        trailRenderer.endWidth = 0.05f;
        trailRenderer.autodestruct = false;

        Material trailMat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"));
        trailRenderer.material = trailMat;

        UpdateVisuals();
    }

    private void Update()
    {
        // Decay boosted speed gradually towards base speed
        if (currentSpeed > baseSpeed)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, baseSpeed, speedDecayRate * Time.deltaTime);
            UpdateVisuals();
        }

        // Move ball on XZ plane
        Vector3 moveDelta = moveDirection * (currentSpeed * Time.deltaTime);
        transform.position += moveDelta;

        // Keep ball at constant elevation
        Vector3 pos = transform.position;
        pos.y = 0.6f;
        transform.position = pos;

        // Update cooldowns
        UpdateCooldowns();

        // Check sphere collisions for walls and enemies
        CheckCollisions(moveDelta);
    }

    private void UpdateCooldowns()
    {
        List<EnemyController> keys = new List<EnemyController>(hitCooldowns.Keys);
        foreach (var key in keys)
        {
            hitCooldowns[key] -= Time.deltaTime;
            if (hitCooldowns[key] <= 0f)
            {
                hitCooldowns.Remove(key);
            }
        }
    }

    private void CheckCollisions(Vector3 moveDelta)
    {
        float radius = transform.localScale.x * 0.5f;

        // 1. Check for arena wall collision (bounce)
        RaycastHit hit;
        if (Physics.SphereCast(transform.position, radius, moveDirection, out hit, moveDelta.magnitude + 0.15f))
        {
            // If it's a wall or arena boundary
            if (hit.collider.gameObject.name.Contains("Wall") || hit.collider.gameObject.name.Contains("Border"))
            {
                ReflectFromWall(hit.normal, hit.point);
            }
        }

        // Clamp inside arena walls to prevent escaping bounds
        Vector3 curPos = transform.position;
        float boundary = 14.2f;
        bool bouncedWall = false;

        if (curPos.x > boundary)
        {
            curPos.x = boundary;
            moveDirection.x = -Mathf.Abs(moveDirection.x);
            bouncedWall = true;
        }
        else if (curPos.x < -boundary)
        {
            curPos.x = -boundary;
            moveDirection.x = Mathf.Abs(moveDirection.x);
            bouncedWall = true;
        }

        if (curPos.z > boundary)
        {
            curPos.z = boundary;
            moveDirection.z = -Mathf.Abs(moveDirection.z);
            bouncedWall = true;
        }
        else if (curPos.z < -boundary)
        {
            curPos.z = -boundary;
            moveDirection.z = Mathf.Abs(moveDirection.z);
            bouncedWall = true;
        }

        if (bouncedWall)
        {
            transform.position = curPos;
            moveDirection.y = 0;
            moveDirection.Normalize();
            PlayBounceEffect(transform.position);
        }

        // 2. Check for Enemy collision -> PIERCE THROUGH (บอลทะลุ ไม่เด้งกลับ!)
        Collider[] overlaps = Physics.OverlapSphere(transform.position, radius + 0.2f);
        foreach (var col in overlaps)
        {
            EnemyController enemy = col.GetComponentInParent<EnemyController>();
            if (enemy != null)
            {
                PierceEnemy(enemy, col.ClosestPoint(transform.position));
            }
        }
    }

    private void ReflectFromWall(Vector3 normal, Vector3 hitPoint)
    {
        normal.y = 0;
        if (normal == Vector3.zero) return;
        normal.Normalize();

        // Accurate reflection vector
        moveDirection = Vector3.Reflect(moveDirection, normal);
        moveDirection.y = 0;
        moveDirection.Normalize();

        PlayBounceEffect(hitPoint);
    }

    private void PlayBounceEffect(Vector3 hitPoint)
    {
        if (SoundEffects.Instance != null)
        {
            SoundEffects.Instance.PlayBounce();
        }

        // Spark particles on wall bounce
        SpawnSpark(hitPoint);

        if (CameraController.Instance != null)
        {
            CameraController.Instance.ShakeCamera(0.08f, 0.12f);
        }
    }

    /// <summary>
    /// Pierces through enemy without bouncing! Inflicts damage and blood.
    /// </summary>
    private void PierceEnemy(EnemyController enemy, Vector3 contactPoint)
    {
        if (hitCooldowns.ContainsKey(enemy)) return; // Already damaged in this pass

        // Record cooldown so the ball doesn't multi-hit every frame
        hitCooldowns[enemy] = 0.35f;

        float damage = (currentSpeed > baseSpeed + 3f) ? boostedDamage : normalDamage;
        enemy.TakeDamage(damage, contactPoint, moveDirection);

        // DO NOT CHANGE moveDirection -> The ball penetrates straight through!
        // Noticeable visual feedback
        if (CameraController.Instance != null)
        {
            CameraController.Instance.ShakeCamera(0.12f, 0.2f);
        }
    }

    /// <summary>
    /// Called when the player hits the ball with their bat/weapon.
    /// </summary>
    public void HitByPlayer(Vector3 hitDirection, float customForce = 0f)
    {
        hitDirection.y = 0;
        moveDirection = hitDirection.normalized;

        currentSpeed = (customForce > 0f) ? customForce : boostedSpeed;

        UpdateVisuals();

        if (SoundEffects.Instance != null)
        {
            SoundEffects.Instance.PlayHitBall();
        }

        // Hit spark burst
        SpawnSpark(transform.position);

        if (CameraController.Instance != null)
        {
            CameraController.Instance.ShakeCamera(0.2f, 0.35f);
        }
    }

    private void SpawnSpark(Vector3 position)
    {
        GameObject sparkObj = new GameObject("BallSpark");
        sparkObj.transform.position = position;

        ParticleSystem ps = sparkObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        Color c = (currentSpeed > baseSpeed + 3f) ? boostedColor : normalColor;
        main.startColor = new ParticleSystem.MinMaxGradient(c, Color.white);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 7f);
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
        main.loop = false;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 15) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;

        ps.Play();
        Destroy(sparkObj, 0.6f);
    }

    private void UpdateVisuals()
    {
        float ratio = Mathf.InverseLerp(baseSpeed, boostedSpeed, currentSpeed);
        Color activeColor = Color.Lerp(normalColor, boostedColor, ratio);

        if (ballRenderer != null)
        {
            ballRenderer.GetPropertyBlock(propBlock);
            propBlock.SetColor("_BaseColor", activeColor);
            propBlock.SetColor("_Color", activeColor);
            ballRenderer.SetPropertyBlock(propBlock);
        }

        if (trailRenderer != null)
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(activeColor, 0.0f), new GradientColorKey(Color.white, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.85f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
            );
            trailRenderer.colorGradient = gradient;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        EnemyController enemy = other.GetComponentInParent<EnemyController>();
        if (enemy != null)
        {
            PierceEnemy(enemy, other.ClosestPoint(transform.position));
        }
    }
}
