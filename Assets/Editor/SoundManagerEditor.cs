using UnityEngine;
using UnityEditor;
using System.Reflection;

/// <summary>
/// Custom Inspector Editor สำหรับ SoundManager
/// ช่วยให้ปรับความดังของเสียงทุกชนิดใน Inspector ได้อย่างสะดวก พร้อมปุ่มทดสอบเสียง (Test Play) แบบเรียลไทม์
/// </summary>
[CustomEditor(typeof(SoundManager))]
public class SoundManagerEditor : Editor
{
    private SoundManager soundManager;

    private SerializedProperty masterVolumeProp;
    private SerializedProperty sfxVolumeProp;
    private SerializedProperty playerVolumeProp;
    private SerializedProperty enemyVolumeProp;
    private SerializedProperty uiVolumeProp;
    private SerializedProperty usePlayerPrefsProp;

    // Player Drone
    private SerializedProperty playerHoverEngineClipProp;
    private SerializedProperty playerHoverVolumeProp;
    private SerializedProperty playerDashClipProp;
    private SerializedProperty playerDashVolumeProp;
    private SerializedProperty playerHurtClipProp;
    private SerializedProperty playerHurtVolumeProp;
    private SerializedProperty playerDeathClipProp;
    private SerializedProperty playerDeathVolumeProp;

    // Bat & Skills
    private SerializedProperty batSwingClipProp;
    private SerializedProperty batSwingVolumeProp;
    private SerializedProperty batHitBallClipProp;
    private SerializedProperty batHitBallVolumeProp;
    private SerializedProperty magnetSkillClipProp;
    private SerializedProperty magnetSkillVolumeProp;

    // Bouncing Ball
    private SerializedProperty ballBounceClipProp;
    private SerializedProperty ballBounceVolumeProp;
    private SerializedProperty ballHighSpeedBoostClipProp;
    private SerializedProperty ballHighSpeedBoostVolumeProp;
    private SerializedProperty ballIceEffectClipProp;
    private SerializedProperty ballIceEffectVolumeProp;
    private SerializedProperty ballExplodeEffectClipProp;
    private SerializedProperty ballExplodeVolumeProp;

    // Enemy Drones
    private SerializedProperty enemySpawnClipProp;
    private SerializedProperty enemySpawnVolumeProp;
    private SerializedProperty enemyMovementClipProp;
    private SerializedProperty enemyMovementVolumeProp;
    private SerializedProperty enemyTelegraphWarningClipProp;
    private SerializedProperty enemyTelegraphVolumeProp;
    private SerializedProperty enemyHurtClipProp;
    private SerializedProperty enemyHurtVolumeProp;
    private SerializedProperty enemyDieExplosionClipProp;
    private SerializedProperty enemyDieExplosionVolumeProp;

    // Environment & UI
    private SerializedProperty bumperHitClipProp;
    private SerializedProperty bumperHitVolumeProp;
    private SerializedProperty buttonClickClipProp;
    private SerializedProperty buttonClickVolumeProp;

    private static bool showPlayer = true;
    private static bool showBat = true;
    private static bool showBall = true;
    private static bool showEnemy = true;
    private static bool showUI = true;

