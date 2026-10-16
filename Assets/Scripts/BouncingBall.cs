using System.Collections.Generic;
using UnityEngine;

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
    public Color explosiveColor = new Color(1f, 0.1f, 0.1f);
    public Color iceColor = new Color(0.4f, 0.8f, 1f); // สีฟ้าครามเมื่อบอลติดสถานะน้ำแข็ง

    private Vector3 moveDirection;
    private TrailRenderer trailRenderer;
    private Renderer ballRenderer;
    private MaterialPropertyBlock propBlock;
    private Rigidbody rb;

    private bool isAttachedToPlayer = false;
    private Transform playerTransform;

    private bool isExplosive = false;
    private float explosionRadius = 5f;
    private float explosionDamage = 80f;
    private bool explodeOnlyOnEnemy = false; // ตัวแปรเก็บค่าเงื่อนไขการระเบิด

    // ตัวแปรสำหรับสถานะกระสุนน้ำแข็ง (Ice Effect)
    private bool hasIceEffect = false;
    private float storedSlowFactor = 0.4f;
    private float storedSlowDuration = 2.0f;

    // Orbit State variables
    private bool isOrbiting = false;
    private Transform orbitTargetTransform;
    private float orbitAngle = 0f;
    private float orbitRadius = 2.5f;
    private float currentOrbitSpeed = 150f;
    private float maxOrbitSpeed = 500f;
    private float speedRampRate = 60f;
    private float orbitTimer = 0f;
    private float orbitDurationMax = 5f;
    private float orbitDamageTimer = 0f;

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

        if (isOrbiting)
        {
            if (orbitTargetTransform == null)
            {
                ExitOrbitState();
                return;
            }

            orbitTimer += Time.deltaTime;
            if (orbitTimer >= orbitDurationMax)
            {
                ExitOrbitState();
                return;
            }

            currentOrbitSpeed = Mathf.Min(currentOrbitSpeed + speedRampRate * Time.deltaTime, maxOrbitSpeed);

            orbitAngle += currentOrbitSpeed * Time.deltaTime;
            float rad = orbitAngle * Mathf.Deg2Rad;
            float x = orbitTargetTransform.position.x + Mathf.Cos(rad) * orbitRadius;
            float z = orbitTargetTransform.position.z + Mathf.Sin(rad) * orbitRadius;
            transform.position = new Vector3(x, 0.6f, z);

            orbitDamageTimer += Time.deltaTime;
            if (orbitDamageTimer >= 0.2f)
            {
                orbitDamageTimer = 0f;
                Collider[] colliders = Physics.OverlapSphere(transform.position, 1.5f);
                foreach (var col in colliders)
                {
                    EnemyBase enemy = col.GetComponentInParent<EnemyBase>() ?? col.GetComponent<EnemyBase>();
                    if (enemy != null)
                    {
                        enemy.TakeDamage(boostedDamage, transform.position, (enemy.transform.position - transform.position).normalized);
                    }
                }
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
        isOrbiting = false;
    }

    public void SetAttachedToPlayer(Transform player)
    {
        isAttachedToPlayer = true;
        playerTransform = player;
        currentSpeed = 0f;
        isOrbiting = false;
    }

    public void ReleaseFromPlayer()
    {
        isAttachedToPlayer = false;
        playerTransform = null;
    }

    public void ApplyOrbitEffect(Transform centerTarget, float initialSpeed, float maxSpeed, float rampRate, float duration)
    {
        isOrbiting = true;
        orbitTargetTransform = centerTarget;
        currentOrbitSpeed = initialSpeed;
        maxOrbitSpeed = maxSpeed;
        speedRampRate = rampRate;
        orbitDurationMax = duration;
        orbitTimer = 0f;
        orbitDamageTimer = 0f;

        Vector3 dir = transform.position - centerTarget.position;
        orbitAngle = Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg;
        orbitRadius = Mathf.Max(1.5f, dir.magnitude);
    }

    private void ExitOrbitState()
    {
        isOrbiting = false;
        float rad = orbitAngle * Mathf.Deg2Rad;
        moveDirection = new Vector3(Mathf.Cos(rad), 0, Mathf.Sin(rad)).normalized;

        currentSpeed = boostedSpeed * 1.8f;
        UpdateVisuals();
    }

    public void SetExplosiveState(bool explosive, float radius, float damage, bool onlyOnEnemy)
    {
        isExplosive = explosive;
        explosionRadius = radius;
        explosionDamage = damage;
        explodeOnlyOnEnemy = onlyOnEnemy;
        UpdateVisuals();
    }

    // ฟังก์ชันสำหรับรับค่าความหนืดเมื่อชน Icewall
    public void ApplySlowDown(float slowFactor)
    {
        currentSpeed *= slowFactor; // ลดความเร็วลงตามตัวคูณที่กำหนด
        currentSpeed = Mathf.Max(currentSpeed, 3f); // ล็อกความเร็วขั้นต่ำไม่ให้บอลหยุดนิ่งสนิท
        UpdateVisuals();
    }

    // ฟังก์ชันรับสถานะน้ำแข็งจาก Icewall และเก็บบัฟไว้ส่งต่อให้ศัตรู
    public void ApplyIceEffect(float slowFactor, float duration)
    {
        ApplySlowDown(slowFactor);

        hasIceEffect = true;
        storedSlowFactor = slowFactor;
        storedSlowDuration = duration;

        UpdateVisuals();
    }

    private void TriggerExplosion(Vector3 hitPoint)
    {
        SpawnExplosionEffect(hitPoint);

        Collider[] colliders = Physics.OverlapSphere(hitPoint, explosionRadius);
        foreach (var col in colliders)
        {
            EnemyBase enemy = col.GetComponentInParent<EnemyBase>() ?? col.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                enemy.TakeDamage(explosionDamage, enemy.transform.position, (enemy.transform.position - hitPoint).normalized);
            }
        }

        isExplosive = false;
        explodeOnlyOnEnemy = false;
        UpdateVisuals();
    }

    public void SplitBall(int count, float duration)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject clone = Instantiate(gameObject, transform.position, Quaternion.identity);
            BouncingBall clonedBall = clone.GetComponent<BouncingBall>();
            if (clonedBall != null)
            {
                float randomAngle = Random.Range(0f, 360f);
                Quaternion rot = Quaternion.Euler(0, randomAngle, 0);
                clonedBall.moveDirection = rot * Vector3.forward;
                clonedBall.currentSpeed = baseSpeed;
                clonedBall.isExplosive = false;
                clonedBall.explodeOnlyOnEnemy = false;

                Destroy(clone, duration);
            }
        }
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
        if (isOrbiting) return;

        float radius = transform.localScale.x * 0.5f;

        RaycastHit hit;
        if (Physics.SphereCast(transform.position, radius, moveDirection, out hit, moveDelta.magnitude + 0.15f))
        {
            SpecialPinballObject specialObj = hit.collider.GetComponentInParent<SpecialPinballObject>() ?? hit.collider.GetComponent<SpecialPinballObject>();
            if (specialObj != null)
            {
                specialObj.ActivateEffect(this);
                return;
            }

            // เช็กการชนกับ Icewall
            Icewall iceWall = hit.collider.GetComponentInParent<Icewall>() ?? hit.collider.GetComponent<Icewall>();
            if (iceWall != null)
            {
                ApplyIceEffect(iceWall.slowMultiplier, iceWall.enemySlowDuration);
                ReflectFromSurface(hit.normal, hit.point, hit.collider);
                return;
            }

            if (hit.collider.gameObject.name.Contains("Wall") || hit.collider.gameObject.name.Contains("Border") || hit.collider.CompareTag("Wall"))
            {
                ReflectFromSurface(hit.normal, hit.point, hit.collider);
            }
            else if (hit.collider.gameObject.name.Contains("Pinball") || hit.collider.gameObject.name.Contains("Bumper") || hit.collider.CompareTag("Pinball"))
            {
                currentSpeed = Mathf.Min(currentSpeed * 1.1f, boostedSpeed);
                ReflectFromSurface(hit.normal, hit.point, hit.collider);

                PinballBumper bumper = hit.collider.GetComponentInParent<PinballBumper>();
                if (bumper != null) bumper.OnHitByBall();
            }
            else
            {
                EnemyBase enemyHit = hit.collider.GetComponentInParent<EnemyBase>() ?? hit.collider.GetComponent<EnemyBase>();
                if (enemyHit != null && !hitCooldowns.ContainsKey(enemyHit))
                {
                    HitEnemyAndBounce(enemyHit, hit.point);
                }
                else
                {
                    ReflectFromSurface(hit.normal, hit.point, hit.collider);
                }
            }
        }

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

            if (isExplosive && !explodeOnlyOnEnemy)
            {
                TriggerExplosion(transform.position);
            }
        }

        Collider[] overlaps = Physics.OverlapSphere(transform.position, radius + 0.1f);
        foreach (var col in overlaps)
        {
            SpecialPinballObject specialOverlap = col.GetComponentInParent<SpecialPinballObject>() ?? col.GetComponent<SpecialPinballObject>();
            if (specialOverlap != null)
            {
                specialOverlap.ActivateEffect(this);
                break;
            }

            // เช็ก Overlap กับ Icewall
            Icewall iceOverlap = col.GetComponentInParent<Icewall>() ?? col.GetComponent<Icewall>();
            if (iceOverlap != null)
            {
                ApplyIceEffect(iceOverlap.slowMultiplier, iceOverlap.enemySlowDuration);
                Vector3 normal = (transform.position - col.transform.position).normalized;
                normal.y = 0;
                if (normal == Vector3.zero) normal = -moveDirection;
                ReflectFromSurface(normal, col.ClosestPoint(transform.position), col);
                break;
            }

            if (col.gameObject.name.Contains("Pinball") || col.gameObject.name.Contains("Bumper") || col.CompareTag("Pinball"))
            {
                Vector3 normal = (transform.position - col.transform.position).normalized;
                normal.y = 0;
                if (normal == Vector3.zero) normal = -moveDirection;
                ReflectFromSurface(normal, col.ClosestPoint(transform.position), col);

                PinballBumper bumper = col.GetComponentInParent<PinballBumper>();
                if (bumper != null) bumper.OnHitByBall();

                break;
            }

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
        enemy.TakeDamage(damage, contactPoint, moveDirection);

        // ถ้าบอลมีสถานะน้ำแข็ง ให้ส่งต่อไปยังศัตรู
        if (hasIceEffect)
        {
            enemy.ApplySlowEffect(storedSlowFactor, storedSlowDuration);
            hasIceEffect = false; // ส่งต่อแล้ว ล้างสถานะน้ำแข็งออก
            UpdateVisuals();
        }

        if (isExplosive)
        {
            TriggerExplosion(contactPoint);
        }

        Vector3 normal = (transform.position - enemy.transform.position).normalized;
        normal.y = 0;
        if (normal == Vector3.zero) normal = -moveDirection;

        ReflectFromSurface(normal, contactPoint, enemy.GetComponent<Collider>());
    }

    private void ReflectFromSurface(Vector3 normal, Vector3 hitPoint, Collider hitCollider)
    {
        if (isOrbiting)
        {
            ExitOrbitState();
        }

        if (isExplosive)
        {
            bool isHitEnemy = (hitCollider != null) &&
                              (hitCollider.CompareTag("Enemy") ||
                               hitCollider.transform.parent != null && hitCollider.transform.parent.CompareTag("Enemy") ||
                               hitCollider.GetComponent<EnemyBase>() != null ||
                               hitCollider.GetComponentInParent<EnemyBase>() != null);

            if (!explodeOnlyOnEnemy || isHitEnemy)
            {
                TriggerExplosion(hitPoint);
            }
        }

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
        isOrbiting = false;

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
        Color c = isExplosive ? explosiveColor : ((currentSpeed > baseSpeed + 3f) ? boostedColor : normalColor);
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

    private void SpawnExplosionEffect(Vector3 position)
    {
        GameObject expObj = new GameObject("ExplosionVFX");
        expObj.transform.position = position;

        ParticleSystem ps = expObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startColor = new ParticleSystem.MinMaxGradient(Color.red, Color.yellow);
        main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(6f, 12f);
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
        main.loop = false;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 35) });

        ps.Play();
        Destroy(expObj, 1f);
    }

    private void UpdateVisuals()
    {
        Color activeColor = normalColor;

        if (hasIceEffect)
        {
            activeColor = iceColor; // แสดงสีฟ้าครามเมื่อติดสถานะน้ำแข็ง
        }
        else if (isExplosive)
        {
            activeColor = explosiveColor;
        }
        else
        {
            float ratio = Mathf.InverseLerp(baseSpeed, boostedSpeed, currentSpeed);
            activeColor = Color.Lerp(normalColor, boostedColor, ratio);
        }

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