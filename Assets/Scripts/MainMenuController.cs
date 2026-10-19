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
/// Controls the Main Menu UI, transitions to game scenes, instructions popup, and quit game.
/// All UI elements can be directly modified and assigned via the Unity Hierarchy and Inspector.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Scene Settings")]
#if UNITY_EDITOR
    [Tooltip("ลากไฟล์ Scene (.unity) ที่ต้องการให้โหลดเมื่อกดปุ่ม Play มาวางใส่ที่นี่ได้โดยตรง")]
    public SceneAsset targetSceneAsset;
#endif
    [Tooltip("ชื่อ Scene ที่จะโหลดเมื่อกดปุ่มเริ่มเกม")]
    public string gameSceneName = "Tutorial";

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (targetSceneAsset != null)
        {
            gameSceneName = targetSceneAsset.name;
        }
        else if (!string.IsNullOrEmpty(gameSceneName))
        {
            string[] guids = AssetDatabase.FindAssets("t:Scene " + gameSceneName);
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == gameSceneName)
                {
                    targetSceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                    break;
                }
            }
        }
    }
#endif

    [Header("UI References in Hierarchy (แก้ไขได้โดยตรงใน Hierarchy)")]
    public Canvas mainMenuCanvas;
    public Button playButton;
    public Button howToPlayButton;
    public Button quitButton;
    public GameObject howToPlayPanel;
    public Button closeHowToPlayButton;

    private void Awake()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;

        EnsureEventSystem();
        BindHierarchyUI();
    }

    private void Start()
    {
        // หากใน Hierarchy ยังไม่มี UI เลย ให้สร้างขึ้นมาให้อัตโนมัติเป็นค่าเริ่มต้น
        if (mainMenuCanvas == null && FindAnyObjectByType<Canvas>() == null)
        {
            CreateMenuUI();
        }
    }

    /// <summary>
    /// เชื่อมต่อปุ่มต่างๆ ที่อยู่ใน Hierarchy เข้ากับฟังก์ชันการทำงาน
    /// </summary>
    public void BindHierarchyUI()
    {
        if (playButton != null)
        {
            playButton.onClick.RemoveListener(PlayGame);
            playButton.onClick.AddListener(PlayGame);
        }

        if (howToPlayButton != null)
        {
            howToPlayButton.onClick.RemoveListener(OpenHowToPlay);
            howToPlayButton.onClick.AddListener(OpenHowToPlay);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(QuitGame);
            quitButton.onClick.AddListener(QuitGame);
        }

        if (closeHowToPlayButton != null)
        {
            closeHowToPlayButton.onClick.RemoveListener(CloseHowToPlay);
            closeHowToPlayButton.onClick.AddListener(CloseHowToPlay);
        }

        if (howToPlayPanel != null)
        {
            howToPlayPanel.SetActive(false);
        }
    }

    private void Update()
    {
        // กด ESC เพื่อปิดหน้าต่างวิธีเล่น (ถ้าเปิดอยู่)
        bool escPressed = false;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) escPressed = true;