    private void OnEnable()
    {
        soundManager = (SoundManager)target;

        masterVolumeProp = serializedObject.FindProperty("masterVolume");
        sfxVolumeProp = serializedObject.FindProperty("sfxVolume");
        playerVolumeProp = serializedObject.FindProperty("playerVolume");
        enemyVolumeProp = serializedObject.FindProperty("enemyVolume");
        uiVolumeProp = serializedObject.FindProperty("uiVolume");
        usePlayerPrefsProp = serializedObject.FindProperty("usePlayerPrefs");

        playerHoverEngineClipProp = serializedObject.FindProperty("playerHoverEngineClip");
        playerHoverVolumeProp = serializedObject.FindProperty("playerHoverVolume");
        playerDashClipProp = serializedObject.FindProperty("playerDashClip");
        playerDashVolumeProp = serializedObject.FindProperty("playerDashVolume");
        playerHurtClipProp = serializedObject.FindProperty("playerHurtClip");
        playerHurtVolumeProp = serializedObject.FindProperty("playerHurtVolume");
        playerDeathClipProp = serializedObject.FindProperty("playerDeathClip");
        playerDeathVolumeProp = serializedObject.FindProperty("playerDeathVolume");

        batSwingClipProp = serializedObject.FindProperty("batSwingClip");
        batSwingVolumeProp = serializedObject.FindProperty("batSwingVolume");
        batHitBallClipProp = serializedObject.FindProperty("batHitBallClip");
        batHitBallVolumeProp = serializedObject.FindProperty("batHitBallVolume");
        magnetSkillClipProp = serializedObject.FindProperty("magnetSkillClip");
        magnetSkillVolumeProp = serializedObject.FindProperty("magnetSkillVolume");

        ballBounceClipProp = serializedObject.FindProperty("ballBounceClip");
        ballBounceVolumeProp = serializedObject.FindProperty("ballBounceVolume");
        ballHighSpeedBoostClipProp = serializedObject.FindProperty("ballHighSpeedBoostClip");
        ballHighSpeedBoostVolumeProp = serializedObject.FindProperty("ballHighSpeedBoostVolume");
        ballIceEffectClipProp = serializedObject.FindProperty("ballIceEffectClip");
        ballIceEffectVolumeProp = serializedObject.FindProperty("ballIceEffectVolume");
        ballExplodeEffectClipProp = serializedObject.FindProperty("ballExplodeEffectClip");
        ballExplodeVolumeProp = serializedObject.FindProperty("ballExplodeVolume");

        enemySpawnClipProp = serializedObject.FindProperty("enemySpawnClip");
        enemySpawnVolumeProp = serializedObject.FindProperty("enemySpawnVolume");
        enemyMovementClipProp = serializedObject.FindProperty("enemyMovementClip");
        enemyMovementVolumeProp = serializedObject.FindProperty("enemyMovementVolume");
        enemyTelegraphWarningClipProp = serializedObject.FindProperty("enemyTelegraphWarningClip");
        enemyTelegraphVolumeProp = serializedObject.FindProperty("enemyTelegraphVolume");
        enemyHurtClipProp = serializedObject.FindProperty("enemyHurtClip");
        enemyHurtVolumeProp = serializedObject.FindProperty("enemyHurtVolume");
        enemyDieExplosionClipProp = serializedObject.FindProperty("enemyDieExplosionClip");
        enemyDieExplosionVolumeProp = serializedObject.FindProperty("enemyDieExplosionVolume");

        bumperHitClipProp = serializedObject.FindProperty("bumperHitClip");
        bumperHitVolumeProp = serializedObject.FindProperty("bumperHitVolume");
        buttonClickClipProp = serializedObject.FindProperty("buttonClickClip");
        buttonClickVolumeProp = serializedObject.FindProperty("buttonClickVolume");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawHeaderPanel();
        DrawMasterVolumeSliders();

        EditorGUILayout.Space(8);

        DrawCategorySection("1. เสียงฝั่งผู้เล่น (Player Drone)", ref showPlayer, new Color(0.2f, 0.7f, 1f), () =>
        {
            DrawSoundRow("Hovering (Engine Loop)", playerHoverEngineClipProp, playerHoverVolumeProp, () =>
            {
                if (EditorApplication.isPlaying) soundManager.StartHoverEngine();
                else PlayPreviewClip(playerHoverEngineClipProp.objectReferenceValue as AudioClip);
            });
            DrawSoundRow("Dash (พ่นไอพ่น)", playerDashClipProp, playerDashVolumeProp, () =>
            {
                if (EditorApplication.isPlaying) soundManager.PlayDash();
                else PlayPreviewClip(playerDashClipProp.objectReferenceValue as AudioClip);
            });
            DrawSoundRow("Player Hurt (เจ็บ/ชอร์ต)", playerHurtClipProp, playerHurtVolumeProp, () =>
            {
                if (EditorApplication.isPlaying) soundManager.PlayPlayerHurt();
                else PlayPreviewClip(playerHurtClipProp.objectReferenceValue as AudioClip);
            });
            DrawSoundRow("Player Death (ระบบปิดตัว)", playerDeathClipProp, playerDeathVolumeProp, () =>
            {
                if (EditorApplication.isPlaying) soundManager.PlayPlayerDeath();
                else PlayPreviewClip(playerDeathClipProp.objectReferenceValue as AudioClip);
            });
        });

        DrawCategorySection("2. เสียงไม้ตีและสกิล (Bat & Skills)", ref showBat, new Color(1f, 0.75f, 0.2f), () =>
        {
            DrawSoundRow("Swing (เหวี่ยงไม้ตีลม)", batSwingClipProp, batSwingVolumeProp, () =>
            {
                if (EditorApplication.isPlaying) soundManager.PlaySwing();
                else PlayPreviewClip(batSwingClipProp.objectReferenceValue as AudioClip);
            });
            DrawSoundRow("Hit Ball (หวดโดนบอล)", batHitBallClipProp, batHitBallVolumeProp, () =>
            {
                if (EditorApplication.isPlaying) soundManager.PlayHitBall();
                else PlayPreviewClip(batHitBallClipProp.objectReferenceValue as AudioClip);
            });
            DrawSoundRow("Magnet Skill (ดูดบอล)", magnetSkillClipProp, magnetSkillVolumeProp, () =>
            {
                if (EditorApplication.isPlaying) soundManager.PlayMagnetSkill();
                else PlayPreviewClip(magnetSkillClipProp.objectReferenceValue as AudioClip);
            });
        });

        DrawCategorySection("3. เสียงลูกบอลพินบอล (Bouncing Ball)", ref showBall, new Color(0.3f, 0.95f, 0.6f), () =>
        {
            DrawSoundRow("Bounce (เด้งกระทบ)", ballBounceClipProp, ballBounceVolumeProp, () =>
            {
                if (EditorApplication.isPlaying) soundManager.PlayBounce();
                else PlayPreviewClip(ballBounceClipProp.objectReferenceValue as AudioClip);
            });
            DrawSoundRow("High-Speed Boost (พุ่งไว)", ballHighSpeedBoostClipProp, ballHighSpeedBoostVolumeProp, () =>
            {
                if (EditorApplication.isPlaying) soundManager.PlayHighSpeedBoost();
                else PlayPreviewClip(ballHighSpeedBoostClipProp.objectReferenceValue as AudioClip);
            });
            DrawSoundRow("Ice Effect (เกล็ดน้ำแข็ง)", ballIceEffectClipProp, ballIceEffectVolumeProp, () =>
            {
                if (EditorApplication.isPlaying) soundManager.PlayIceEffect();
                else PlayPreviewClip(ballIceEffectClipProp.objectReferenceValue as AudioClip);
            });
            DrawSoundRow("Explode (ระเบิดหุ่นยนต์)", ballExplodeEffectClipProp, ballExplodeVolumeProp, () =>
            {
                if (EditorApplication.isPlaying) soundManager.PlayBallExplode();
                else PlayPreviewClip(ballExplodeEffectClipProp.objectReferenceValue as AudioClip);
            });
        });

        DrawCategorySection("4. เสียงฝั่งศัตรู (Enemy Drones)", ref showEnemy, new Color(1f, 0.35f, 0.35f), () =>
        {
            DrawSoundRow("Spawn (เกิด/เปิดเครื่อง)", enemySpawnClipProp, enemySpawnVolumeProp, () =>
            {
                if (EditorApplication.isPlaying) soundManager.PlayEnemySpawn();
                else PlayPreviewClip(enemySpawnClipProp.objectReferenceValue as AudioClip);
            });
            DrawSoundRow("Movement (มอเตอร์เซอร์โว)", enemyMovementClipProp, enemyMovementVolumeProp, () =>
            {
                if (EditorApplication.isPlaying) soundManager.PlayEnemyMovement();
                else PlayPreviewClip(enemyMovementClipProp.objectReferenceValue as AudioClip);
            });
            DrawSoundRow("Telegraph Warning (สัญญาณเตือนภัย)", enemyTelegraphWarningClipProp, enemyTelegraphVolumeProp, () =>
            {
                if (EditorApplication.isPlaying) soundManager.PlayTelegraphWarning();
                else PlayPreviewClip(enemyTelegraphWarningClipProp.objectReferenceValue as AudioClip);
            });
            DrawSoundRow("Enemy Hurt (เจ็บ/เกราะยุบ)", enemyHurtClipProp, enemyHurtVolumeProp, () =>
            {
                if (EditorApplication.isPlaying) soundManager.PlayEnemyHurt();
                else PlayPreviewClip(enemyHurtClipProp.objectReferenceValue as AudioClip);
            });
            DrawSoundRow("Enemy Die (ระเบิดแตกกระจาย)", enemyDieExplosionClipProp, enemyDieExplosionVolumeProp, () =>
            {
                if (EditorApplication.isPlaying) soundManager.PlayEnemyDie();
                else PlayPreviewClip(enemyDieExplosionClipProp.objectReferenceValue as AudioClip);
            });
        });

        DrawCategorySection("5. เสียงสิ่งกีดขวางและ UI (Environment & UI)", ref showUI, new Color(0.85f, 0.45f, 1f), () =>
        {
            DrawSoundRow("Bumper Hit (เสาพินบอล)", bumperHitClipProp, bumperHitVolumeProp, () =>
            {
                if (EditorApplication.isPlaying) soundManager.PlayBumperHit();
                else PlayPreviewClip(bumperHitClipProp.objectReferenceValue as AudioClip);
            });
            DrawSoundRow("Button Click (คลิกปุ่ม)", buttonClickClipProp, buttonClickVolumeProp, () =>
            {
                if (EditorApplication.isPlaying) soundManager.PlayButtonClick();
                else PlayPreviewClip(buttonClickClipProp.objectReferenceValue as AudioClip);
            });
        });

        if (serializedObject.ApplyModifiedProperties())
        {
            soundManager.UpdateHoverEngineVolume();
        }
    }

