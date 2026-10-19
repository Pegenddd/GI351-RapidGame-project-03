using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// GameManager manages score, defeated enemy counter, player survival time,
/// HUD UI, and the Death / Game Over system with Main Menu navigation.
/// All UI elements can be directly modified and assigned via the Unity Hierarchy and Inspector.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    public int enemiesDefeated = 0;
    public int score = 0;
    public float survivalTime = 0f;
    public bool isGameOver { get; private set; } = false;

    [Header("Scene Navigation")]
#if UNITY_EDITOR
    [Tooltip("ลากไฟล์ Scene (.unity) ที่ต้องการให้เป็นเมนูหลักมาวางใส่ที่นี่ได้โดยตรง")]
    public SceneAsset targetMainMenuSceneAsset;
#endif
    [Tooltip("ชื่อ Scene เมนูหลัก")]
    public string mainMenuSceneName = "MainMenu";

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (targetMainMenuSceneAsset != null)
        {
            mainMenuSceneName = targetMainMenuSceneAsset.name;
        }
    }
#endif

    [Header("UI References in Hierarchy (แก้ไขได้โดยตรงใน Hierarchy)")]
    public Canvas hudCanvas;
    public Text scoreText;
    public Text speedText;
    public GameObject controlsPanel;
    public GameObject gameOverPanel;
    public Text gameOverStatsText;
    public Button restartButton;
    public Button mainMenuButton;

    private BouncingBall activeBall;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        Time.timeScale = 1f;
        EnsureEventSystem();
        BindHierarchyUI();
    }

    private void Start()
    {
        activeBall = FindAnyObjectByType<BouncingBall>();

        // ถ้าใน Hierarchy ยังไม่มี HUDCanvas เลย ถึงจะสร้าง fallback
        if (hudCanvas == null && GameObject.Find("HUDCanvas") == null)
        {
            CreateHUD();
        }
    }

    /// <summary>
    /// ค้นหาและเชื่อมโยงกับ UI GameObjects ที่อยู่ใน Hierarchy อัตโนมัติ
    /// </summary>
    public void BindHierarchyUI()
    {
        if (hudCanvas == null)
        {
            GameObject canvasObj = GameObject.Find("HUDCanvas");
            if (canvasObj != null) hudCanvas = canvasObj.GetComponent<Canvas>();
        }

        if (hudCanvas != null)
        {
            if (scoreText == null)
            {
                Transform t = hudCanvas.transform.Find("TopPanel/ScoreText") ?? hudCanvas.transform.Find("ScoreText");
                if (t != null) scoreText = t.GetComponent<Text>();
            }

            if (speedText == null)
            {
                Transform t = hudCanvas.transform.Find("SpeedText");
                if (t != null) speedText = t.GetComponent<Text>();
            }

            if (controlsPanel == null)
            {
                Transform t = hudCanvas.transform.Find("ControlsPanel");
                if (t != null) controlsPanel = t.gameObject;
            }

            if (gameOverPanel == null)
            {
                Transform t = hudCanvas.transform.Find("GameOverPanel");
                if (t != null) gameOverPanel = t.gameObject;
            }
        }

        if (gameOverPanel != null)
        {
            if (gameOverStatsText == null)
            {
                Transform t = gameOverPanel.transform.Find("CardBox/StatsText") ?? gameOverPanel.transform.Find("StatsText");
                if (t != null) gameOverStatsText = t.GetComponent<Text>();
            }

            if (restartButton == null)
            {
                Transform t = gameOverPanel.transform.Find("CardBox/RestartBtn") ?? gameOverPanel.transform.Find("RestartBtn");
                if (t != null) restartButton = t.GetComponent<Button>();
            }

            if (mainMenuButton == null)
            {
                Transform t = gameOverPanel.transform.Find("CardBox/MainMenuBtn") ?? gameOverPanel.transform.Find("MainMenuBtn");
                if (t != null) mainMenuButton = t.GetComponent<Button>();
            }

            // ซ่อน GameOverPanel ตอนเริ่มเกม
            gameOverPanel.SetActive(false);
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(RestartGame);
            restartButton.onClick.AddListener(RestartGame);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(GoToMainMenu);
            mainMenuButton.onClick.AddListener(GoToMainMenu);
        }
    }

    private void Update()
    {
        if (!isGameOver)
        {
            survivalTime += Time.deltaTime;

            // Restart game with R key
            bool restartPressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) restartPressed = true;
#endif
            if (Input.GetKeyDown(KeyCode.R)) restartPressed = true;

            if (restartPressed)
            {
                RestartGame();
            }

            // Update Ball Speed on HUD
            if (activeBall == null)
            {
                activeBall = FindAnyObjectByType<BouncingBall>();
            }

            if (speedText != null && activeBall != null)
            {
                speedText.text = $"⚡ Ball Speed: {activeBall.currentSpeed:F1} m/s";
                speedText.color = (activeBall.currentSpeed > activeBall.baseSpeed + 2f)
                    ? new Color(1f, 0.6f, 0.1f)
                    : new Color(0.3f, 0.9f, 1f);
            }
        }
        else
        {
            // ทางลัดขณะ Game Over
            bool rPressed = false;
            bool mPressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.rKey.wasPressedThisFrame) rPressed = true;
                if (Keyboard.current.mKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame) mPressed = true;
            }
