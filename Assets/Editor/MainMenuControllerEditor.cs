using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Custom Inspector for MainMenuController that provides a convenient Dropdown
/// and SceneAsset picker to choose which scene the Play button loads.
/// </summary>
[CustomEditor(typeof(MainMenuController))]
public class MainMenuControllerEditor : Editor
{
    private SerializedProperty targetSceneAssetProp;
    private SerializedProperty gameSceneNameProp;
    private SerializedProperty mainMenuCanvasProp;
    private SerializedProperty playButtonProp;
    private SerializedProperty howToPlayButtonProp;
    private SerializedProperty quitButtonProp;
    private SerializedProperty howToPlayPanelProp;
    private SerializedProperty closeHowToPlayButtonProp;

    private List<string> sceneNames = new List<string>();
    private List<string> scenePaths = new List<string>();

    private void OnEnable()
    {
        targetSceneAssetProp = serializedObject.FindProperty("targetSceneAsset");
        gameSceneNameProp = serializedObject.FindProperty("gameSceneName");
        mainMenuCanvasProp = serializedObject.FindProperty("mainMenuCanvas");
        playButtonProp = serializedObject.FindProperty("playButton");
        howToPlayButtonProp = serializedObject.FindProperty("howToPlayButton");
        quitButtonProp = serializedObject.FindProperty("quitButton");
        howToPlayPanelProp = serializedObject.FindProperty("howToPlayPanel");
        closeHowToPlayButtonProp = serializedObject.FindProperty("closeHowToPlayButton");

        RefreshSceneList();
    }

    private void RefreshSceneList()
    {
        sceneNames.Clear();
        scenePaths.Clear();

        string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string name = Path.GetFileNameWithoutExtension(path);
            sceneNames.Add(name);
            scenePaths.Add(path);
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        MainMenuController controller = (MainMenuController)target;

        // 1. Scene Selection Box
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("🎮 Play Button Target Scene", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // Dropdown Selection
        if (sceneNames.Count > 0)
        {
            int currentIndex = sceneNames.IndexOf(gameSceneNameProp.stringValue);
            if (currentIndex == -1) currentIndex = 0;

            int selectedIndex = EditorGUILayout.Popup("Choose Scene (Dropdown)", currentIndex, sceneNames.ToArray());
            if (selectedIndex != currentIndex || string.IsNullOrEmpty(gameSceneNameProp.stringValue))
            {
                gameSceneNameProp.stringValue = sceneNames[selectedIndex];
                targetSceneAssetProp.objectReferenceValue = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePaths[selectedIndex]);
            }
        }

        // Direct SceneAsset Drag & Drop
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(targetSceneAssetProp, new GUIContent("Scene Asset (Drag & Drop)"));
        if (EditorGUI.EndChangeCheck())
        {
            if (targetSceneAssetProp.objectReferenceValue != null)
            {
                gameSceneNameProp.stringValue = targetSceneAssetProp.objectReferenceValue.name;
            }
        }

        // Current Scene Name Display
        EditorGUILayout.LabelField("Target Scene Name:", gameSceneNameProp.stringValue, EditorStyles.boldLabel);

        // Build Settings Check
        CheckBuildSettingsStatus(gameSceneNameProp.stringValue);

        EditorGUILayout.EndVertical();

        // 2. UI References in Hierarchy
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("🎨 UI References in Hierarchy", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("องค์ประกอบ UI เหล่านี้สามารถคลิกเลือกและแก้ไขได้โดยตรงในแท็บ Hierarchy", MessageType.Info);

        EditorGUILayout.PropertyField(mainMenuCanvasProp);
        EditorGUILayout.PropertyField(playButtonProp);
        EditorGUILayout.PropertyField(howToPlayButtonProp);
        EditorGUILayout.PropertyField(quitButtonProp);
        EditorGUILayout.PropertyField(howToPlayPanelProp);
        EditorGUILayout.PropertyField(closeHowToPlayButtonProp);

        serializedObject.ApplyModifiedProperties();
    }

    private void CheckBuildSettingsStatus(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;

        bool isInBuild = false;
        int buildIndex = -1;

        for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
        {
            var s = EditorBuildSettings.scenes[i];
            if (s.enabled && Path.GetFileNameWithoutExtension(s.path) == sceneName)
            {
                isInBuild = true;
                buildIndex = i;
                break;
            }
        }

        if (isInBuild)
        {
            EditorGUILayout.LabelField($"✓ Scene อยู่ใน Build Settings แล้ว (Index: {buildIndex})", EditorStyles.miniLabel);
        }
        else
        {
            EditorGUILayout.HelpBox($"⚠ Scene '{sceneName}' ยังไม่ได้อยู่ใน Build Settings!", MessageType.Warning);
            if (GUILayout.Button("➕ Add Scene to Build Settings"))
            {
                AddSceneToBuildSettings(sceneName);
            }
        }
    }

    private void AddSceneToBuildSettings(string sceneName)
    {
        string[] guids = AssetDatabase.FindAssets("t:Scene " + sceneName);
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == sceneName)
            {
                List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
                scenes.Add(new EditorBuildSettingsScene(path, true));
                EditorBuildSettings.scenes = scenes.ToArray();
                Debug.Log($"Added {path} to Build Settings.");
                break;
            }
        }
    }
}
