using UnityEngine;

/// <summary>
/// Generates procedural sound effects so the game has rich audio without needing external assets.
/// </summary>
public class SoundEffects : MonoBehaviour
{
    public static SoundEffects Instance { get; private set; }

    private AudioSource audioSource;

    private AudioClip swingClip;
    private AudioClip hitBallClip;
    private AudioClip bounceClip;
    private AudioClip enemyHurtClip;
    private AudioClip enemyDieClip;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        GenerateClips();
    }

    private void GenerateClips()
    {
        swingClip = CreateWhooshClip("SwingWhoosh", 0.15f, 400f, 150f);
        hitBallClip = CreateImpactClip("HitBall", 0.2f, 650f, 180f);
        bounceClip = CreateToneClip("BouncePing", 0.12f, 520f, 680f);
        enemyHurtClip = CreateSquishClip("EnemyHurt", 0.18f, 320f, 90f);
        enemyDieClip = CreateExplosionClip("EnemyDie", 0.35f, 220f, 40f);
    }

    public void PlaySwing() => Play(swingClip, 0.7f, Random.Range(0.95f, 1.05f));
    public void PlayHitBall() => Play(hitBallClip, 1.0f, Random.Range(0.95f, 1.15f));
    public void PlayBounce() => Play(bounceClip, 0.6f, Random.Range(0.9f, 1.1f));
    public void PlayEnemyHurt() => Play(enemyHurtClip, 0.9f, Random.Range(0.9f, 1.2f));
    public void PlayEnemyDie() => Play(enemyDieClip, 1.0f, Random.Range(0.85f, 1.05f));

    private void Play(AudioClip clip, float volume, float pitch)
    {
        if (clip == null || audioSource == null) return;
        audioSource.pitch = pitch;
        audioSource.PlayOneShot(clip, volume);
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
            float noise = (Random.value * 2f - 1f) * 0.4f;
            float sine = Mathf.Sin(2f * Mathf.PI * freq * (i / (float)sampleRate)) * 0.6f;
            samples[i] = (sine + noise) * env;
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateImpactClip(string name, float duration, float startFreq, float endFreq)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(duration * sampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float freq = Mathf.Lerp(startFreq, endFreq, t * t);
            float env = Mathf.Exp(-t * 12f);
            float sine = Mathf.Sin(2f * Mathf.PI * freq * (i / (float)sampleRate));
            float noise = (Random.value * 2f - 1f) * 0.3f * Mathf.Exp(-t * 20f);
            samples[i] = (sine + noise) * env;
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateToneClip(string name, float duration, float startFreq, float endFreq)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(duration * sampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float freq = Mathf.Lerp(startFreq, endFreq, t);
            float env = Mathf.Exp(-t * 14f);
            samples[i] = Mathf.Sin(2f * Mathf.PI * freq * (i / (float)sampleRate)) * env;
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateSquishClip(string name, float duration, float startFreq, float endFreq)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(duration * sampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float freq = Mathf.Lerp(startFreq, endFreq, t);
            float env = Mathf.Exp(-t * 10f);
            float squishNoise = (Random.value * 2f - 1f) * 0.6f;
            float tone = Mathf.Sin(2f * Mathf.PI * freq * (i / (float)sampleRate)) * 0.4f;
            samples[i] = (squishNoise + tone) * env;
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateExplosionClip(string name, float duration, float startFreq, float endFreq)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(duration * sampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float freq = Mathf.Lerp(startFreq, endFreq, t);
            float env = Mathf.Exp(-t * 6f);
            float noise = (Random.value * 2f - 1f) * 0.8f;
            float sub = Mathf.Sin(2f * Mathf.PI * freq * (i / (float)sampleRate)) * 0.5f;
            samples[i] = (noise + sub) * env;
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