#endif
            if (Input.GetKeyDown(KeyCode.R)) rPressed = true;
            if (Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.Escape)) mPressed = true;

            if (rPressed) RestartGame();
            else if (mPressed) GoToMainMenu();
        }
    }

    private void EnsureEventSystem()
    {
        if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }

    public void OnEnemyDefeated(Vector3 position)
    {
        enemiesDefeated++;
        score += 100;

        if (scoreText != null)
        {
            scoreText.text = $"🎯 ศัตรูที่กำจัด (Defeated): {enemiesDefeated}\n⭐ คะแนน (Score): {score}";
        }
    }

    /// <summary>
    /// เรียกใช้เมื่อผู้เล่นตายเพื่อแสดงผลหน้าจอ Game Over
    /// </summary>
    public void TriggerGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        // ปลดล็อกเคอร์เซอร์เมาส์ให้คลิกปุ่ม UI ได้
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        int minutes = Mathf.FloorToInt(survivalTime / 60f);
        int seconds = Mathf.FloorToInt(survivalTime % 60f);
        string timeStr = $"{minutes:00}:{seconds:00}";
        string stats = $"⏱️ เวลาที่รอด: {timeStr}\n" +
                       $"🎯 ศัตรูที่กำจัด: {enemiesDefeated} ตัว\n" +
                       $"⭐ คะแนนรวม: {score} แต้ม";

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            if (gameOverStatsText != null)
            {
                gameOverStatsText.text = stats;
            }
        }
        else
        {
            CreateGameOverUI(stats);
        }
    }

    public void RestartGame()
    {
        if (SoundEffects.Instance != null) SoundEffects.Instance.PlayButtonClick();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToMainMenu()
    {
        if (SoundEffects.Instance != null) SoundEffects.Instance.PlayButtonClick();
        Time.timeScale = 1f;

        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            SceneManager.LoadScene(0);
        }
    }

    private void CreateHUD()
    {
        GameObject canvasObj = new GameObject("HUDCanvas");
        hudCanvas = canvasObj.AddComponent<Canvas>();
        hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

        // Top Status Panel
        GameObject topPanelObj = new GameObject("TopPanel");
        topPanelObj.transform.SetParent(canvasObj.transform, false);
        Image topBg = topPanelObj.AddComponent<Image>();
        topBg.color = new Color(0.08f, 0.1f, 0.15f, 0.85f);
        RectTransform topRect = topPanelObj.GetComponent<RectTransform>();
        topRect.anchorMin = new Vector2(0.5f, 1f);
        topRect.anchorMax = new Vector2(0.5f, 1f);
        topRect.pivot = new Vector2(0.5f, 1f);
        topRect.anchoredPosition = new Vector2(0, -15);
        topRect.sizeDelta = new Vector2(480, 85);

        // Score Text
        GameObject scoreTextObj = new GameObject("ScoreText");
        scoreTextObj.transform.SetParent(topPanelObj.transform, false);
        scoreText = scoreTextObj.AddComponent<Text>();
        scoreText.font = defaultFont;
        scoreText.fontSize = 20;
        scoreText.alignment = TextAnchor.MiddleCenter;
        scoreText.color = Color.white;
        scoreText.text = $"🎯 ศัตรูที่กำจัด (Defeated): 0\n⭐ คะแนน (Score): 0";
        RectTransform scoreRect = scoreTextObj.GetComponent<RectTransform>();
        scoreRect.anchorMin = Vector2.zero;
        scoreRect.anchorMax = Vector2.one;
        scoreRect.sizeDelta = Vector2.zero;

        // Ball Speed Tag
        GameObject speedObj = new GameObject("SpeedText");
        speedObj.transform.SetParent(canvasObj.transform, false);
        speedText = speedObj.AddComponent<Text>();
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

        // Bottom Left Controls Panel
        controlsPanel = new GameObject("ControlsPanel");
        controlsPanel.transform.SetParent(canvasObj.transform, false);
        Image controlsBg = controlsPanel.AddComponent<Image>();
        controlsBg.color = new Color(0.05f, 0.08f, 0.12f, 0.85f);
        RectTransform ctrlRect = controlsPanel.GetComponent<RectTransform>();
        ctrlRect.anchorMin = new Vector2(0f, 0f);
        ctrlRect.anchorMax = new Vector2(0f, 0f);
        ctrlRect.pivot = new Vector2(0f, 0f);
        ctrlRect.anchoredPosition = new Vector2(20, 20);
        ctrlRect.sizeDelta = new Vector2(420, 155);

        GameObject ctrlTextObj = new GameObject("ControlsText");
        ctrlTextObj.transform.SetParent(controlsPanel.transform, false);
        Text ctrlText = ctrlTextObj.AddComponent<Text>();
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
    }

    private void CreateGameOverUI(string stats)
    {
        EnsureEventSystem();

        GameObject canvasObj = hudCanvas != null ? hudCanvas.gameObject : GameObject.Find("HUDCanvas");
        if (canvasObj == null)
        {
            canvasObj = new GameObject("GameOverCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

        gameOverPanel = new GameObject("GameOverPanel");
        gameOverPanel.transform.SetParent(canvasObj.transform, false);
        RectTransform panelRect = gameOverPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;

        Image panelBg = gameOverPanel.AddComponent<Image>();
        panelBg.color = new Color(0.04f, 0.02f, 0.02f, 0.88f);

        GameObject cardObj = new GameObject("CardBox");
        cardObj.transform.SetParent(gameOverPanel.transform, false);
        Image cardBg = cardObj.AddComponent<Image>();
        cardBg.color = new Color(0.1f, 0.12f, 0.16f, 0.98f);
        RectTransform cardRect = cardObj.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(560, 480);

        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(cardObj.transform, false);
        Text titleText = titleObj.AddComponent<Text>();
        titleText.font = defaultFont;
        titleText.fontSize = 50;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(1f, 0.28f, 0.28f);
        titleText.text = "💀 GAME OVER";
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0, -30);
        titleRect.sizeDelta = new Vector2(0, 65);

        GameObject subObj = new GameObject("Subtitle");
        subObj.transform.SetParent(cardObj.transform, false);
        Text subText = subObj.AddComponent<Text>();
        subText.font = defaultFont;
        subText.fontSize = 18;
        subText.alignment = TextAnchor.MiddleCenter;
        subText.color = new Color(0.8f, 0.85f, 0.9f);
        subText.text = "คุณพ่ายแพ้ในการต่อสู้!";
        RectTransform subRect = subObj.GetComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0f, 1f);
        subRect.anchorMax = new Vector2(1f, 1f);
        subRect.pivot = new Vector2(0.5f, 1f);
        subRect.anchoredPosition = new Vector2(0, -95);
        subRect.sizeDelta = new Vector2(0, 30);

        GameObject statsObj = new GameObject("StatsText");
        statsObj.transform.SetParent(cardObj.transform, false);
        gameOverStatsText = statsObj.AddComponent<Text>();
        gameOverStatsText.font = defaultFont;
        gameOverStatsText.fontSize = 22;
        gameOverStatsText.lineSpacing = 1.4f;
        gameOverStatsText.alignment = TextAnchor.MiddleCenter;
        gameOverStatsText.color = Color.white;
        gameOverStatsText.text = stats;
        RectTransform statsRect = statsObj.GetComponent<RectTransform>();
        statsRect.anchorMin = new Vector2(0.1f, 0.42f);
        statsRect.anchorMax = new Vector2(0.9f, 0.72f);
        statsRect.sizeDelta = Vector2.zero;

        restartButton = CreateButton(cardObj.transform, "RestartBtn", "🔄 เล่นอีกครั้ง [R]", new Vector2(0, -115), new Color(0.18f, 0.65f, 0.35f), defaultFont, RestartGame);
        mainMenuButton = CreateButton(cardObj.transform, "MainMenuBtn", "🏠 เมนูหลัก [M]", new Vector2(0, -180), new Color(0.25f, 0.45f, 0.75f), defaultFont, GoToMainMenu);
    }

    private Button CreateButton(Transform parent, string name, string label, Vector2 position, Color normalCol, Font font, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = normalCol;

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = normalCol;
        colors.highlightedColor = normalCol * 1.25f;
        colors.pressedColor = normalCol * 0.8f;
        colors.selectedColor = normalCol * 1.1f;
        btn.colors = colors;
        if (onClick != null) btn.onClick.AddListener(onClick);

        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(360, 50);
        rect.anchoredPosition = position;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        Text btnText = textObj.AddComponent<Text>();
        btnText.font = font;
        btnText.fontSize = 20;
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