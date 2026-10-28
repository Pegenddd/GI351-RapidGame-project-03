using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Permanently generates all UI GameObjects directly into the Hierarchy for all scenes
/// (including Tutorial.unity, SampleScene.unity, and MainMenu.unity).
/// This allows the developer to freely select, inspect, modify, and style all UI components in the Scene View and Hierarchy.
/// </summary>
[InitializeOnLoad]
public static class HierarchyUISetupTool
{
    public const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    public const string TutorialScenePath = "Assets/Scenes/Tutorial.unity";
    public const string GameScenePath = "Assets/Scenes/SampleScene.unity";

    static HierarchyUISetupTool()
    {
        EditorApplication.delayCall += EnsureHierarchyUIExists;
    }

    [MenuItem("Game/Setup UI for Current Scene (Make Editable in Hierarchy)")]
    public static void ManualGenerateUI()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        UpdateBuildSettings();

        Scene activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.name.ToLower().Contains("menu") || Object.FindAnyObjectByType<MainMenuController>() != null)
        {
            SetupActiveMainMenuSceneUI();
        }
        else
        {
            SetupActiveGameSceneUI();
        }

        if (!EditorApplication.isPlaying && activeScene.isLoaded)
        {
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            AssetDatabase.SaveAssets();
        }
        Debug.Log($"🎉 [HierarchyUISetupTool] UI in {activeScene.name} Hierarchy is ready and fully editable!");
    }

    [MenuItem("Game/Setup UI for Tutorial Scene (Make Editable in Hierarchy)")]
    public static void SetupTutorialSceneMenu()
    {
        SetupSpecificScene(TutorialScenePath);
    }

    [MenuItem("Game/Setup UI for SampleScene (Make Editable in Hierarchy)")]
    public static void SetupSampleSceneMenu()
    {
        SetupSpecificScene(GameScenePath);
    }

    [MenuItem("Game/Setup UI for MainMenu Scene (Make Editable in Hierarchy)")]
    public static void SetupMainMenuSceneMenu()
    {
        SetupSpecificScene(MainMenuScenePath);
    }

    public static void SetupSpecificScene(string scenePath)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        Scene activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.path == scenePath)
        {
            ManualGenerateUI();
            return;
        }

        if (activeScene.isDirty)
        {
            EditorSceneManager.SaveScene(activeScene);
        }

        Scene openedScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        if (openedScene.isLoaded)
        {
            ManualGenerateUI();
        }
    }

    private static void EnsureHierarchyUIExists()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer) return;

        UpdateBuildSettings();

        Scene activeScene = EditorSceneManager.GetActiveScene();
        if (!activeScene.isLoaded) return;

        if (activeScene.name.ToLower().Contains("menu") || Object.FindAnyObjectByType<MainMenuController>() != null)
        {
            // ถ้ายังไม่มี MainMenuCanvas ใน Hierarchy ให้สร้าง
            if (GameObject.Find("MainMenuCanvas") == null)
            {
                SetupActiveMainMenuSceneUI();
                SaveActiveSceneSafely(activeScene);
            }
        }
        else
        {
            // สำหรับฉากเกมทั้งหมด รวมถึง Tutorial.unity และ SampleScene.unity
            if (GameObject.Find("HUDCanvas") == null)
            {
                SetupActiveGameSceneUI();
                SaveActiveSceneSafely(activeScene);
            }
        }
    }

    private static void SaveActiveSceneSafely(Scene scene)
    {
        if (!EditorApplication.isPlaying && scene.isLoaded)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }
    }

    public static void UpdateBuildSettings()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>();

        if (File.Exists(MainMenuScenePath))
        {
            buildScenes.Add(new EditorBuildSettingsScene(MainMenuScenePath, true));
        }

        if (File.Exists(TutorialScenePath))
        {
            buildScenes.Add(new EditorBuildSettingsScene(TutorialScenePath, true));
        }

        if (File.Exists(GameScenePath))
        {
            buildScenes.Add(new EditorBuildSettingsScene(GameScenePath, true));
        }

        string[] allSceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });
        foreach (string guid in allSceneGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path == MainMenuScenePath || path == TutorialScenePath || path == GameScenePath) continue;

            buildScenes.Add(new EditorBuildSettingsScene(path, true));
        }

        if (buildScenes.Count > 0)
        {
            EditorBuildSettings.scenes = buildScenes.ToArray();
        }
    }

    public static void SetupGameSceneUI() => SetupActiveGameSceneUI();
    public static void SetupMainMenuSceneUI() => SetupActiveMainMenuSceneUI();

    public static void SetupActiveMainMenuSceneUI()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        Scene activeScene = EditorSceneManager.GetActiveScene();
        if (!activeScene.isLoaded) return;

        EnsureEventSystemInScene();

        MainMenuController menuController = Object.FindAnyObjectByType<MainMenuController>();
        if (menuController == null)
        {
            GameObject menuControllerObj = new GameObject("MainMenuController");
            menuController = menuControllerObj.AddComponent<MainMenuController>();
        }

        if (Object.FindAnyObjectByType<SoundManager>() == null)
        {
            GameObject sfxObj = new GameObject("SoundEffectsManager");
            sfxObj.AddComponent<SoundManager>();
            sfxObj.AddComponent<SoundEffects>();
        }

        GameObject canvasObj = GameObject.Find("MainMenuCanvas");
        if (canvasObj == null)
        {
            canvasObj = new GameObject("MainMenuCanvas", typeof(RectTransform));
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            // 1. Background
            GameObject bgObj = CreateUIObject("Background", canvasObj.transform);
            Image bgImg = bgObj.GetComponent<Image>() ?? bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.04f, 0.06f, 0.1f, 1f);
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            // 2. Title Panel
            GameObject titlePanel = CreateUIObject("TitlePanel", canvasObj.transform);
            RectTransform titleRect = titlePanel.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.72f);
            titleRect.anchorMax = new Vector2(0.5f, 0.72f);
            titleRect.sizeDelta = new Vector2(900, 200);

            GameObject titleTextObj = CreateUIObject("TitleText", titlePanel.transform);
            Text titleText = titleTextObj.GetComponent<Text>() ?? titleTextObj.AddComponent<Text>();
            titleText.font = defaultFont;
            titleText.fontSize = 62;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new Color(0.2f, 0.85f, 1f);
            titleText.text = "⚡ RAPID PINBALL ARENA ⚡";
            RectTransform titleTextRect = titleTextObj.GetComponent<RectTransform>();
            titleTextRect.anchorMin = Vector2.zero;
            titleTextRect.anchorMax = Vector2.one;
            titleTextRect.sizeDelta = Vector2.zero;

            GameObject subTextObj = CreateUIObject("SubtitleText", titlePanel.transform);
            Text subText = subTextObj.GetComponent<Text>() ?? subTextObj.AddComponent<Text>();
            subText.font = defaultFont;
            subText.fontSize = 24;
            subText.alignment = TextAnchor.LowerCenter;
            subText.color = new Color(0.85f, 0.9f, 0.95f, 0.9f);
            subText.text = "Smash the ball, deflect attacks, and survive the arena!";
            RectTransform subTextRect = subTextObj.GetComponent<RectTransform>();
            subTextRect.anchorMin = Vector2.zero;
            subTextRect.anchorMax = Vector2.one;
            subTextRect.anchoredPosition = new Vector2(0, -25);

            // 3. Button Group
            GameObject btnGroup = CreateUIObject("ButtonGroup", canvasObj.transform);
            RectTransform btnGroupRect = btnGroup.GetComponent<RectTransform>();
            btnGroupRect.anchorMin = new Vector2(0.5f, 0.38f);
            btnGroupRect.anchorMax = new Vector2(0.5f, 0.38f);
            btnGroupRect.sizeDelta = new Vector2(380, 280);

            Button playBtn = CreateButton(btnGroup.transform, "PlayButton", "🎮 เริ่มเกม (Play)", new Vector2(0, 75), new Color(0.15f, 0.65f, 0.35f), defaultFont);
            Button howToPlayBtn = CreateButton(btnGroup.transform, "HowToPlayButton", "📖 วิธีการเล่น (How to Play)", new Vector2(0, 0), new Color(0.2f, 0.45f, 0.8f), defaultFont);
            Button quitBtn = CreateButton(btnGroup.transform, "QuitButton", "🚪 ออกจากเกม (Quit)", new Vector2(0, -75), new Color(0.75f, 0.22f, 0.22f), defaultFont);

            // 4. How To Play Modal Popup
            GameObject howToPlayPanel = CreateUIObject("HowToPlayPanel", canvasObj.transform);
            RectTransform panelRect = howToPlayPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;

            Image overlayBg = howToPlayPanel.GetComponent<Image>() ?? howToPlayPanel.AddComponent<Image>();
            overlayBg.color = new Color(0f, 0f, 0f, 0.82f);

            GameObject box = CreateUIObject("ContentBox", howToPlayPanel.transform);
            Image boxImg = box.GetComponent<Image>() ?? box.AddComponent<Image>();
            boxImg.color = new Color(0.08f, 0.12f, 0.18f, 0.98f);
            RectTransform boxRect = box.GetComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.5f, 0.5f);
            boxRect.anchorMax = new Vector2(0.5f, 0.5f);
            boxRect.sizeDelta = new Vector2(720, 500);

            GameObject headerObj = CreateUIObject("Header", box.transform);
            Text headerText = headerObj.GetComponent<Text>() ?? headerObj.AddComponent<Text>();
            headerText.font = defaultFont;
            headerText.fontSize = 32;
            headerText.fontStyle = FontStyle.Bold;
            headerText.alignment = TextAnchor.MiddleCenter;
            headerText.color = new Color(1f, 0.8f, 0.2f);
            headerText.text = "📖 วิธีการเล่น (How to Play)";
            RectTransform headerRect = headerObj.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.anchoredPosition = new Vector2(0, -25);
            headerRect.sizeDelta = new Vector2(0, 50);

            GameObject detailsObj = CreateUIObject("Details", box.transform);
            Text detailsText = detailsObj.GetComponent<Text>() ?? detailsObj.AddComponent<Text>();
            detailsText.font = defaultFont;
            detailsText.fontSize = 20;
            detailsText.lineSpacing = 1.35f;
            detailsText.alignment = TextAnchor.UpperLeft;
            detailsText.color = new Color(0.9f, 0.95f, 1f);
            detailsText.text = "🕹️  [W, A, S, D] หรือ [ลูกศร] : เคลื่อนที่ตัวละคร (Move)\n" +
                               "🏏  [คลิกซ้าย] หรือ [Spacebar] : หวดลูกบอลเพื่อเร่งความเร็วและชิ่งศัตรู\n" +
                               "💨  [Shift ซ้าย] หรือ [คลิกขวา] : พุ่งแดชหลบการโจมตี (Dash)\n" +
                               "🧲  [กด E] : สกิลแม่เหล็กดึงลูกบอล + เล็งวิถีชิ่งสะท้อนล่วงหน้า\n" +
                               "🔄  [R] : รีสตาร์ทเกมอย่างรวดเร็ว (Quick Restart)\n\n" +
                               "💡  เป้าหมาย: ตีลูกบอลให้ชิ่งโดนศัตรูเพื่อทำดาเมจ ระวังศัตรูวิ่งเข้าประชิด!\n" +
                               "     รักษาหลอดเลือดของคุณให้นานที่สุดเพื่อทำสถิติคลิกคะแนนสูงสุด!";
            RectTransform detailsRect = detailsObj.GetComponent<RectTransform>();
            detailsRect.anchorMin = new Vector2(0f, 0f);
            detailsRect.anchorMax = new Vector2(1f, 1f);
            detailsRect.offsetMin = new Vector2(40, 90);
            detailsRect.offsetMax = new Vector2(-40, -80);

            Button closeBtn = CreateButton(box.transform, "CloseButton", "✖ ปิดหน้าต่าง (Close)", new Vector2(0, 45), new Color(0.85f, 0.25f, 0.25f), defaultFont);
            RectTransform closeBtnRect = closeBtn.GetComponent<RectTransform>();
            closeBtnRect.anchorMin = new Vector2(0.5f, 0f);
            closeBtnRect.anchorMax = new Vector2(0.5f, 0f);
            closeBtnRect.sizeDelta = new Vector2(260, 48);

            howToPlayPanel.SetActive(false);

            menuController.gameSceneName = "Tutorial";
            menuController.mainMenuCanvas = canvasObj.GetComponent<Canvas>();
            menuController.playButton = playBtn;
            menuController.howToPlayButton = howToPlayBtn;
            menuController.quitButton = quitBtn;
            menuController.howToPlayPanel = howToPlayPanel;
            menuController.closeHowToPlayButton = closeBtn;

            EditorUtility.SetDirty(menuController);
        }

        if (!EditorApplication.isPlaying && activeScene.isLoaded)
        {
            EditorSceneManager.MarkSceneDirty(activeScene);
        }
    }

    public static void SetupActiveGameSceneUI()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        Scene activeScene = EditorSceneManager.GetActiveScene();
        if (!activeScene.isLoaded) return;

        EnsureEventSystemInScene();

        // 0. Ensure GameManager & SoundEffectsManager exist in Scene
        GameManager gm = Object.FindAnyObjectByType<GameManager>();
        if (gm == null)
        {
            GameObject gmObj = new GameObject("GameManager");
            gm = gmObj.AddComponent<GameManager>();
            gm.mainMenuSceneName = "MainMenu";
        }

        if (Object.FindAnyObjectByType<SoundManager>() == null && Object.FindAnyObjectByType<SoundEffects>() == null)
        {
            GameObject sfxObj = new GameObject("SoundEffectsManager");
            sfxObj.AddComponent<SoundManager>();
            sfxObj.AddComponent<SoundEffects>();
        }

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

        // 1. Setup HUDCanvas in Hierarchy
        GameObject canvasObj = GameObject.Find("HUDCanvas");
        if (canvasObj == null)
        {
            canvasObj = new GameObject("HUDCanvas", typeof(RectTransform));
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Top Status Panel (Score)
        GameObject topPanelObj = CreateUIObject("TopPanel", canvasObj.transform);
        Image topBg = topPanelObj.GetComponent<Image>() ?? topPanelObj.AddComponent<Image>();
        topBg.color = new Color(0.08f, 0.1f, 0.15f, 0.85f);
        RectTransform topRect = topPanelObj.GetComponent<RectTransform>();
        topRect.anchorMin = new Vector2(0.5f, 1f);
        topRect.anchorMax = new Vector2(0.5f, 1f);
        topRect.pivot = new Vector2(0.5f, 1f);
        topRect.anchoredPosition = new Vector2(0, -15);
        topRect.sizeDelta = new Vector2(480, 85);

        GameObject scoreTextObj = CreateUIObject("ScoreText", topPanelObj.transform);
        Text scoreText = scoreTextObj.GetComponent<Text>() ?? scoreTextObj.AddComponent<Text>();
        scoreText.font = defaultFont;
        scoreText.fontSize = 20;
        scoreText.alignment = TextAnchor.MiddleCenter;
        scoreText.color = Color.white;
        scoreText.text = "🎯 ศัตรูที่กำจัด (Defeated): 0\n⭐ คะแนน (Score): 0";
        RectTransform scoreRect = scoreTextObj.GetComponent<RectTransform>();
        scoreRect.anchorMin = Vector2.zero;
        scoreRect.anchorMax = Vector2.one;
        scoreRect.sizeDelta = Vector2.zero;

        // Ball Speed Text
        GameObject speedObj = CreateUIObject("SpeedText", canvasObj.transform);
        Text speedText = speedObj.GetComponent<Text>() ?? speedObj.AddComponent<Text>();
        speedText.font = defaultFont;
        speedText.fontSize = 22;
        speedText.fontStyle = FontStyle.Bold;
        speedText.alignment = TextAnchor.UpperRight;
        speedText.color = new Color(0.3f, 0.9f, 1f);
        speedText.text = "⚡ Ball Speed: 16.0 m/s";
        RectTransform speedRect = speedObj.GetComponent<RectTransform>();
        speedRect.anchorMin = new Vector2(1f, 1f);
        speedRect.anchorMax = new Vector2(1f, 1f);
        speedRect.pivot = new Vector2(1f, 1f);
        speedRect.anchoredPosition = new Vector2(-25, -20);
        speedRect.sizeDelta = new Vector2(300, 40);

        // Controls Panel
        GameObject controlsPanel = CreateUIObject("ControlsPanel", canvasObj.transform);
        Image controlsBg = controlsPanel.GetComponent<Image>() ?? controlsPanel.AddComponent<Image>();
        controlsBg.color = new Color(0.05f, 0.08f, 0.12f, 0.85f);
        RectTransform ctrlRect = controlsPanel.GetComponent<RectTransform>();
        ctrlRect.anchorMin = new Vector2(0f, 0f);
        ctrlRect.anchorMax = new Vector2(0f, 0f);
        ctrlRect.pivot = new Vector2(0f, 0f);
        ctrlRect.anchoredPosition = new Vector2(20, 20);
        ctrlRect.sizeDelta = new Vector2(420, 155);

        GameObject ctrlTextObj = CreateUIObject("ControlsText", controlsPanel.transform);
        Text ctrlText = ctrlTextObj.GetComponent<Text>() ?? ctrlTextObj.AddComponent<Text>();
        ctrlText.font = defaultFont;
        ctrlText.fontSize = 15;
        ctrlText.alignment = TextAnchor.MiddleLeft;
        ctrlText.color = new Color(0.9f, 0.95f, 1f);
        ctrlText.text = "🎮 [WASD] / ลูกศร: เดิน (Move)\n" +
                        "🏏 [คลิกซ้าย / Space]: หวดลูกบอล (Hit Ball)\n" +
                        "💨 [Shift ซ้าย / คลิกขวา]: แดชพุ่งหลบ (Dash)\n" +
                        "🧲 [กด E]: สกิลดูดบอล + โหมดเล็งวิถีชิ่ง\n" +
                        "🔄 [R]: รีสตาร์ทเกม (Restart)";
        RectTransform ctrlTextRect = ctrlTextObj.GetComponent<RectTransform>();
        ctrlTextRect.anchorMin = Vector2.zero;
        ctrlTextRect.anchorMax = Vector2.one;
        ctrlTextRect.offsetMin = new Vector2(15, 10);
        ctrlTextRect.offsetMax = new Vector2(-15, -10);

        // Player Health Bar Panel
        GameObject hpPanel = CreateUIObject("PlayerHPPanel", canvasObj.transform);
        Image panelBg = hpPanel.GetComponent<Image>() ?? hpPanel.AddComponent<Image>();
        panelBg.color = new Color(0.06f, 0.08f, 0.12f, 0.9f);
        RectTransform panelRect = hpPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(30, -30);
        panelRect.sizeDelta = new Vector2(320, 68);

        GameObject hpLabelObj = CreateUIObject("HPLabel", hpPanel.transform);
        Text hpLabel = hpLabelObj.GetComponent<Text>() ?? hpLabelObj.AddComponent<Text>();
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

        GameObject valObj = CreateUIObject("HPValueText", hpPanel.transform);
        Text hpValText = valObj.GetComponent<Text>() ?? valObj.AddComponent<Text>();
        hpValText.font = defaultFont;
        hpValText.fontSize = 16;
        hpValText.fontStyle = FontStyle.Bold;
        hpValText.alignment = TextAnchor.MiddleRight;
        hpValText.color = new Color(0.3f, 0.95f, 0.5f);
        hpValText.text = "100 / 100";
        RectTransform valRect = valObj.GetComponent<RectTransform>();
        valRect.anchorMin = new Vector2(1f, 1f);
        valRect.anchorMax = new Vector2(1f, 1f);
        valRect.pivot = new Vector2(1f, 1f);
        valRect.anchoredPosition = new Vector2(-15, -8);
        valRect.sizeDelta = new Vector2(100, 24);

        GameObject sliderObj = CreateUIObject("PlayerHPSlider", hpPanel.transform);
        Slider hpSlider = sliderObj.GetComponent<Slider>() ?? sliderObj.AddComponent<Slider>();
        hpSlider.interactable = false;
        RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0f, 0f);
        sliderRect.anchorMax = new Vector2(1f, 0f);
        sliderRect.pivot = new Vector2(0.5f, 0f);
        sliderRect.anchoredPosition = new Vector2(0, 12);
        sliderRect.sizeDelta = new Vector2(-30, 20);

        GameObject sliderBgObj = CreateUIObject("Background", sliderObj.transform);
        Image sliderBgImg = sliderBgObj.GetComponent<Image>() ?? sliderBgObj.AddComponent<Image>();
        sliderBgImg.color = new Color(0.2f, 0.05f, 0.05f, 0.95f);
        RectTransform sliderBgRect = sliderBgObj.GetComponent<RectTransform>();
        sliderBgRect.anchorMin = Vector2.zero;
        sliderBgRect.anchorMax = Vector2.one;
        sliderBgRect.sizeDelta = Vector2.zero;

        GameObject fillArea = CreateUIObject("Fill Area", sliderObj.transform);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = Vector2.zero;

        GameObject fillObj = CreateUIObject("Fill", fillArea.transform);
        Image fillImg = fillObj.GetComponent<Image>() ?? fillObj.AddComponent<Image>();
        fillImg.color = new Color(0.25f, 0.85f, 0.4f);
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;

        hpSlider.fillRect = fillRect;
        hpSlider.minValue = 0f;
        hpSlider.maxValue = 1f;
        hpSlider.value = 1f;

        // 2. Setup Game Over Panel in Hierarchy (Default Inactive)
        GameObject gameOverPanel = CreateUIObject("GameOverPanel", canvasObj.transform);
        RectTransform goPanelRect = gameOverPanel.GetComponent<RectTransform>();
        goPanelRect.anchorMin = Vector2.zero;
        goPanelRect.anchorMax = Vector2.one;
        goPanelRect.sizeDelta = Vector2.zero;

        Image goBg = gameOverPanel.GetComponent<Image>() ?? gameOverPanel.AddComponent<Image>();
        goBg.color = new Color(0.04f, 0.02f, 0.02f, 0.88f);

        GameObject cardObj = CreateUIObject("CardBox", gameOverPanel.transform);
        Image cardBg = cardObj.GetComponent<Image>() ?? cardObj.AddComponent<Image>();
        cardBg.color = new Color(0.1f, 0.12f, 0.16f, 0.98f);
        RectTransform cardRect = cardObj.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(560, 480);

        GameObject goTitleObj = CreateUIObject("Title", cardObj.transform);
        Text goTitleText = goTitleObj.GetComponent<Text>() ?? goTitleObj.AddComponent<Text>();
        goTitleText.font = defaultFont;
        goTitleText.fontSize = 50;
        goTitleText.fontStyle = FontStyle.Bold;
        goTitleText.alignment = TextAnchor.MiddleCenter;
        goTitleText.color = new Color(1f, 0.28f, 0.28f);
        goTitleText.text = "💀 GAME OVER";
        RectTransform goTitleRect = goTitleObj.GetComponent<RectTransform>();
        goTitleRect.anchorMin = new Vector2(0f, 1f);
        goTitleRect.anchorMax = new Vector2(1f, 1f);
        goTitleRect.pivot = new Vector2(0.5f, 1f);
        goTitleRect.anchoredPosition = new Vector2(0, -30);
        goTitleRect.sizeDelta = new Vector2(0, 65);

        GameObject goSubObj = CreateUIObject("Subtitle", cardObj.transform);
        Text goSubText = goSubObj.GetComponent<Text>() ?? goSubObj.AddComponent<Text>();
        goSubText.font = defaultFont;
        goSubText.fontSize = 18;
        goSubText.alignment = TextAnchor.MiddleCenter;
        goSubText.color = new Color(0.8f, 0.85f, 0.9f);
        goSubText.text = "คุณพ่ายแพ้ในการต่อสู้!";
        RectTransform goSubRect = goSubObj.GetComponent<RectTransform>();
        goSubRect.anchorMin = new Vector2(0f, 1f);
        goSubRect.anchorMax = new Vector2(1f, 1f);
        goSubRect.pivot = new Vector2(0.5f, 1f);
        goSubRect.anchoredPosition = new Vector2(0, -95);
        goSubRect.sizeDelta = new Vector2(0, 30);

        GameObject statsObj = CreateUIObject("StatsText", cardObj.transform);
        Text statsText = statsObj.GetComponent<Text>() ?? statsObj.AddComponent<Text>();
        statsText.font = defaultFont;
        statsText.fontSize = 22;
        statsText.lineSpacing = 1.4f;
        statsText.alignment = TextAnchor.MiddleCenter;
        statsText.color = Color.white;
        statsText.text = "⏱️ เวลาที่รอด: 00:00\n🎯 ศัตรูที่กำจัด: 0 ตัว\n⭐ คะแนนรวม: 0 แต้ม";
        RectTransform statsRect = statsObj.GetComponent<RectTransform>();
        statsRect.anchorMin = new Vector2(0.1f, 0.42f);
        statsRect.anchorMax = new Vector2(0.9f, 0.72f);
        statsRect.sizeDelta = Vector2.zero;

        Button restartBtn = CreateButton(cardObj.transform, "RestartBtn", "🔄 เล่นอีกครั้ง [R]", new Vector2(0, -115), new Color(0.18f, 0.65f, 0.35f), defaultFont);
        Button mainMenuBtn = CreateButton(cardObj.transform, "MainMenuBtn", "🏠 เมนูหลัก [M]", new Vector2(0, -180), new Color(0.25f, 0.45f, 0.75f), defaultFont);

        // Hide GameOverPanel by default
        gameOverPanel.SetActive(false);

        // Assign to GameManager
        if (gm != null)
        {
            gm.hudCanvas = canvasObj.GetComponent<Canvas>();
            gm.scoreText = scoreText;
            gm.speedText = speedText;
            gm.controlsPanel = controlsPanel;
            gm.gameOverPanel = gameOverPanel;
            gm.gameOverStatsText = statsText;
            gm.restartButton = restartBtn;
            gm.mainMenuButton = mainMenuBtn;
            EditorUtility.SetDirty(gm);
        }

        // Assign to PlayerController
        PlayerController pc = Object.FindAnyObjectByType<PlayerController>();
        if (pc != null)
        {
            pc.playerHealthSlider = hpSlider;
            pc.playerHpText = hpValText;
            EditorUtility.SetDirty(pc);
        }

        if (!EditorApplication.isPlaying && activeScene.isLoaded)
        {
            EditorSceneManager.MarkSceneDirty(activeScene);
        }
    }

    private static void EnsureEventSystemInScene()
    {
        if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }

    /// <summary>
    /// Guaranteed creation of a GameObject with RectTransform to prevent MissingComponentException
    /// </summary>
    private static GameObject CreateUIObject(string name, Transform parent)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            if (existing.GetComponent<RectTransform>() == null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }
            else
            {
                return existing.gameObject;
            }
        }

        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 position, Color normalCol, Font font)
    {
        GameObject btnObj = CreateUIObject(name, parent);

        Image btnImg = btnObj.GetComponent<Image>() ?? btnObj.AddComponent<Image>();
        btnImg.color = normalCol;

        Button btn = btnObj.GetComponent<Button>() ?? btnObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = normalCol;
        colors.highlightedColor = normalCol * 1.25f;
        colors.pressedColor = normalCol * 0.8f;
        colors.selectedColor = normalCol * 1.1f;
        btn.colors = colors;

        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(360, (name.Contains("Close") ? 48 : 55));
        rect.anchoredPosition = position;

        GameObject textObj = CreateUIObject("Text", btnObj.transform);
        Text btnText = textObj.GetComponent<Text>() ?? textObj.AddComponent<Text>();
        btnText.font = font;
        btnText.fontSize = 22;
        btnText.fontStyle = FontStyle.Bold;
        btnText.alignment = TextAnchor.MiddleCenter;
        btnText.color = Color.white;
        btnText.text = label;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        return btn;
    }
}
