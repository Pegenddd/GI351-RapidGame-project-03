using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // 🌟 เพิ่ม namespace สำหรับ UI
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 8.5f;
    public float rotationSpeed = 15f;

    [Header("Player Health Settings")]
    public float maxPlayerHealth = 100f;
    public float currentPlayerHealth;
    public Slider playerHealthSlider; // ลาก Slider UI ของผู้เล่นมาใส่ที่นี่ใน Inspector

    [Header("Dash Settings")]
    public float dashSpeed = 25f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 1f;
    private float lastDashTime = -99f;
    private bool isDashing = false;
    private float dashTimer = 0f;
    private Vector3 dashDir = Vector3.forward;

    [Header("Ball Magnet & Aim Skill")]
    public float magnetPullSpeed = 35f;
    public float maxMagnetDistance = 6f;
    public float magnetCooldown = 3f;
    private float lastMagnetTime = -99f;
    public bool showMagnetGizmo = true;

    [Header("Magnet Range Indicator Visual")]
    public Color indicatorColor = new Color(0.8f, 0.2f, 1f, 0.9f);

    public float aimSlowMultiplier = 0.4f;
    public float trajectoryLength = 15f;
    public int maxBounces = 2;
    private bool isAttractingBall = false;
    public bool HasBallAttached { get; private set; } = false;
    private BouncingBall targetMagnetBall;
    private LineRenderer trajectoryLine;
    private LineRenderer rangeIndicatorLine;

    [Header("Bat / Strike Settings")]
    public float hitRadius = 2.4f;
    public float hitAngle = 140f;
    public float swingCooldown = 0.28f;
    public float hitForce = 32f;

    [Header("Visual Elements")]
    public Transform batTransform;
    public Transform modelTransform;

    private float lastSwingTime = -1f;
    private bool isSwinging = false;
    private float swingAnimationTimer = 0f;
    private Quaternion defaultBatRot;
    private Vector3 moveInput = Vector3.zero;
    private Camera mainCam;

    private void Awake()
    {
        currentPlayerHealth = maxPlayerHealth; // 🌟 กำหนดเลือดเริ่มต้น
        mainCam = Camera.main;
        if (batTransform != null)
        {
            defaultBatRot = batTransform.localRotation;
        }
        SetupTrajectoryLine();
        SetupRangeIndicatorLine();
    }

    private void Start()
    {
        UpdatePlayerHealthUI();
    }

    // 🌟 ฟังก์ชันรับดาเมจเมื่อถูกศัตรูโจมตี
    public void TakeDamage(float damageAmount)
    {
        currentPlayerHealth = Mathf.Max(0, currentPlayerHealth - damageAmount);
        UpdatePlayerHealthUI();

        if (SoundEffects.Instance != null)
        {
            // SoundEffects.Instance.PlayPlayerHurt(); // ถ้ามีเสียงโดนตี
        }

        if (currentPlayerHealth <= 0f)
        {
            Die();
        }
    }

    private void UpdatePlayerHealthUI()
    {
        if (playerHealthSlider != null)
        {
            playerHealthSlider.value = currentPlayerHealth / maxPlayerHealth;
        }
    }

    private void Die()
    {
        Debug.Log("Player Died!");
        // โค้ดจัดการเมื่อผู้เล่นตาย เช่น Restart Game
    }

    private void SetupTrajectoryLine()
    {
        GameObject lineObj = new GameObject("BallTrajectoryLine");
        trajectoryLine = lineObj.AddComponent<LineRenderer>();
        trajectoryLine.startWidth = 0.2f;
        trajectoryLine.endWidth = 0.05f;
        trajectoryLine.positionCount = 0;
        trajectoryLine.material = new Material(Shader.Find("Sprites/Default"));
        trajectoryLine.startColor = new Color(1f, 0.6f, 0.1f);
        trajectoryLine.endColor = new Color(1f, 0.2f, 0.1f, 0.1f);
    }

    private void SetupRangeIndicatorLine()
    {
        GameObject rangeObj = new GameObject("MagnetRangeIndicator");
        rangeIndicatorLine = rangeObj.AddComponent<LineRenderer>();
        rangeIndicatorLine.startWidth = 0.15f;
        rangeIndicatorLine.endWidth = 0.15f;
        rangeIndicatorLine.positionCount = 51;
        rangeIndicatorLine.useWorldSpace = true;

        Shader lineShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
        Material lineMat = new Material(lineShader);

        if (lineMat.HasProperty("_BaseColor")) lineMat.SetColor("_BaseColor", indicatorColor);
        if (lineMat.HasProperty("_Color")) lineMat.SetColor("_Color", indicatorColor);

        rangeIndicatorLine.material = lineMat;
        rangeIndicatorLine.startColor = indicatorColor;
        rangeIndicatorLine.endColor = indicatorColor;
    }

    private void Update()
    {
        ReadInput();

        if (CheckDashAction() && Time.time >= lastDashTime + dashCooldown && !isDashing)
        {
            StartDash();
        }

        if (isDashing)
        {
            HandleDashMovement();
        }
        else
        {
            HandleMovement();
            HandleAiming();
            HandleMagnetSkill();
        }

        HandleBatAnimation();
        UpdateRangeIndicatorVisual();

        if (CheckHitAction() && Time.time >= lastSwingTime + swingCooldown && !isDashing)
        {
            PerformSwing();
        }

        if (HasBallAttached && targetMagnetBall != null)
        {
            DrawTrajectory();
        }
        else if (trajectoryLine != null)
        {
            trajectoryLine.positionCount = 0;
        }
    }

    private void ReadInput()
    {
        float x = 0f;
        float z = 0f;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) z += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) z -= 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x += 1f;
        }
        else