#endif
        if (Input.GetKeyDown(KeyCode.Escape)) escPressed = true;

        if (escPressed && howToPlayPanel != null && howToPlayPanel.activeSelf)
        {
            CloseHowToPlay();
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

    public void PlayGame()
    {
        if (SoundEffects.Instance != null) SoundEffects.Instance.PlayButtonClick();

        Time.timeScale = 1f;
        if (!string.IsNullOrEmpty(gameSceneName))
        {
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
            if (nextIndex < SceneManager.sceneCountInBuildSettings)
            {
                SceneManager.LoadScene(nextIndex);
            }
            else
            {
                SceneManager.LoadScene(0);
            }
        }
    }

    public void OpenHowToPlay()
    {
        if (SoundEffects.Instance != null) SoundEffects.Instance.PlayButtonClick();
        if (howToPlayPanel != null)
        {
            howToPlayPanel.SetActive(true);
        }
    }

    public void CloseHowToPlay()
    {
        if (SoundEffects.Instance != null) SoundEffects.Instance.PlayButtonClick();
        if (howToPlayPanel != null)
        {
            howToPlayPanel.SetActive(false);
        }
    }

    public void QuitGame()
    {
        if (SoundEffects.Instance != null) SoundEffects.Instance.PlayButtonClick();
        Debug.Log("Quitting Application...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// Fallback: สร้าง UI หน้าเมนูหลักอัตโนมัติหากยังไม่มีใน Scene
    /// </summary>
    private void CreateMenuUI()
    {
        GameObject canvasObj = new GameObject("MainMenuCanvas");
        mainMenuCanvas = canvasObj.AddComponent<Canvas>();
        mainMenuCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

        // Background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.04f, 0.06f, 0.1f, 1f);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        // Title Panel
        GameObject titlePanel = new GameObject("TitlePanel");
        titlePanel.transform.SetParent(canvasObj.transform, false);
        RectTransform titleRect = titlePanel.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.72f);
        titleRect.anchorMax = new Vector2(0.5f, 0.72f);
        titleRect.sizeDelta = new Vector2(900, 200);

        GameObject titleTextObj = new GameObject("TitleText");
        titleTextObj.transform.SetParent(titlePanel.transform, false);
        Text titleText = titleTextObj.AddComponent<Text>();
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

        GameObject subTextObj = new GameObject("SubtitleText");
        subTextObj.transform.SetParent(titlePanel.transform, false);
        Text subText = subTextObj.AddComponent<Text>();
        subText.font = defaultFont;
        subText.fontSize = 24;
        subText.alignment = TextAnchor.LowerCenter;
        subText.color = new Color(0.85f, 0.9f, 0.95f, 0.9f);
        subText.text = "Smash the ball, deflect attacks, and survive the arena!";
        RectTransform subTextRect = subTextObj.GetComponent<RectTransform>();
        subTextRect.anchorMin = Vector2.zero;
        subTextRect.anchorMax = Vector2.one;
        subTextRect.anchoredPosition = new Vector2(0, -25);

        // Buttons
        GameObject btnGroup = new GameObject("ButtonGroup");
        btnGroup.transform.SetParent(canvasObj.transform, false);
        RectTransform btnGroupRect = btnGroup.AddComponent<RectTransform>();
        btnGroupRect.anchorMin = new Vector2(0.5f, 0.38f);
        btnGroupRect.anchorMax = new Vector2(0.5f, 0.38f);
        btnGroupRect.sizeDelta = new Vector2(380, 280);

        playButton = CreateButton(btnGroup.transform, "PlayButton", "🎮 เริ่มเกม (Play)", new Vector2(0, 75), new Color(0.15f, 0.65f, 0.35f), defaultFont, PlayGame);
        howToPlayButton = CreateButton(btnGroup.transform, "HowToPlayButton", "📖 วิธีการเล่น (How to Play)", new Vector2(0, 0), new Color(0.2f, 0.45f, 0.8f), defaultFont, OpenHowToPlay);
        quitButton = CreateButton(btnGroup.transform, "QuitButton", "🚪 ออกจากเกม (Quit)", new Vector2(0, -75), new Color(0.75f, 0.22f, 0.22f), defaultFont, QuitGame);

        // How To Play Modal
        howToPlayPanel = new GameObject("HowToPlayPanel");
        howToPlayPanel.transform.SetParent(canvasObj.transform, false);
        RectTransform panelRect = howToPlayPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;

        Image overlayBg = howToPlayPanel.AddComponent<Image>();
        overlayBg.color = new Color(0f, 0f, 0f, 0.8f);

        GameObject box = new GameObject("ContentBox");
        box.transform.SetParent(howToPlayPanel.transform, false);
        Image boxImg = box.AddComponent<Image>();
        boxImg.color = new Color(0.08f, 0.12f, 0.18f, 0.98f);
        RectTransform boxRect = box.GetComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0.5f, 0.5f);
        boxRect.anchorMax = new Vector2(0.5f, 0.5f);
        boxRect.sizeDelta = new Vector2(720, 500);

        GameObject headerObj = new GameObject("Header");
        headerObj.transform.SetParent(box.transform, false);
        Text headerText = headerObj.AddComponent<Text>();
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

        GameObject detailsObj = new GameObject("Details");
        detailsObj.transform.SetParent(box.transform, false);
        Text detailsText = detailsObj.AddComponent<Text>();
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

        closeHowToPlayButton = CreateButton(box.transform, "CloseButton", "✖ ปิดหน้าต่าง (Close)", new Vector2(0, 45), new Color(0.85f, 0.25f, 0.25f), defaultFont, CloseHowToPlay);
        RectTransform closeBtnRect = closeHowToPlayButton.GetComponent<RectTransform>();
        closeBtnRect.anchorMin = new Vector2(0.5f, 0f);
        closeBtnRect.anchorMax = new Vector2(0.5f, 0f);
        closeBtnRect.sizeDelta = new Vector2(260, 48);

        howToPlayPanel.SetActive(false);
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
        rect.sizeDelta = new Vector2(360, 60);
        rect.anchoredPosition = position;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        Text btnText = textObj.AddComponent<Text>();
        btnText.font = font;
        btnText.fontSize = 24;
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
