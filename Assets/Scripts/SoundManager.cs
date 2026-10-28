using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SoundManager - ระบบจัดการเสียง (ปิดระบบ Auto-Create ไม่แอบสร้างตัวเองใน Hierarchy)
/// </summary>
public class SoundManager : MonoBehaviour
{
    private static SoundManager _instance;

    // ❌ ปรับแก้ไม่ให้สั่ง new GameObject หรือ addComponent เองเมื่อหาไม่เจอ
    public static SoundManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<SoundManager>();
            }
            return _instance;
        }
        private set => _instance = value;
    }

    [Header("=== การตั้งค่าระดับเสียงรวม (Master Volume Settings) ===")]
    [Range(0f, 1f), Tooltip("ระดับเสียงหลักทั้งหมดในเกม")]
    public float masterVolume = 1.0f;

    [Range(0f, 1f), Tooltip("ระดับเสียงเอฟเฟกต์ SFX รวม")]
    public float sfxVolume = 1.0f;

    [Range(0f, 1f), Tooltip("ระดับเสียงเอฟเฟกต์ฝั่งผู้เล่น")]
    public float playerVolume = 1.0f;

    [Range(0f, 1f), Tooltip("ระดับเสียงเอฟเฟกต์ฝั่งศัตรู")]
    public float enemyVolume = 1.0f;

    [Range(0f, 1f), Tooltip("ระดับเสียง UI / เมนู")]
    public float uiVolume = 1.0f;

    [Tooltip("เปิด = บันทึกและดึงค่าเสียงจาก PlayerPrefs อัตโนมัติ")]
    public bool usePlayerPrefs = false;

    [Header("--- 1. เสียงฝั่งผู้เล่น (Player Drone) ---")]
    public AudioClip playerHoverEngineClip;
    [Range(0f, 2f)] public float playerHoverVolume = 0.5f;

    public AudioClip playerDashClip;
    [Range(0f, 2f)] public float playerDashVolume = 1.0f;

    public AudioClip playerHurtClip;
    [Range(0f, 2f)] public float playerHurtVolume = 1.0f;

    public AudioClip playerDeathClip;
    [Range(0f, 2f)] public float playerDeathVolume = 1.2f;

    [Header("--- 2. เสียงไม้ตีและสกิล (Bat & Skills) ---")]
    public AudioClip batSwingClip;
    [Range(0f, 2f)] public float batSwingVolume = 0.8f;

    public AudioClip batHitBallClip;
    [Range(0f, 2f)] public float batHitBallVolume = 1.2f;

    public AudioClip magnetSkillClip;
    [Range(0f, 2f)] public float magnetSkillVolume = 0.9f;

    [Header("--- 3. เสียงลูกบอลพินบอล (Bouncing Ball) ---")]
    public AudioClip ballBounceClip;
    [Range(0f, 2f)] public float ballBounceVolume = 0.85f;

    public AudioClip ballHighSpeedBoostClip;
    [Range(0f, 2f)] public float ballHighSpeedBoostVolume = 1.0f;

    public AudioClip ballIceEffectClip;
    [Range(0f, 2f)] public float ballIceEffectVolume = 1.0f;

    public AudioClip ballExplodeEffectClip;
    [Range(0f, 2f)] public float ballExplodeVolume = 1.3f;

    [Header("--- 4. เสียงฝั่งศัตรู (Enemy Drones) ---")]
    public AudioClip enemySpawnClip;
    [Range(0f, 2f)] public float enemySpawnVolume = 0.9f;

    public AudioClip enemyMovementClip;
    [Range(0f, 2f)] public float enemyMovementVolume = 0.6f;

    public AudioClip enemyTelegraphWarningClip;
    [Range(0f, 2f)] public float enemyTelegraphVolume = 1.0f;

    public AudioClip enemyHurtClip;
    [Range(0f, 2f)] public float enemyHurtVolume = 1.0f;

    public AudioClip enemyDieExplosionClip;
    [Range(0f, 2f)] public float enemyDieExplosionVolume = 1.4f;

    [Header("--- 5. เสียงสิ่งกีดขวางและ UI (Environment & UI) ---")]
    public AudioClip bumperHitClip;
    [Range(0f, 2f)] public float bumperHitVolume = 1.0f;

    public AudioClip buttonClickClip;
    [Range(0f, 2f)] public float buttonClickVolume = 0.9f;

    // AudioSources
    private AudioSource sfx2DAudioSource;
    private AudioSource hoverEngineSource;

    // Procedural Fallback Clips
    private AudioClip procHoverEngineClip;
    private AudioClip procDashClip;
    private AudioClip procPlayerHurtClip;
    private AudioClip procPlayerDeathClip;
    private AudioClip procSwingClip;
    private AudioClip procHitBallClip;
    private AudioClip procMagnetSkillClip;
    private AudioClip procBounceClip;
    private AudioClip procHighSpeedBoostClip;
    private AudioClip procIceEffectClip;
    private AudioClip procBallExplodeClip;
    private AudioClip procEnemySpawnClip;
    private AudioClip procEnemyMovementClip;
    private AudioClip procEnemyTelegraphWarningClip;
    private AudioClip procEnemyHurtClip;
    private AudioClip procEnemyDieExplosionClip;
    private AudioClip procBumperHitClip;
    private AudioClip procButtonClickClip;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;

        if (usePlayerPrefs)
        {
            LoadSavedVolumePreferences();
        }

        EnsureAudioListener();
        SetupAudioSources();
        GenerateProceduralClips();
        AutoFindExistingSoundAssets();
    }

    private void Start()
    {
        EnsureAudioListener();
        StartHoverEngine();
    }

    private void Update()
    {
        AudioListener.volume = masterVolume;
        UpdateHoverEngineVolume();
    }

    private void OnValidate()
    {
        masterVolume = Mathf.Clamp01(masterVolume);
        sfxVolume = Mathf.Clamp01(sfxVolume);
        playerVolume = Mathf.Clamp01(playerVolume);
        enemyVolume = Mathf.Clamp01(enemyVolume);
        uiVolume = Mathf.Clamp01(uiVolume);

        AudioListener.volume = masterVolume;
        UpdateHoverEngineVolume();
    }

    public void EnsureAudioListener()
    {
        AudioListener listener = FindAnyObjectByType<AudioListener>();
        if (listener == null)
        {
            Camera cam = Camera.main ?? FindAnyObjectByType<Camera>();
            if (cam != null)
            {
                cam.gameObject.AddComponent<AudioListener>();
            }
            else
            {
                gameObject.AddComponent<AudioListener>();
            }
        }
        AudioListener.volume = masterVolume;
    }

    public void UpdateHoverEngineVolume()
    {
        if (hoverEngineSource != null && hoverEngineSource.isPlaying)
        {
            float targetVol = Mathf.Clamp01(sfxVolume * playerVolume * playerHoverVolume);
            hoverEngineSource.volume = targetVol;
        }
    }

    #region Audio Sources Setup
    private void SetupAudioSources()
    {
        sfx2DAudioSource = gameObject.AddComponent<AudioSource>();
        sfx2DAudioSource.playOnAwake = false;
        sfx2DAudioSource.spatialBlend = 0f;

        hoverEngineSource = gameObject.AddComponent<AudioSource>();
        hoverEngineSource.playOnAwake = false;
        hoverEngineSource.loop = true;
        hoverEngineSource.spatialBlend = 0f;
    }

    private void AutoFindExistingSoundAssets()
    {
        if (playerHoverEngineClip == null)
        {
            AudioClip hover = Resources.Load<AudioClip>("Sound/hover");
            if (hover != null) playerHoverEngineClip = hover;
        }
        if (ballExplodeEffectClip == null || enemyDieExplosionClip == null)
        {
            AudioClip boom = Resources.Load<AudioClip>("Sound/boomm");
            if (boom != null)
            {
                if (ballExplodeEffectClip == null) ballExplodeEffectClip = boom;
                if (enemyDieExplosionClip == null) enemyDieExplosionClip = boom;
            }
        }
    }
    #endregion

    #region Volume Control Methods
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        AudioListener.volume = masterVolume;
        if (usePlayerPrefs) PlayerPrefs.SetFloat("Sound_MasterVolume", masterVolume);
        UpdateHoverEngineVolume();
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (usePlayerPrefs) PlayerPrefs.SetFloat("Sound_SFXVolume", sfxVolume);
        UpdateHoverEngineVolume();
    }

    public void SetPlayerVolume(float volume)
    {
        playerVolume = Mathf.Clamp01(volume);
        if (usePlayerPrefs) PlayerPrefs.SetFloat("Sound_PlayerVolume", playerVolume);
        UpdateHoverEngineVolume();
    }

    public void SetEnemyVolume(float volume)
    {
        enemyVolume = Mathf.Clamp01(volume);
        if (usePlayerPrefs) PlayerPrefs.SetFloat("Sound_EnemyVolume", enemyVolume);
    }

    public void SetUIVolume(float volume)
    {
        uiVolume = Mathf.Clamp01(volume);
        if (usePlayerPrefs) PlayerPrefs.SetFloat("Sound_UIVolume", uiVolume);
    }

    public AudioClip GetHoverEngineClip() => playerHoverEngineClip != null ? playerHoverEngineClip : procHoverEngineClip;
    public AudioClip GetDashClip() => playerDashClip != null ? playerDashClip : procDashClip;
    public AudioClip GetPlayerHurtClip() => playerHurtClip != null ? playerHurtClip : procPlayerHurtClip;
    public AudioClip GetPlayerDeathClip() => playerDeathClip != null ? playerDeathClip : procPlayerDeathClip;
    public AudioClip GetSwingClip() => batSwingClip != null ? batSwingClip : procSwingClip;
    public AudioClip GetHitBallClip() => batHitBallClip != null ? batHitBallClip : procHitBallClip;
    public AudioClip GetMagnetSkillClip() => magnetSkillClip != null ? magnetSkillClip : procMagnetSkillClip;
    public AudioClip GetBounceClip() => ballBounceClip != null ? ballBounceClip : procBounceClip;
    public AudioClip GetHighSpeedBoostClip() => ballHighSpeedBoostClip != null ? ballHighSpeedBoostClip : procHighSpeedBoostClip;
    public AudioClip GetIceEffectClip() => ballIceEffectClip != null ? ballIceEffectClip : procIceEffectClip;
    public AudioClip GetBallExplodeClip() => ballExplodeEffectClip != null ? ballExplodeEffectClip : procBallExplodeClip;
    public AudioClip GetEnemySpawnClip() => enemySpawnClip != null ? enemySpawnClip : procEnemySpawnClip;
    public AudioClip GetEnemyMovementClip() => enemyMovementClip != null ? enemyMovementClip : procEnemyMovementClip;
    public AudioClip GetEnemyTelegraphClip() => enemyTelegraphWarningClip != null ? enemyTelegraphWarningClip : procEnemyTelegraphWarningClip;
    public AudioClip GetEnemyHurtClip() => enemyHurtClip != null ? enemyHurtClip : procEnemyHurtClip;
    public AudioClip GetEnemyDieClip() => enemyDieExplosionClip != null ? enemyDieExplosionClip : procEnemyDieExplosionClip;
    public AudioClip GetBumperHitClip() => bumperHitClip != null ? bumperHitClip : procBumperHitClip;
    public AudioClip GetButtonClickClip() => buttonClickClip != null ? buttonClickClip : procButtonClickClip;

    private void LoadSavedVolumePreferences()
    {
        masterVolume = PlayerPrefs.GetFloat("Sound_MasterVolume", masterVolume);
        sfxVolume = PlayerPrefs.GetFloat("Sound_SFXVolume", sfxVolume);
        playerVolume = PlayerPrefs.GetFloat("Sound_PlayerVolume", playerVolume);
        enemyVolume = PlayerPrefs.GetFloat("Sound_EnemyVolume", enemyVolume);
        uiVolume = PlayerPrefs.GetFloat("Sound_UIVolume", uiVolume);
    }
    #endregion

    #region Playback Functions
    public void StartHoverEngine()
    {
        if (hoverEngineSource == null) return;
        AudioClip clip = playerHoverEngineClip != null ? playerHoverEngineClip : procHoverEngineClip;
        if (clip != null)
        {
            hoverEngineSource.clip = clip;
            hoverEngineSource.volume = masterVolume * sfxVolume * playerVolume * playerHoverVolume;
            if (!hoverEngineSource.isPlaying)
            {
                hoverEngineSource.Play();
            }
        }
    }

    public void StopHoverEngine()
    {
        if (hoverEngineSource != null && hoverEngineSource.isPlaying)
        {
            hoverEngineSource.Stop();
        }
    }

    public void SetHoverEnginePitch(float pitch)
    {
        if (hoverEngineSource != null)
        {
            hoverEngineSource.pitch = Mathf.Clamp(pitch, 0.8f, 1.4f);
        }
    }

    public void PlayDash()
    {
        AudioClip clip = playerDashClip != null ? playerDashClip : procDashClip;
        float vol = masterVolume * sfxVolume * playerVolume * playerDashVolume;
        Play2D(clip, vol, Random.Range(0.95f, 1.08f));
    }

    public void PlayPlayerHurt()
    {
        AudioClip clip = playerHurtClip != null ? playerHurtClip : procPlayerHurtClip;
        float vol = masterVolume * sfxVolume * playerVolume * playerHurtVolume;
        Play2D(clip, vol, Random.Range(0.92f, 1.1f));
    }

    public void PlayPlayerDeath()
    {
        StopHoverEngine();
        AudioClip clip = playerDeathClip != null ? playerDeathClip : procPlayerDeathClip;
        float vol = masterVolume * sfxVolume * playerVolume * playerDeathVolume;
        Play2D(clip, vol, 0.85f);
    }

    public void PlaySwing()
    {
        AudioClip clip = batSwingClip != null ? batSwingClip : procSwingClip;
        float vol = masterVolume * sfxVolume * playerVolume * batSwingVolume;
        Play2D(clip, vol, Random.Range(0.95f, 1.12f));
    }

    public void PlayHitBall(Vector3? pos = null)
    {
        AudioClip clip = batHitBallClip != null ? batHitBallClip : procHitBallClip;
        float vol = masterVolume * sfxVolume * playerVolume * batHitBallVolume;
        PlaySound(clip, pos, vol, Random.Range(0.94f, 1.08f));
    }

    public void PlayMagnetSkill()
    {
        AudioClip clip = magnetSkillClip != null ? magnetSkillClip : procMagnetSkillClip;
        float vol = masterVolume * sfxVolume * playerVolume * magnetSkillVolume;
        Play2D(clip, vol, 1.0f);
    }

    public void PlayBounce(Vector3? pos = null)
    {
        AudioClip clip = ballBounceClip != null ? ballBounceClip : procBounceClip;
        float vol = masterVolume * sfxVolume * ballBounceVolume;
        PlaySound(clip, pos, vol, Random.Range(0.9f, 1.15f));
    }

    public void PlayHighSpeedBoost(Vector3? pos = null)
    {
        AudioClip clip = ballHighSpeedBoostClip != null ? ballHighSpeedBoostClip : procHighSpeedBoostClip;
        float vol = masterVolume * sfxVolume * ballHighSpeedBoostVolume;
        PlaySound(clip, pos, vol, Random.Range(0.98f, 1.1f));
    }

    public void PlayIceEffect(Vector3? pos = null)
    {
        AudioClip clip = ballIceEffectClip != null ? ballIceEffectClip : procIceEffectClip;
        float vol = masterVolume * sfxVolume * ballIceEffectVolume;
        PlaySound(clip, pos, vol, Random.Range(0.95f, 1.05f));
    }

    public void PlayBallExplode(Vector3? pos = null)
    {
        AudioClip clip = ballExplodeEffectClip != null ? ballExplodeEffectClip : procBallExplodeClip;
        float vol = masterVolume * sfxVolume * ballExplodeVolume;
        PlaySound(clip, pos, vol, Random.Range(0.88f, 1.05f));
    }

    public void PlayEnemySpawn(Vector3? pos = null)
    {
        AudioClip clip = enemySpawnClip != null ? enemySpawnClip : procEnemySpawnClip;
        float vol = masterVolume * sfxVolume * enemyVolume * enemySpawnVolume;
        PlaySound(clip, pos, vol, Random.Range(0.96f, 1.04f));
    }

    public void PlayEnemyMovement(Vector3? pos = null)
    {
        AudioClip clip = enemyMovementClip != null ? enemyMovementClip : procEnemyMovementClip;
        float vol = masterVolume * sfxVolume * enemyVolume * enemyMovementVolume;
        PlaySound(clip, pos, vol, Random.Range(0.9f, 1.1f));
    }

    public void PlayTelegraphWarning(Vector3? pos = null)
    {
        AudioClip clip = enemyTelegraphWarningClip != null ? enemyTelegraphWarningClip : procEnemyTelegraphWarningClip;
        float vol = masterVolume * sfxVolume * enemyVolume * enemyTelegraphVolume;
        PlaySound(clip, pos, vol, 1.0f);
    }

    public void PlayEnemyHurt(Vector3? pos = null)
    {
        AudioClip clip = enemyHurtClip != null ? enemyHurtClip : procEnemyHurtClip;
        float vol = masterVolume * sfxVolume * enemyVolume * enemyHurtVolume;
        PlaySound(clip, pos, vol, Random.Range(0.92f, 1.15f));
    }

    public void PlayEnemyDie(Vector3? pos = null)
    {
        AudioClip clip = enemyDieExplosionClip != null ? enemyDieExplosionClip : procEnemyDieExplosionClip;
        float vol = masterVolume * sfxVolume * enemyVolume * enemyDieExplosionVolume;
        PlaySound(clip, pos, vol, Random.Range(0.85f, 1.05f));
    }

    public void PlayBumperHit(Vector3? pos = null)
    {
        AudioClip clip = bumperHitClip != null ? bumperHitClip : procBumperHitClip;
        float vol = masterVolume * sfxVolume * bumperHitVolume;
        PlaySound(clip, pos, vol, Random.Range(0.95f, 1.1f));
    }

    public void PlayButtonClick()
    {
        AudioClip clip = buttonClickClip != null ? buttonClickClip : procButtonClickClip;
        float vol = masterVolume * sfxVolume * uiVolume * buttonClickVolume;
        Play2D(clip, vol, 1.0f);
    }
    #endregion

    #region Helper Playback Methods
    private void Play2D(AudioClip clip, float volume, float pitch = 1.0f)
    {
        if (clip == null || sfx2DAudioSource == null) return;
        sfx2DAudioSource.pitch = pitch;
        sfx2DAudioSource.PlayOneShot(clip, volume);
    }

    private void PlaySound(AudioClip clip, Vector3? pos, float volume, float pitch = 1.0f)
    {
        if (clip == null) return;

        if (pos.HasValue)
        {
            PlayClipAtPoint(clip, pos.Value, volume, pitch);
        }
        else
        {
            Play2D(clip, volume, pitch);
        }
    }

    public void PlayClipAtPoint(AudioClip clip, Vector3 position, float volume, float pitch = 1.0f)
    {
        if (clip == null) return;

        GameObject tempAudioObj = new GameObject("TempAudioSource");
        tempAudioObj.transform.position = position;

        AudioSource source = tempAudioObj.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.pitch = pitch;
        source.spatialBlend = 0.65f;
        source.minDistance = 3f;
        source.maxDistance = 45f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.Play();

        Destroy(tempAudioObj, clip.length / Mathf.Max(0.1f, pitch) + 0.1f);
    }
    #endregion

    #region Procedural Audio Synthesizers
    private void GenerateProceduralClips()
    {
        procHoverEngineClip = CreateDroneHumClip("Proc_HoverEngine", 1.5f, 120f);
        procDashClip = CreateWhooshClip("Proc_DashBurst", 0.18f, 700f, 180f);
        procPlayerHurtClip = CreateElectricSparkClip("Proc_PlayerHurt", 0.2f, 900f, 180f);
        procPlayerDeathClip = CreateSystemShutdownClip("Proc_PlayerDeath", 0.9f, 320f, 40f);

        procSwingClip = CreateWhooshClip("Proc_SwingWhoosh", 0.14f, 480f, 160f);
        procHitBallClip = CreateHeavyImpactClip("Proc_HitBall", 0.22f, 680f, 110f);
        procMagnetSkillClip = CreateTractorBeamClip("Proc_MagnetSkill", 0.45f, 440f);

        procBounceClip = CreateMetallicClinkClip("Proc_BounceClink", 0.12f, 1400f, 850f);
        procHighSpeedBoostClip = CreatePlasmaRushClip("Proc_HighSpeedBoost", 0.3f, 500f, 1200f);
        procIceEffectClip = CreateIceShatterClip("Proc_IceShatter", 0.35f);
        procBallExplodeClip = CreateRobotExplosionClip("Proc_BallExplode", 0.5f, 180f, 35f);

        procEnemySpawnClip = CreateBootUpSequenceClip("Proc_EnemySpawn", 0.4f);
        procEnemyMovementClip = CreateServoMotorClip("Proc_EnemyMovement", 0.25f, 360f);
        procEnemyTelegraphWarningClip = CreateAlarmBeepClip("Proc_TelegraphWarning", 0.25f, 880f);
        procEnemyHurtClip = CreateSparksCrunchClip("Proc_EnemyHurt", 0.2f, 450f, 120f);
        procEnemyDieExplosionClip = CreateRobotExplosionClip("Proc_EnemyDie", 0.65f, 220f, 30f);

        procBumperHitClip = CreateArcadeChimeClip("Proc_BumperHit", 0.2f, 600f, 1200f);
        procButtonClickClip = CreateFuturisticBeepClip("Proc_ButtonClick", 0.08f, 880f, 1250f);
    }

    private AudioClip CreateDroneHumClip(string name, float duration, float baseFreq)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(duration * sampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float fundamental = Mathf.Sin(2f * Mathf.PI * baseFreq * t);
            float harmonic1 = Mathf.Sin(2f * Mathf.PI * baseFreq * 2f * t) * 0.4f;
            float harmonic2 = Mathf.Sin(2f * Mathf.PI * baseFreq * 3f * t) * 0.2f;
            float wobble = Mathf.Sin(2f * Mathf.PI * 6f * t) * 0.15f;
            float noise = (Random.value * 2f - 1f) * 0.05f;
            samples[i] = (fundamental + harmonic1 + harmonic2 + wobble + noise) * 0.3f;
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateWhooshClip(string name, float duration, float startFreq, float endFreq)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(duration * sampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float freq = Mathf.Lerp(startFreq, endFreq, t);
            float env = Mathf.Sin(t * Mathf.PI);
            float noise = (Random.value * 2f - 1f) * 0.7f;
            float sine = Mathf.Sin(2f * Mathf.PI * freq * (i / (float)sampleRate)) * 0.3f;
            samples[i] = (sine + noise) * env;
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateHeavyImpactClip(string name, float duration, float startFreq, float endFreq)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(duration * sampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float freq = Mathf.Lerp(startFreq, endFreq, t * t);
            float env = Mathf.Exp(-t * 13f);
            float bass = Mathf.Sin(2f * Mathf.PI * freq * (i / (float)sampleRate)) * 0.75f;
            float metalRing = Mathf.Sin(2f * Mathf.PI * 1850f * (i / (float)sampleRate)) * Mathf.Exp(-t * 22f) * 0.35f;
            float punch = (Random.value * 2f - 1f) * Mathf.Exp(-t * 35f) * 0.5f;
            samples[i] = (bass + metalRing + punch) * env;
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateMetallicClinkClip(string name, float duration, float highFreq, float lowFreq)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(duration * sampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float env = Mathf.Exp(-t * 16f);
            float tone1 = Mathf.Sin(2f * Mathf.PI * highFreq * (i / (float)sampleRate)) * 0.6f;
            float tone2 = Mathf.Sin(2f * Mathf.PI * lowFreq * (i / (float)sampleRate)) * 0.4f;
            float noise = (Random.value * 2f - 1f) * Mathf.Exp(-t * 40f) * 0.2f;
            samples[i] = (tone1 + tone2 + noise) * env;
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateElectricSparkClip(string name, float duration, float startFreq, float endFreq)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(duration * sampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float env = Mathf.Exp(-t * 12f);
            float freq = Mathf.Lerp(startFreq, endFreq, t);
            float sparkNoise = (Random.value > 0.6f ? 1f : -1f) * Random.value * 0.65f;
            float buzz = Mathf.Sin(2f * Mathf.PI * freq * (i / (float)sampleRate)) * 0.4f;
            samples[i] = (sparkNoise + buzz) * env;
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateSystemShutdownClip(string name, float duration, float startFreq, float endFreq)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(duration * sampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float freq = Mathf.Lerp(startFreq, endFreq, Mathf.Pow(t, 1.8f));
            float env = (1f - t);
            float sine = Mathf.Sin(2f * Mathf.PI * freq * (i / (float)sampleRate)) * 0.7f;
            float electricGlitch = (Random.value * 2f - 1f) * Mathf.Exp(-t * 7f) * 0.35f;
            samples[i] = (sine + electricGlitch) * env;
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateTractorBeamClip(string name, float duration, float baseFreq)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(duration * sampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float lfo = Mathf.Sin(2f * Mathf.PI * 14f * t) * 60f;
            float tone = Mathf.Sin(2f * Mathf.PI * (baseFreq + lfo) * t) * 0.6f;
            float sub = Mathf.Sin(2f * Mathf.PI * (baseFreq * 0.5f) * t) * 0.3f;
            float env = Mathf.Sin((float)i / sampleCount * Mathf.PI);
            samples[i] = (tone + sub) * env;
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreatePlasmaRushClip(string name, float duration, float startFreq, float endFreq)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(duration * sampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float freq = Mathf.Lerp(startFreq, endFreq, t);
            float env = Mathf.Sin(t * Mathf.PI);
            float tone = Mathf.Sin(2f * Mathf.PI * freq * (i / (float)sampleRate)) * 0.5f;
            float noise = (Random.value * 2f - 1f) * 0.45f;
            samples[i] = (tone + noise) * env;
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateIceShatterClip(string name, float duration)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(duration * sampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float env = Mathf.Exp(-t * 9f);
            float highBell1 = Mathf.Sin(2f * Mathf.PI * 2800f * (i / (float)sampleRate)) * 0.3f;
            float highBell2 = Mathf.Sin(2f * Mathf.PI * 3950f * (i / (float)sampleRate)) * 0.25f;
            float glassCrack = (Random.value * 2f - 1f) * 0.45f;
            samples[i] = (highBell1 + highBell2 + glassCrack) * env;
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateRobotExplosionClip(string name, float duration, float startFreq, float endFreq)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(duration * sampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float freq = Mathf.Lerp(startFreq, endFreq, t);
            float env = Mathf.Exp(-t * 5.5f);
            float noise = (Random.value * 2f - 1f) * 0.8f;
            float subBass = Mathf.Sin(2f * Mathf.PI * freq * (i / (float)sampleRate)) * 0.6f;
            samples[i] = (noise + subBass) * env;
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateBootUpSequenceClip(string name, float duration)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(duration * sampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float freq = t < 0.25f ? 523.25f : (t < 0.5f ? 659.25f : (t < 0.75f ? 783.99f : 1046.50f));
            float env = Mathf.Sin(t * Mathf.PI);
            float tone = Mathf.Sin(2f * Mathf.PI * freq * (i / (float)sampleRate)) * 0.5f;
            samples[i] = tone * env;
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateServoMotorClip(string name, float duration, float freq)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(duration * sampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float env = Mathf.Sin(t * Mathf.PI);
            float buzz = Mathf.Sin(2f * Mathf.PI * freq * (i / (float)sampleRate)) * 0.4f;
            float motorHarmonic = Mathf.Sin(2f * Mathf.PI * (freq * 2.2f) * (i / (float)sampleRate)) * 0.2f;
            samples[i] = (buzz + motorHarmonic) * env;
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateAlarmBeepClip(string name, float duration, float freq)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(duration * sampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float env = Mathf.Exp(-t * 6f);
            float squareish = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * freq * (i / (float)sampleRate))) * 0.45f;
            samples[i] = squareish * env;
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateSparksCrunchClip(string name, float duration, float startFreq, float endFreq)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(duration * sampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float freq = Mathf.Lerp(startFreq, endFreq, t);
            float env = Mathf.Exp(-t * 11f);
            float noise = (Random.value * 2f - 1f) * 0.6f;
            float metal = Mathf.Sin(2f * Mathf.PI * freq * (i / (float)sampleRate)) * 0.4f;
            samples[i] = (noise + metal) * env;
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateArcadeChimeClip(string name, float duration, float startFreq, float endFreq)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(duration * sampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float freq = Mathf.Lerp(startFreq, endFreq, Mathf.Sqrt(t));
            float env = Mathf.Exp(-t * 10f);
            float tone = Mathf.Sin(2f * Mathf.PI * freq * (i / (float)sampleRate)) * 0.6f;
            samples[i] = tone * env;
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateFuturisticBeepClip(string name, float duration, float startFreq, float endFreq)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(duration * sampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float freq = Mathf.Lerp(startFreq, endFreq, t);
            float env = Mathf.Exp(-t * 14f);
            float tone = Mathf.Sin(2f * Mathf.PI * freq * (i / (float)sampleRate)) * 0.65f;
            samples[i] = tone * env;
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
    #endregion
}