    private void DrawHeaderPanel()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("🎵 SOUND MANAGER CONTROLLER", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("ปรับแต่งระดับเสียง และใส่ไฟล์เสียงเองได้ที่นี่ พร้อมระบบทดสอบเสียงแบบเรียลไทม์", EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(4);
    }

    private void DrawMasterVolumeSliders()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("🎚️ ระดับเสียงรวมของเกม (Master & Category Volumes)", EditorStyles.boldLabel);
        EditorGUILayout.Space(2);

        DrawVolumeSlider(masterVolumeProp, "Master Volume (รวมทั้งหมด)", new Color(0.2f, 0.9f, 0.4f));
        DrawVolumeSlider(sfxVolumeProp, "SFX Volume (เสียงเอฟเฟกต์)", new Color(0.3f, 0.8f, 1f));
        DrawVolumeSlider(playerVolumeProp, "Player Volume (ฝั่งผู้เล่น)", new Color(0.4f, 0.7f, 1f));
        DrawVolumeSlider(enemyVolumeProp, "Enemy Volume (ฝั่งศัตรู)", new Color(1f, 0.4f, 0.4f));
        DrawVolumeSlider(uiVolumeProp, "UI Volume (เมนูและปุ่ม)", new Color(0.9f, 0.5f, 1f));

        EditorGUILayout.Space(4);
        EditorGUILayout.PropertyField(usePlayerPrefsProp, new GUIContent("จำค่าเสียงด้วย PlayerPrefs"));

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🔇 Mute All (ปิดเสียง)", GUILayout.Height(22)))
        {
            masterVolumeProp.floatValue = 0f;
        }
        if (GUILayout.Button("🔊 Max All (100%)", GUILayout.Height(22)))
        {
            masterVolumeProp.floatValue = 1f;
            sfxVolumeProp.floatValue = 1f;
            playerVolumeProp.floatValue = 1f;
            enemyVolumeProp.floatValue = 1f;
            uiVolumeProp.floatValue = 1f;
        }
        if (GUILayout.Button("🔄 คืนค่าเริ่มต้น (Defaults)", GUILayout.Height(22)))
        {
            masterVolumeProp.floatValue = 1f;
            sfxVolumeProp.floatValue = 1f;
            playerVolumeProp.floatValue = 1f;
            enemyVolumeProp.floatValue = 1f;
            uiVolumeProp.floatValue = 1f;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawVolumeSlider(SerializedProperty prop, string label, Color color)
    {
        EditorGUILayout.BeginHorizontal();
        Color oldColor = GUI.contentColor;
        GUI.contentColor = color;
        EditorGUILayout.LabelField(label, GUILayout.Width(170));
        GUI.contentColor = oldColor;

        prop.floatValue = EditorGUILayout.Slider(prop.floatValue, 0f, 1f);
        EditorGUILayout.LabelField($"{Mathf.RoundToInt(prop.floatValue * 100f)}%", GUILayout.Width(42));
        EditorGUILayout.EndHorizontal();
    }

    private void DrawCategorySection(string title, ref bool foldout, Color headerColor, System.Action content)
    {
        GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldoutHeader);
        foldoutStyle.fontStyle = FontStyle.Bold;

        Color oldColor = GUI.contentColor;
        GUI.contentColor = headerColor;
        foldout = EditorGUILayout.BeginFoldoutHeaderGroup(foldout, title, foldoutStyle);
        GUI.contentColor = oldColor;

        if (foldout)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            content.Invoke();
            EditorGUILayout.EndVertical();
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space(4);
    }

    private void DrawSoundRow(string label, SerializedProperty clipProp, SerializedProperty volumeProp, System.Action testAction)
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.BeginHorizontal();

        // 1. Label
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel, GUILayout.Width(180));

