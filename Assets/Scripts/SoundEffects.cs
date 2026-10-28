
using UnityEngine;

/// <summary>
/// SoundEffects - ส่งต่อคำสั่งเสียงไปยัง SoundManager
/// พร้อมช่องตั้งค่าเพลงพื้นหลังใน Inspector
/// </summary>
public class SoundEffects : MonoBehaviour
{
    public static SoundEffects Instance { get; private set; }

    [Header("Master Volume")]
    [Range(0f, 1f)]
    public float masterVolume = 1.0f;

    [Header("Background Music")]
    [Tooltip("ลากไฟล์เพลงพื้นหลังมาใส่ตรงนี้")]
    public AudioClip backgroundMusicClip;

    [Range(0f, 1f)]
    public float bgmVolume = 0.5f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

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

    private void Start()
    {
        SoundManager manager = SoundManager.Instance;

        if (manager == null)
            return;

        if (backgroundMusicClip != null)
        {
            manager.backgroundMusicClip = backgroundMusicClip;
            manager.SetBGMVolume(bgmVolume);
            manager.PlayBackgroundMusic();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnValidate()
    {
        masterVolume = Mathf.Clamp01(masterVolume);
        bgmVolume = Mathf.Clamp01(bgmVolume);

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.masterVolume = masterVolume;
            SoundManager.Instance.SetBGMVolume(bgmVolume);
            SoundManager.Instance.UpdateHoverEngineVolume();
        }
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        SoundManager.Instance?.SetMasterVolume(masterVolume);
    }

    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        SoundManager.Instance?.SetBGMVolume(bgmVolume);
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
