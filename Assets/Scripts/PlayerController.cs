using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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
    public Slider playerHealthSlider;
    public Text playerHpText;

    [Header("Dash Settings")]
    public float dashSpeed = 25f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 1f;
    private float lastDashTime = -99f;
    private bool isDashing = false;
    private float dashTimer = 0f;
    private Vector3 dashDir = Vector3.forward;

    [Header("Dash Lightning Effect")]
    public bool enableDashLightning = true;
    public Color dashLightningColor = new Color(0.2f, 0.8f, 1f, 1f);
    private Vector3 lastDashTrailPos;

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

    [Header("Hit Cone Indicator Visual (Press V to Toggle)")]
    public bool showHitConeVisual = true;
    public Color hitConeColor = new Color(1f, 0.5f, 0f, 0.6f);
    private LineRenderer hitConeLine;

    [Header("Visual Elements")]
    public Transform batTransform;
    public Transform modelTransform;

    private float lastSwingTime = -1f;
    private bool isSwinging = false;
    private float swingAnimationTimer = 0f;
    private Quaternion defaultBatRot;
    private Vector3 moveInput = Vector3.zero;
    private Camera mainCam;

    [Header("Death & State")]
    private bool isDead = false;
    public bool IsDead => isDead;

    [Header("Player Audio Settings")]
    [Tooltip("เสียงมอเตอร์โดรนฮัมตอนเคลื่อนที่ (Loop)")]
    public AudioClip hoverEngineSound;
    [Tooltip("เสียงพ่นไอพ่นตอน Dash")]
    public AudioClip dashSound;
    [Tooltip("เสียงชอร์ตไฟฟ้าตอนโดนดาเมจ")]
    public AudioClip playerHurtSound;
    [Tooltip("เสียงชอร์ตไฟฟ้าแรงสูงตอนผู้เล่นตาย")]
    public AudioClip playerDeathSound;
    [Tooltip("เสียงฟาดอากาศตอนเหวี่ยงไม้")]
    public AudioClip swingSound;
    [Tooltip("เสียงเหล็กกระแทกบอลตอนหวดโดน")]
    public AudioClip hitBallSound;
    [Tooltip("เสียงคลื่นแม่เหล็กไฟฟ้าตอนใช้สกิลดูดบอล")]
    public AudioClip magnetSkillSound;
    [Range(0f, 2f)]
    public float playerAudioVolume = 1.0f;

    private AudioSource hoverAudioSource;
    private AudioSource actionAudioSource;

    private void Awake()
    {
        currentPlayerHealth = maxPlayerHealth;
        mainCam = Camera.main;
        if (batTransform != null)
        {
            defaultBatRot = batTransform.localRotation;
        }
        SetupTrajectoryLine();
        SetupRangeIndicatorLine();
        SetupHitConeLine();
        SetupAudioSources();
    }

    private void SetupAudioSources()
    {
        hoverAudioSource = gameObject.AddComponent<AudioSource>();
        hoverAudioSource.spatialBlend = 0.5f;
        hoverAudioSource.loop = true;
        hoverAudioSource.volume = playerAudioVolume;

        if (hoverEngineSound != null)
        {
            hoverAudioSource.clip = hoverEngineSound;
            hoverAudioSource.Play();
        }

        actionAudioSource = gameObject.AddComponent<AudioSource>();
        actionAudioSource.spatialBlend = 0.5f;
    }

    private void PlayPlayerAudio(AudioClip clip)
    {
        if (clip != null && actionAudioSource != null)
        {
            actionAudioSource.PlayOneShot(clip, playerAudioVolume);
        }
    }

    private void Start()
    {
        EnsureHealthUI();
        UpdatePlayerHealthUI();
    }

    private void EnsureHealthUI()
    {
        if (playerHealthSlider == null)
        {
            GameObject hpSliderObj = GameObject.Find("PlayerHPSlider");
            if (hpSliderObj != null)
            {
                playerHealthSlider = hpSliderObj.GetComponent<Slider>();
            }
        }

        if (playerHpText == null)
        {
            GameObject hpTextObj = GameObject.Find("HPValueText");
            if (hpTextObj != null)
            {
                playerHpText = hpTextObj.GetComponent<Text>();
            }
        }

        if (playerHealthSlider != null) return;

        GameObject canvasObj = GameObject.Find("HUDCanvas");
        if (canvasObj == null)
        {
            canvasObj = new GameObject("HUDCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

        GameObject hpPanel = new GameObject("PlayerHPPanel");
        hpPanel.transform.SetParent(canvasObj.transform, false);
        RectTransform panelRect = hpPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(30, -30);
        panelRect.sizeDelta = new Vector2(320, 68);

        Image panelBg = hpPanel.AddComponent<Image>();
        panelBg.color = new Color(0.06f, 0.08f, 0.12f, 0.9f);

        GameObject hpLabelObj = new GameObject("HPLabel");
        hpLabelObj.transform.SetParent(hpPanel.transform, false);
        Text hpLabel = hpLabelObj.AddComponent<Text>();
        hpLabel.font = defaultFont;
        hpLabel.fontSize = 17;
        hpLabel.fontStyle = FontStyle.Bold;
        hpLabel.color = new Color(1f, 0.85f, 0.85f);
        hpLabel.text = "❤️ PLAYER HEALTH";
        RectTransform labelRect = hpLabelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(0f, 1f);
        labelRect.pivot = new Vector2(0f, 1f);
        labelRect.anchoredPosition = new Vector2(15, -8);
        labelRect.sizeDelta = new Vector2(180, 24);

        GameObject valObj = new GameObject("HPValueText");
        valObj.transform.SetParent(hpPanel.transform, false);
        playerHpText = valObj.AddComponent<Text>();
        playerHpText.font = defaultFont;
        playerHpText.fontSize = 16;
        playerHpText.fontStyle = FontStyle.Bold;
        playerHpText.alignment = TextAnchor.MiddleRight;
        playerHpText.color = new Color(0.3f, 0.95f, 0.5f);
        playerHpText.text = $"{Mathf.CeilToInt(currentPlayerHealth)} / {Mathf.CeilToInt(maxPlayerHealth)}";
        RectTransform valRect = valObj.GetComponent<RectTransform>();
        valRect.anchorMin = new Vector2(1f, 1f);
        valRect.anchorMax = new Vector2(1f, 1f);
        valRect.pivot = new Vector2(1f, 1f);
        valRect.anchoredPosition = new Vector2(-15, -8);
        valRect.sizeDelta = new Vector2(100, 24);

        GameObject sliderObj = new GameObject("PlayerHPSlider");
        sliderObj.transform.SetParent(hpPanel.transform, false);
        playerHealthSlider = sliderObj.AddComponent<Slider>();
        playerHealthSlider.interactable = false;
        RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0f, 0f);
        sliderRect.anchorMax = new Vector2(1f, 0f);
        sliderRect.pivot = new Vector2(0.5f, 0f);
        sliderRect.anchoredPosition = new Vector2(0, 12);
        sliderRect.sizeDelta = new Vector2(-30, 20);

        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(sliderObj.transform, false);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.05f, 0.05f, 0.95f);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = Vector2.zero;

        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillArea.transform, false);
        Image fillImg = fillObj.AddComponent<Image>();
        fillImg.color = new Color(0.25f, 0.85f, 0.4f);
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;

        playerHealthSlider.fillRect = fillRect;
        playerHealthSlider.minValue = 0f;
        playerHealthSlider.maxValue = 1f;
        playerHealthSlider.value = 1f;
    }

    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;

        currentPlayerHealth = Mathf.Max(0, currentPlayerHealth - damageAmount);
        UpdatePlayerHealthUI();

        if (playerHurtSound != null)
        {
            PlayPlayerAudio(playerHurtSound);
        }
        else if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayPlayerHurt();
        }

        if (currentPlayerHealth <= 0f)
        {
            Die();
        }
    }

    private void UpdatePlayerHealthUI()
    {
        float ratio = Mathf.Clamp01(currentPlayerHealth / maxPlayerHealth);

        if (playerHealthSlider != null)
        {
            playerHealthSlider.value = ratio;
            if (playerHealthSlider.fillRect != null)
            {
                Image fillImg = playerHealthSlider.fillRect.GetComponent<Image>();
                if (fillImg != null)
                {
                    fillImg.color = (ratio > 0.5f)
                        ? Color.Lerp(Color.yellow, new Color(0.2f, 0.85f, 0.4f), (ratio - 0.5f) * 2f)
                        : Color.Lerp(new Color(0.95f, 0.2f, 0.2f), Color.yellow, ratio * 2f);
                }
            }
        }

        if (playerHpText != null)
        {
            playerHpText.text = $"{Mathf.CeilToInt(currentPlayerHealth)} / {Mathf.CeilToInt(maxPlayerHealth)}";
            playerHpText.color = (ratio > 0.3f) ? new Color(0.9f, 0.95f, 1f) : new Color(1f, 0.3f, 0.3f);
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        StopMagnet();

        if (hoverAudioSource != null) hoverAudioSource.Stop();

        if (playerDeathSound != null)
        {
            PlayPlayerAudio(playerDeathSound);
        }
        else if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayPlayerDeath();
        }

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (GameManager.Instance == null)
        {
            GameObject gmObj = new GameObject("GameManager");
            gmObj.AddComponent<GameManager>();
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerGameOver();
        }
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

    private void SetupHitConeLine()
    {
        GameObject coneObj = new GameObject("HitConeIndicator");
        hitConeLine = coneObj.AddComponent<LineRenderer>();
        hitConeLine.startWidth = 0.12f;
        hitConeLine.endWidth = 0.12f;
        hitConeLine.useWorldSpace = true;

        Shader lineShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
        Material lineMat = new Material(lineShader);

        if (lineMat.HasProperty("_BaseColor")) lineMat.SetColor("_BaseColor", hitConeColor);
        if (lineMat.HasProperty("_Color")) lineMat.SetColor("_Color", hitConeColor);

        hitConeLine.material = lineMat;
        hitConeLine.startColor = hitConeColor;
        hitConeLine.endColor = hitConeColor;
    }

    private void Update()
    {
        if (isDead)
        {
            if (trajectoryLine != null) trajectoryLine.positionCount = 0;
            if (rangeIndicatorLine != null) rangeIndicatorLine.enabled = false;
            if (hitConeLine != null) hitConeLine.enabled = false;
            return;
        }

        bool toggleVPressed = false;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame) toggleVPressed = true;
#endif
        if (Input.GetKeyDown(KeyCode.V)) toggleVPressed = true;

        if (toggleVPressed)
        {
            showHitConeVisual = !showHitConeVisual;
        }

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
        UpdateHitConeVisual();

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
        lastDashTrailPos = transform.position;

        if (dashSound != null)
        {
            PlayPlayerAudio(dashSound);
        }
        else if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayDash();
        }

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
            float dashDistance = dashSpeed * Time.deltaTime;
            float playerRadius = 0.4f;
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            bool isBlocked = false;

            RaycastHit[] hits = Physics.SphereCastAll(origin, playerRadius, dashDir, dashDistance + 0.1f);
            foreach (var hit in hits)
            {
                if (hit.collider.transform != transform &&
                    (targetMagnetBall == null || hit.collider.transform != targetMagnetBall.transform))
                {
                    EnemyBase enemyCheck = hit.collider.GetComponentInParent<EnemyBase>() ?? hit.collider.GetComponent<EnemyBase>();
                    if (enemyCheck == null)
                    {
                        isBlocked = true;
                        break;
                    }
                }
            }

            if (!isBlocked)
            {
                Vector3 oldPos = transform.position;
                transform.position += dashDir * dashDistance;

                if (enableDashLightning && Vector3.Distance(oldPos, transform.position) > 0.15f)
                {
                    GameObject lightningObj = new GameObject("DashLightning");
                    DashLightningEffect lEffect = lightningObj.AddComponent<DashLightningEffect>();
                    lEffect.Init(oldPos + Vector3.up * 0.5f, transform.position + Vector3.up * 0.5f, dashLightningColor);
                    lastDashTrailPos = transform.position;
                }
            }
            else
            {
                isDashing = false;
            }
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
            }
        }

        float targetEnginePitch = (moveInput.sqrMagnitude > 0.01f) ? 1.15f : 1.0f;
        if (hoverAudioSource != null && hoverAudioSource.isPlaying)
        {
            hoverAudioSource.pitch = Mathf.Lerp(hoverAudioSource.pitch, targetEnginePitch, 6f * Time.deltaTime);
        }
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetHoverEnginePitch(targetEnginePitch);
        }
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
        if (isAttractingBall && targetMagnetBall == null)
        {
            isAttractingBall = false;
        }
        if (HasBallAttached && targetMagnetBall == null)
        {
            StopMagnet();
        }

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

                if (magnetSkillSound != null)
                {
                    PlayPlayerAudio(magnetSkillSound);
                }
                else if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlayMagnetSkill();
                }
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

    private void UpdateHitConeVisual()
    {
        if (hitConeLine == null) return;

        if (!showHitConeVisual)
        {
            hitConeLine.enabled = false;
            return;
        }

        hitConeLine.enabled = true;
        hitConeLine.startColor = hitConeColor;
        hitConeLine.endColor = hitConeColor;
        if (hitConeLine.material.HasProperty("_BaseColor")) hitConeLine.material.SetColor("_BaseColor", hitConeColor);
        if (hitConeLine.material.HasProperty("_Color")) hitConeLine.material.SetColor("_Color", hitConeColor);

        int segments = 21;
        Vector3 centerPos = transform.position + Vector3.up * 0.05f;
        Vector3 forward = transform.forward;
        float halfAngle = hitAngle * 0.5f;

        Vector3[] points = new Vector3[segments + 2];
        points[0] = centerPos;

        for (int i = 0; i <= segments; i++)
        {
            float currentAngle = -halfAngle + (hitAngle * (i / (float)segments));
            Quaternion rot = Quaternion.Euler(0, currentAngle, 0);
            Vector3 dir = rot * forward;
            points[i + 1] = centerPos + dir * hitRadius;
        }
        points[segments + 1] = centerPos;

        hitConeLine.positionCount = points.Length;
        hitConeLine.SetPositions(points);
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

        if (swingSound != null)
        {
            PlayPlayerAudio(swingSound);
        }
        else if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySwing();
        }

        SpawnSwingSlashEffect();

        bool hitSomething = false;

        if (HasBallAttached && targetMagnetBall != null)
        {
            targetMagnetBall.HitByPlayer(transform.forward, hitForce);
            StopMagnet();
            hitSomething = true;
        }
        else
        {
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
                hitSomething = true;
            }
        }

        if (hitSomething)
        {
            if (hitBallSound != null)
            {
                PlayPlayerAudio(hitBallSound);
            }
            else if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayHitBall(transform.position);
            }
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