        // 2. Audio Clip Object Field
        EditorGUILayout.PropertyField(clipProp, GUIContent.none);

        // 3. Test Play Button
        if (GUILayout.Button("▶️ Test", GUILayout.Width(60), GUILayout.Height(18)))
        {
            testAction.Invoke();
        }

        EditorGUILayout.EndHorizontal();

        // 4. Volume Slider (0 - 2x)
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("   ความดัง (Volume)", GUILayout.Width(150));
        volumeProp.floatValue = EditorGUILayout.Slider(volumeProp.floatValue, 0f, 2f);
        EditorGUILayout.LabelField($"{Mathf.RoundToInt(volumeProp.floatValue * 100f)}%", GUILayout.Width(42));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private static void PlayPreviewClip(AudioClip clip)
    {
        if (clip == null) return;

        System.Type audioUtil = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
        if (audioUtil != null)
        {
            MethodInfo method = audioUtil.GetMethod("PlayPreviewClip", BindingFlags.Static | BindingFlags.Public, null, new System.Type[] { typeof(AudioClip), typeof(int), typeof(bool) }, null)
                             ?? audioUtil.GetMethod("PlayClip", BindingFlags.Static | BindingFlags.Public, null, new System.Type[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
            if (method != null)
            {
                method.Invoke(null, new object[] { clip, 0, false });
            }
        }
    }
}
