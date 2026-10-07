using UnityEngine;

/// <summary>
/// Bird's Eye View Camera Controller:
/// - High-angle top-down bird's eye view framing the arena and player.
/// - Smooth tracking with configurable offset.
/// - Punchy camera shake for strikes and impacts.
/// </summary>
public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [Header("Target & Follow")]
    public Transform target;
    public Vector3 offset = new Vector3(0, 18f, -10f);
    public float smoothSpeed = 6f;
    public float pitchAngle = 60f;

    [Header("Camera Shake")]
    private float shakeTimer = 0f;
    private float shakeMagnitude = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        // Set top-down bird eye view angle
        transform.rotation = Quaternion.Euler(pitchAngle, 0, 0);
    }

    private void Start()
    {
        if (target == null)
        {
            PlayerController pc = FindFirstObjectByType<PlayerController>();
            if (pc != null)
            {
                target = pc.transform;
            }
        }
    }

    private void LateUpdate()
    {
        Vector3 targetPos = (target != null) ? target.position + offset : offset;

        // Apply shake
        if (shakeTimer > 0f)
        {
            Vector3 shakeOffset = Random.insideUnitSphere * shakeMagnitude;
            shakeOffset.y *= 0.5f;
            targetPos += shakeOffset;

            shakeTimer -= Time.deltaTime;
        }

        // Smooth follow
        transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(pitchAngle, 0, 0);
    }

    /// <summary>
    /// Triggers screen shake for hits and impacts.
    /// </summary>
    public void ShakeCamera(float duration, float magnitude)
    {
        shakeTimer = Mathf.Max(shakeTimer, duration);
        shakeMagnitude = Mathf.Max(shakeMagnitude, magnitude);
    }
}
