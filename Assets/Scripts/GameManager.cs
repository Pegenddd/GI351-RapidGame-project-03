using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// GameManager manages score, defeated enemy counter, and on-screen HUD UI.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    public int enemiesDefeated = 0;
    public int score = 0;

    [Header("UI Settings")]
    [Tooltip("เปิด/ปิดการแสดงผล HUD บนหน้าจอ")]
    public bool showHUD = false;

    // UI References
    private Text scoreText;
    private Text speedText;
    private BouncingBall activeBall;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (showHUD)
        {
            CreateHUD();
        }
        else
        {
            // ทำความสะอาด HUDCanvas หากมีค้างอยู่ใน Scene
            GameObject existingCanvas = GameObject.Find("HUDCanvas");
            if (existingCanvas != null)
            {
                Destroy(existingCanvas);
            }
        }
    }

    private void Start()
    {
        activeBall = FindAnyObjectByType<BouncingBall>();
    }

    private void Update()
    {
        // Restart game with R
        bool restartPressed = false;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) restartPressed = true;
#endif
        if (Input.GetKeyDown(KeyCode.R)) restartPressed = true;

        if (restartPressed)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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

    public void OnEnemyDefeated(Vector3 position)
    {
        enemiesDefeated++;
        score += 100;

        if (scoreText != null)
        {
            scoreText.text = $"🎯 ศัตรูที่กำจัด (Defeated): {enemiesDefeated}\n⭐ คะแนน (Score): {score}";
        }
    }

    private void CreateHUD()
    {
        // Canvas
        GameObject canvasObj = new GameObject("HUDCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

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
        scoreText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
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
        speedText.font = scoreText.font;
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
        GameObject controlsPanel = new GameObject("ControlsPanel");
        controlsPanel.transform.SetParent(canvasObj.transform, false);
        Image controlsBg = controlsPanel.AddComponent<Image>();
        controlsBg.color = new Color(0.05f, 0.08f, 0.12f, 0.85f);
        RectTransform ctrlRect = controlsPanel.GetComponent<RectTransform>();
        ctrlRect.anchorMin = new Vector2(0f, 0f);
        ctrlRect.anchorMax = new Vector2(0f, 0f);
        ctrlRect.pivot = new Vector2(0f, 0f);
        ctrlRect.anchoredPosition = new Vector2(20, 20);
        ctrlRect.sizeDelta = new Vector2(400, 120);

        GameObject ctrlTextObj = new GameObject("ControlsText");
        ctrlTextObj.transform.SetParent(controlsPanel.transform, false);
        Text ctrlText = ctrlTextObj.AddComponent<Text>();
        ctrlText.font = scoreText.font;
        ctrlText.fontSize = 16;
        ctrlText.alignment = TextAnchor.MiddleLeft;
        ctrlText.color = new Color(0.9f, 0.95f, 1f);
        ctrlText.text = "🎮 [W, A, S, D] / ลูกศร: เดิน (Move)\n" +
                        "🏏 [คลิกซ้าย / Space]: หวดลูกบอล (Hit Ball)\n" +
                        "🩸 [บอลทะลุศัตรู]: บอลทะลุไม่หยุด ศัตรูเสียเลือด!\n" +
                        "🔄 [R]: รีสตาร์ทเกม (Restart)";
        RectTransform ctrlTextRect = ctrlTextObj.GetComponent<RectTransform>();
        ctrlTextRect.anchorMin = Vector2.zero;
        ctrlTextRect.anchorMax = Vector2.one;
        ctrlTextRect.offsetMin = new Vector2(15, 10);
        ctrlTextRect.offsetMax = new Vector2(-15, -10);
    }
}