#endif
        {
            x = Input.GetAxisRaw("Horizontal");
            z = Input.GetAxisRaw("Vertical");
        }

        moveInput = new Vector3(x, 0, z).normalized;
    }

    private bool CheckHitAction()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) return true;
#endif
        return Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space);
    }

    private bool CheckDashAction()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.leftShiftKey.wasPressedThisFrame) return true;
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame) return true;
#endif
        return Input.GetKeyDown(KeyCode.LeftShift) || Input.GetMouseButtonDown(1);
    }

    private void StartDash()
    {
        isDashing = true;
        dashTimer = 0f;
        lastDashTime = Time.time;
        dashDir = moveInput.sqrMagnitude > 0.01f ? moveInput : transform.forward;

        if (isAttractingBall && !HasBallAttached)
        {
            StopMagnet();
        }
    }

    private void HandleDashMovement()
    {
        dashTimer += Time.deltaTime;
        if (dashTimer < dashDuration)
        {
            transform.position += dashDir * (dashSpeed * Time.deltaTime);
            ClampPosition();
        }
        else
        {
            isDashing = false;
        }
    }

    private void HandleMovement()
    {
        if (moveInput.sqrMagnitude > 0.01f)
        {
            float currentSpeed = moveSpeed;
            if (HasBallAttached)
            {
                currentSpeed *= aimSlowMultiplier;
            }

            Vector3 moveDir = moveInput.normalized;
            float moveDistance = currentSpeed * Time.deltaTime;

            float playerRadius = 0.4f;
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            bool isBlocked = false;

            if (Physics.SphereCast(origin, playerRadius, moveDir, out RaycastHit hit, moveDistance + 0.1f))
            {
                if (hit.collider.transform != transform &&
                    (targetMagnetBall == null || hit.collider.transform != targetMagnetBall.transform))
                {
                    isBlocked = true;
                }
            }

            if (!isBlocked)
            {
                transform.position += moveDir * moveDistance;
                ClampPosition();
            }
        }
    }

    private void ClampPosition()
    {
        Vector3 clampedPos = transform.position;
        clampedPos.x = Mathf.Clamp(clampedPos.x, -13.5f, 13.5f);
        clampedPos.z = Mathf.Clamp(clampedPos.z, -13.5f, 13.5f);
        clampedPos.y = 1f;
        transform.position = clampedPos;
    }

    private void HandleAiming()
    {
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return;

        Vector2 mouseScreenPos;
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            mouseScreenPos = Mouse.current.position.ReadValue();
        }
        else
