using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls the bouncing ball:
/// - Bounces off arena walls, enemies, and pinball bumpers with speed retention.
/// - Inflicts damage and triggers blood VFX on enemies.
/// - Features dynamic trail, impact VFX, and player magnet/attachment support.
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

    // Magnet & Attachment state variables
    private bool isAttachedToPlayer = false;
    private Transform playerTransform;

    // Cooldown per enemy to prevent multi-hit on consecutive frames (เปลี่ยนจาก EnemyController เป็น EnemyBase)
    private readonly Dictionary<EnemyBase, float> hitCooldowns = new Dictionary<EnemyBase, float>();

    private void Awake()
    {
        currentSpeed = baseSpeed;
        moveDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
        if (moveDirection == Vector3.zero) moveDirection = Vector3.forward;

        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
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
        if (isAttachedToPlayer)
        {
            if (playerTransform != null)
            {
                transform.position = playerTransform.position + playerTransform.forward * 1.2f + Vector3.up * 0.6f;
            }
            return;
        }

        if (currentSpeed > baseSpeed)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, baseSpeed, speedDecayRate * Time.deltaTime);
            UpdateVisuals();
        }

        Vector3 moveDelta = moveDirection * (currentSpeed * Time.deltaTime);
        transform.position += moveDelta;

        Vector3 pos = transform.position;
        pos.y = 0.6f;
        transform.position = pos;

        UpdateCooldowns();
        CheckCollisions(moveDelta);
    }

    public void StartMagnetPull()
    {
        isAttachedToPlayer = false;
    }

    public void SetAttachedToPlayer(Transform player)
    {
        isAttachedToPlayer = true;
        playerTransform = player;
        currentSpeed = 0f;
    }

    public void ReleaseFromPlayer()
    {
        isAttachedToPlayer = false;
        playerTransform = null;
    }

    private void UpdateCooldowns()
    {
        List<EnemyBase> keys = new List<EnemyBase>(hitCooldowns.Keys);
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

        RaycastHit hit;
        if (Physics.SphereCast(transform.position, radius, moveDirection, out hit, moveDelta.magnitude + 0.15f))
        {
            // 1. เช็คชนกำแพง
            if (hit.collider.gameObject.name.Contains("Wall") || hit.collider.gameObject.name.Contains("Border") || hit.collider.CompareTag("Wall"))
            {
                ReflectFromSurface(hit.normal, hit.point);
            }
            // 2. เช็คชน Pinball / Bumper
            else if (hit.collider.gameObject.name.Contains("Pinball") || hit.collider.gameObject.name.Contains("Bumper") || hit.collider.CompareTag("Pinball"))
            {
                currentSpeed = Mathf.Min(currentSpeed * 1.1f, boostedSpeed);
                ReflectFromSurface(hit.normal, hit.point);

                PinballBumper bumper = hit.collider.GetComponentInParent<PinballBumper>();
                if (bumper != null) bumper.OnHitByBall();
            }
            // 🌟 3. เช็คชน EnemyBase ผ่าน Raycast
            else
            {
                EnemyBase enemyHit = hit.collider.GetComponentInParent<EnemyBase>() ?? hit.collider.GetComponent<EnemyBase>();
                if (enemyHit != null && !hitCooldowns.ContainsKey(enemyHit))
                {
                    HitEnemyAndBounce(enemyHit, hit.point);
                }
            }
        }

        // Clamp ขอบสนามกันหลุด
        Vector3 curPos = transform.position;
        float boundary = 14.2f;
        bool bouncedWall = false;

        if (curPos.x > boundary) { curPos.x = boundary; moveDirection.x = -Mathf.Abs(moveDirection.x); bouncedWall = true; }
        else if (curPos.x < -boundary) { curPos.x = -boundary; moveDirection.x = Mathf.Abs(moveDirection.x); bouncedWall = true; }

        if (curPos.z > boundary) { curPos.z = boundary; moveDirection.z = -Mathf.Abs(moveDirection.z); bouncedWall = true; }
        else if (curPos.z < -boundary) { curPos.z = -boundary; moveDirection.z = Mathf.Abs(moveDirection.z); bouncedWall = true; }

        if (bouncedWall)
        {
            transform.position = curPos;
            moveDirection.y = 0;
            moveDirection.Normalize();
            PlayBounceEffect(transform.position);
        }

        // 4. เช็คชนวัตถุรอบตัวผ่าน OverlapSphere
        Collider[] overlaps = Physics.OverlapSphere(transform.position, radius + 0.1f);
        foreach (var col in overlaps)
        {
            if (col.gameObject.name.Contains("Pinball") || col.gameObject.name.Contains("Bumper") || col.CompareTag("Pinball"))
            {
                Vector3 normal = (transform.position - col.transform.position).normalized;
                normal.y = 0;
                if (normal == Vector3.zero) normal = -moveDirection;
                ReflectFromSurface(normal, col.ClosestPoint(transform.position));

                PinballBumper bumper = col.GetComponentInParent<PinballBumper>();
                if (bumper != null) bumper.OnHitByBall();

                break;
            }

            // 🌟 เช็คชน EnemyBase ผ่าน OverlapSphere
            EnemyBase enemy = col.GetComponentInParent<EnemyBase>() ?? col.GetComponent<EnemyBase>();
            if (enemy != null && !hitCooldowns.ContainsKey(enemy))
            {
                HitEnemyAndBounce(enemy, col.ClosestPoint(transform.position));
                break;
            }
        }
    }

    private void HitEnemyAndBounce(EnemyBase enemy, Vector3 contactPoint)
    {
        hitCooldowns[enemy] = 0.2f;

        float damage = (currentSpeed > baseSpeed + 3f) ? boostedDamage : normalDamage;
        enemy.TakeDamage(damage, contactPoint, moveDirection); // 🌟 สั่งลดเลือดและแสดงดาเมจบนตัวศัตรู

        Vector3 normal = (transform.position - enemy.transform.position).normalized;
        normal.y = 0;
        if (normal == Vector3.zero) normal = -moveDirection;

        ReflectFromSurface(normal, contactPoint);
    }

    private void ReflectFromSurface(Vector3 normal, Vector3 hitPoint)
    {
        normal.y = 0;
        if (normal == Vector3.zero) return;
        normal.Normalize();

        moveDirection = Vector3.Reflect(moveDirection, normal);
        moveDirection.y = 0;
        moveDirection.Normalize();

        PlayBounceEffect(hitPoint);
    }

    private void PlayBounceEffect(Vector3 hitPoint)
    {
        if (SoundEffects.Instance != null) SoundEffects.Instance.PlayBounce();
        SpawnSpark(hitPoint);

        if (CameraController.Instance != null)
        {
            CameraController.Instance.ShakeCamera(0.08f, 0.12f);
        }
    }

    public void HitByPlayer(Vector3 hitDirection, float customForce = 0f)
    {
        ReleaseFromPlayer();

        hitDirection.y = 0;
        moveDirection = hitDirection.normalized;

        currentSpeed = (customForce > 0f) ? customForce : boostedSpeed;
        UpdateVisuals();

        if (SoundEffects.Instance != null) SoundEffects.Instance.PlayHitBall();
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
}