using UnityEngine;

/// <summary>
/// SoundEffects - คอมโพเนนต์ความเข้ากันได้ย้อนหลัง (Compatibility Wrapper)
/// ส่งต่อคำสั่งเสียงทั้งหมดไปยัง SoundManager.Instance โดยตรง
/// เพื่อให้โค้ดเดิมหรือเครื่องมือเดิมใน Hierarchy/Editor ทำงานต่อได้ 100% โดยไม่ติดขัด
/// สามารถปรับความดัง Master Volume ใน Inspector ได้เช่นกัน
/// </summary>
public class SoundEffects : MonoBehaviour
{
    public static SoundEffects Instance { get; private set; }

    [Header("Master Volume (ปรับความดังรวมที่นี่ 0 ถึง 1)")]
    [Range(0f, 1f), Tooltip("ปรับความดังรวมทั้งหมด มีผลทันทีแบบ Real-time")]
    public float masterVolume = 1.0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // ตรวจสอบว่ามี SoundManager หรือยัง ถ้ายังไม่มีให้สร้างขึ้นมา
        if (SoundManager.Instance == null)
        {
            if (GetComponent<SoundManager>() == null)
            {
                gameObject.AddComponent<SoundManager>();
            }
        }

        if (SoundManager.Instance != null)
        {
            masterVolume = SoundManager.Instance.masterVolume;
        }
    }

    private void OnValidate()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.masterVolume = masterVolume;
            SoundManager.Instance.UpdateHoverEngineVolume();
        }
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        SoundManager.Instance?.SetMasterVolume(masterVolume);
    }

    public void PlaySwing() => SoundManager.Instance?.PlaySwing();
    public void PlayHitBall(Vector3? pos = null) => SoundManager.Instance?.PlayHitBall(pos);
    public void PlayBounce(Vector3? pos = null) => SoundManager.Instance?.PlayBounce(pos);
    public void PlayEnemyHurt(Vector3? pos = null) => SoundManager.Instance?.PlayEnemyHurt(pos);
    public void PlayEnemyDie(Vector3? pos = null) => SoundManager.Instance?.PlayEnemyDie(pos);
    public void PlayPlayerHurt() => SoundManager.Instance?.PlayPlayerHurt();
    public void PlayPlayerDeath() => SoundManager.Instance?.PlayPlayerDeath();
    public void PlayButtonClick() => SoundManager.Instance?.PlayButtonClick();
    public void PlayDash() => SoundManager.Instance?.PlayDash();
    public void PlayMagnetSkill() => SoundManager.Instance?.PlayMagnetSkill();
}