#endif
        {
            mouseScreenPos = Input.mousePosition;
        }

        Ray ray = mainCam.ScreenPointToRay(mouseScreenPos);
        Plane groundPlane = new Plane(Vector3.up, new Vector3(0, transform.position.y, 0));

        if (groundPlane.Raycast(ray, out float enter))
        {
            Vector3 targetPoint = ray.GetPoint(enter);
            Vector3 lookDir = (targetPoint - transform.position);
            lookDir.y = 0;

            if (lookDir.sqrMagnitude > 0.1f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }
        }
    }

    private void HandleMagnetSkill()
    {
        bool isCooldownReady = Time.time >= lastMagnetTime + magnetCooldown;

        if (Input.GetKeyDown(KeyCode.E) && !HasBallAttached && !isAttractingBall && isCooldownReady)
        {
            BouncingBall[] balls = FindObjectsByType<BouncingBall>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            float closestDist = float.MaxValue;
            BouncingBall chosenBall = null;

            foreach (var ball in balls)
            {
                float dist = Vector3.Distance(transform.position, ball.transform.position);
                if (dist < closestDist && dist <= maxMagnetDistance)
                {
                    closestDist = dist;
                    chosenBall = ball;
                }
            }

            if (chosenBall != null)
            {
                targetMagnetBall = chosenBall;
                isAttractingBall = true;
                lastMagnetTime = Time.time;
                targetMagnetBall.StartMagnetPull();
            }
        }

        if (isAttractingBall && targetMagnetBall != null && !HasBallAttached)
        {
            Vector3 targetHoldPos = transform.position + transform.forward * 1.2f;
            targetMagnetBall.transform.position = Vector3.MoveTowards(targetMagnetBall.transform.position, targetHoldPos, magnetPullSpeed * Time.deltaTime);
            targetMagnetBall.transform.position = new Vector3(targetMagnetBall.transform.position.x, 0.6f, targetMagnetBall.transform.position.z);

            float distToPlayer = Vector3.Distance(transform.position, targetMagnetBall.transform.position);
            if (distToPlayer <= 1.5f)
            {
                HasBallAttached = true;
                isAttractingBall = false;
                targetMagnetBall.SetAttachedToPlayer(transform);
            }
        }
    }

    private void UpdateRangeIndicatorVisual()
    {
        if (rangeIndicatorLine == null) return;

        bool isCooldownReady = Time.time >= lastMagnetTime + magnetCooldown;
        if (isCooldownReady && !HasBallAttached)
        {
            rangeIndicatorLine.enabled = true;

            rangeIndicatorLine.startColor = indicatorColor;
            rangeIndicatorLine.endColor = indicatorColor;
            if (rangeIndicatorLine.material.HasProperty("_BaseColor")) rangeIndicatorLine.material.SetColor("_BaseColor", indicatorColor);
            if (rangeIndicatorLine.material.HasProperty("_Color")) rangeIndicatorLine.material.SetColor("_Color", indicatorColor);

            int segments = 51;
            Vector3[] points = new Vector3[segments];
            Vector3 centerPos = transform.position;

            for (int i = 0; i < segments; i++)
            {
                float angle = (i / (float)(segments - 1)) * Mathf.PI * 2f;
                float x = Mathf.Sin(angle) * maxMagnetDistance;
                float z = Mathf.Cos(angle) * maxMagnetDistance;

                points[i] = new Vector3(centerPos.x + x, 0.05f, centerPos.z + z);
            }
            rangeIndicatorLine.SetPositions(points);
        }
        else
        {
            rangeIndicatorLine.enabled = false;
        }
    }

    private void StopMagnet()
    {
        if (targetMagnetBall != null)
        {
            targetMagnetBall.ReleaseFromPlayer();
        }
        isAttractingBall = false;
        HasBallAttached = false;
        targetMagnetBall = null;
        if (trajectoryLine != null) trajectoryLine.positionCount = 0;
    }

    private void DrawTrajectory()
    {
        if (trajectoryLine == null || targetMagnetBall == null) return;

        List<Vector3> points = new List<Vector3>();
        Vector3 currentPos = transform.position + Vector3.up * 0.6f;
        points.Add(currentPos);

        Vector3 currentDir = transform.forward;
        currentDir.y = 0;
        currentDir.Normalize();

        float remainingLength = trajectoryLength;

        for (int i = 0; i <= maxBounces; i++)
        {
            Ray ray = new Ray(currentPos, currentDir);
            if (Physics.Raycast(ray, out RaycastHit hit, remainingLength))
            {
                if (hit.collider.transform != transform && hit.collider.transform != targetMagnetBall.transform)
                {
                    points.Add(hit.point + Vector3.up * 0.6f);
                    remainingLength -= hit.distance;

                    currentDir = Vector3.Reflect(currentDir, hit.normal);
                    currentDir.y = 0;
                    currentDir.Normalize();

                    currentPos = hit.point + currentDir * 0.1f;
                }
                else
                {
                    currentPos = hit.point + currentDir * 0.1f;
                    i--;
                }
            }
            else
            {
                points.Add(currentPos + currentDir * remainingLength);
                break;
            }
        }

        trajectoryLine.positionCount = points.Count;
        trajectoryLine.SetPositions(points.ToArray());
    }

    private void PerformSwing()
    {
        lastSwingTime = Time.time;
        isSwinging = true;
        swingAnimationTimer = 0f;

        if (SoundEffects.Instance != null) SoundEffects.Instance.PlaySwing();
        SpawnSwingSlashEffect();

        if (HasBallAttached && targetMagnetBall != null)
        {
            targetMagnetBall.HitByPlayer(transform.forward, hitForce);
            StopMagnet();
            return;
        }

        BouncingBall[] balls = FindObjectsByType<BouncingBall>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        BouncingBall targetBall = null;
        float closestDist = float.MaxValue;

        foreach (var ball in balls)
        {
            Vector3 toBall = ball.transform.position - transform.position;
            toBall.y = 0;
            float dist = toBall.magnitude;

            if (dist <= hitRadius)
            {
                float angle = Vector3.Angle(transform.forward, toBall);
                if (angle <= hitAngle * 0.5f && dist < closestDist)
                {
                    closestDist = dist;
                    targetBall = ball;
                }
            }
        }

        if (targetBall != null)
        {
            targetBall.HitByPlayer(transform.forward, hitForce);
        }
    }

    private void HandleBatAnimation()
    {
        if (!isSwinging || batTransform == null) return;

        swingAnimationTimer += Time.deltaTime;
        float progress = swingAnimationTimer / 0.18f;

        if (progress <= 1f)
        {
            float swingAngle = Mathf.Sin(progress * Mathf.PI) * 90f;
            batTransform.localRotation = defaultBatRot * Quaternion.Euler(0, swingAngle, -swingAngle * 0.4f);
        }
        else
        {
            batTransform.localRotation = defaultBatRot;
            isSwinging = false;
        }
    }

    private void SpawnSwingSlashEffect()
    {
        GameObject slashObj = new GameObject("SwingSlashVFX");
        slashObj.transform.position = transform.position + transform.forward * 1.1f + Vector3.up * 0.2f;
        slashObj.transform.rotation = transform.rotation * Quaternion.Euler(0, -45, 0);

        ParticleSystem ps = slashObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.9f, 0.3f, 0.9f), new Color(1f, 0.4f, 0.1f, 0f));
        main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(6f, 10f);
        main.startLifetime = 0.15f;
        main.loop = false;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.arc = 120f;
        shape.radius = 1.2f;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 30) });

        ps.Play();
        Destroy(slashObj, 0.4f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, hitRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * hitRadius);

        if (showMagnetGizmo)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, maxMagnetDistance);
        }
    }
}