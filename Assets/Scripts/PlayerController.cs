using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Player controller handling:
/// - Movement (WASD / Arrows)
/// - Aiming towards mouse / movement direction
/// - Striking the ball with a bat / melee swing (Left Click / Space)
/// - Compatible with both new Input System and legacy Input Manager
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 8.5f;
    public float rotationSpeed = 15f;

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
        mainCam = Camera.main;
        if (batTransform != null)
        {
            defaultBatRot = batTransform.localRotation;
        }
    }

    private void Update()
    {
        ReadInput();
        HandleMovement();
        HandleAiming();
        HandleBatAnimation();

        // Check strike action
        if (CheckHitAction() && Time.time >= lastSwingTime + swingCooldown)
        {
            PerformSwing();
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

    private void HandleMovement()
    {
        if (moveInput.sqrMagnitude > 0.01f)
        {
            Vector3 movement = moveInput * (moveSpeed * Time.deltaTime);
            transform.position += movement;

            // Clamp inside arena bounds
            Vector3 clampedPos = transform.position;
            clampedPos.x = Mathf.Clamp(clampedPos.x, -13.5f, 13.5f);
            clampedPos.z = Mathf.Clamp(clampedPos.z, -13.5f, 13.5f);
            clampedPos.y = 1f; // Standard player height
            transform.position = clampedPos;
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
        else if (moveInput.sqrMagnitude > 0.01f)
        {
            // Fallback: rotate towards moving direction
            Quaternion targetRot = Quaternion.LookRotation(moveInput, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }
    }

    private void PerformSwing()
    {
        lastSwingTime = Time.time;
        isSwinging = true;
        swingAnimationTimer = 0f;

        // Swing sound
        if (SoundEffects.Instance != null)
        {
            SoundEffects.Instance.PlaySwing();
        }

        // Spawn visual swing slash arc
        SpawnSwingSlashEffect();

        // Search for ball within hit radius
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
                // Check if in front of player
                float angle = Vector3.Angle(transform.forward, toBall);
                if (angle <= hitAngle * 0.5f)
                {
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        targetBall = ball;
                    }
                }
            }
        }

        if (targetBall != null)
        {
            // Strike the ball in facing direction (or towards mouse)
            Vector3 strikeDir = transform.forward;
            targetBall.HitByPlayer(strikeDir, hitForce);
        }
    }

    private void HandleBatAnimation()
    {
        if (!isSwinging || batTransform == null) return;

        swingAnimationTimer += Time.deltaTime;
        float progress = swingAnimationTimer / 0.18f;

        if (progress <= 1f)
        {
            // Rapid swing rotation arc
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

        // Particle arc
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
    }
}
