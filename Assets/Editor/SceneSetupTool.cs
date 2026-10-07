using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor tool that permanently generates all GameObjects and Materials into SampleScene.
/// This allows the developer to freely select, move, scale, and edit all components in the Scene View and Inspector.
/// </summary>
[InitializeOnLoad]
public static class SceneSetupTool
{
    static SceneSetupTool()
    {
        EditorApplication.delayCall += CheckAndSetupScene;
    }

    [MenuItem("Game/Setup Scene Objects (Make Editable In Scene)")]
    public static void MenuSetup()
    {
        ExecuteSetup(true);
    }

    private static void CheckAndSetupScene()
    {
        // Only run if Arena or Player doesn't exist yet in the scene
        if (GameObject.Find("Arena") == null || GameObject.Find("Player") == null)
        {
            ExecuteSetup(false);
        }
    }

    public static void ExecuteSetup(bool forceRebuild)
    {
        Scene activeScene = EditorSceneManager.GetActiveScene();
        if (!activeScene.isLoaded) return;

        if (forceRebuild)
        {
            // Destroy existing generated objects to rebuild cleanly
            SafeDestroy(GameObject.Find("Arena"));
            SafeDestroy(GameObject.Find("Player"));
            SafeDestroy(GameObject.Find("BouncingBall"));
            SafeDestroy(GameObject.Find("EnemySpawner"));
            SafeDestroy(GameObject.Find("GameManager"));
            SafeDestroy(GameObject.Find("SoundEffectsManager"));
            SafeDestroy(GameObject.Find("HUDCanvas"));
            SafeDestroy(GameObject.Find("GameSetup"));
        }
        else
        {
            if (GameObject.Find("Arena") != null && GameObject.Find("Player") != null)
            {
                return; // Already configured
            }
        }

        Debug.Log("🔨 [SceneSetupTool] Building editable scene objects and materials...");

        // 1. Create / Load Materials
        Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");

        Material groundMat = GetOrCreateMaterial("Assets/Materials/GroundMat.mat", litShader, new Color(0.12f, 0.14f, 0.18f));
        Material wallMat = GetOrCreateMaterial("Assets/Materials/WallMat.mat", litShader, new Color(0.2f, 0.5f, 0.8f));
        Material playerMat = GetOrCreateMaterial("Assets/Materials/PlayerMat.mat", litShader, new Color(0.2f, 0.7f, 1f));
        Material batMat = GetOrCreateMaterial("Assets/Materials/BatMat.mat", litShader, new Color(1f, 0.75f, 0.1f));
        Material ballMat = GetOrCreateMaterial("Assets/Materials/BallMat.mat", litShader, new Color(0.2f, 0.9f, 1f));
        Material eyeMat = GetOrCreateMaterial("Assets/Materials/EyeMat.mat", unlitShader, Color.white);

        // 2. Setup Directional Light
        SetupLighting();

        // 3. Setup Arena (Ground & 4 Walls)
        GameObject arenaObj = GameObject.Find("Arena");
        if (arenaObj == null)
        {
            arenaObj = new GameObject("Arena");

            // Ground Floor
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(arenaObj.transform);
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(3f, 1f, 3f);
            ground.GetComponent<Renderer>().sharedMaterial = groundMat;

            // 4 Boundary Walls
            CreateWall("Wall_North", arenaObj.transform, new Vector3(0, 1f, 15f), new Vector3(31f, 2f, 1f), wallMat);
            CreateWall("Wall_South", arenaObj.transform, new Vector3(0, 1f, -15f), new Vector3(31f, 2f, 1f), wallMat);
            CreateWall("Wall_East", arenaObj.transform, new Vector3(15f, 1f, 0), new Vector3(1f, 2f, 31f), wallMat);
            CreateWall("Wall_West", arenaObj.transform, new Vector3(-15f, 1f, 0), new Vector3(1f, 2f, 31f), wallMat);
        }

        // 4. Setup Player
        GameObject playerObj = GameObject.Find("Player");
        if (playerObj == null)
        {
            playerObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            playerObj.name = "Player";
            playerObj.tag = "Player";
            playerObj.transform.position = new Vector3(0, 1f, -6f);
            playerObj.GetComponent<Renderer>().sharedMaterial = playerMat;

            // Bat Weapon
            GameObject bat = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bat.name = "BatWeapon";
            bat.transform.SetParent(playerObj.transform);
            bat.transform.localPosition = new Vector3(0.5f, 0.2f, 0.45f);
            bat.transform.localRotation = Quaternion.Euler(45f, 30f, 0f);
            bat.transform.localScale = new Vector3(0.15f, 0.65f, 0.15f);
            Object.DestroyImmediate(bat.GetComponent<Collider>());
            bat.GetComponent<Renderer>().sharedMaterial = batMat;

            // Player Eyes
            GameObject eyes = GameObject.CreatePrimitive(PrimitiveType.Cube);
            eyes.name = "Eyes";
            eyes.transform.SetParent(playerObj.transform);
            eyes.transform.localPosition = new Vector3(0, 0.5f, 0.45f);
            eyes.transform.localScale = new Vector3(0.5f, 0.2f, 0.25f);
            Object.DestroyImmediate(eyes.GetComponent<Collider>());
            eyes.GetComponent<Renderer>().sharedMaterial = eyeMat;

            PlayerController pc = playerObj.AddComponent<PlayerController>();
            pc.batTransform = bat.transform;
        }

        // 5. Setup Bouncing Ball
        GameObject ballObj = GameObject.Find("BouncingBall");
        if (ballObj == null)
        {
            ballObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ballObj.name = "BouncingBall";
            ballObj.tag = "Respawn";
            ballObj.transform.position = new Vector3(0, 0.6f, -1.5f);
            ballObj.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
            ballObj.GetComponent<Renderer>().sharedMaterial = ballMat;

            BouncingBall ballComp = ballObj.AddComponent<BouncingBall>();

            TrailRenderer tr = ballObj.AddComponent<TrailRenderer>();
            tr.time = 0.35f;
            tr.startWidth = 0.7f;
            tr.endWidth = 0.05f;
            tr.sharedMaterial = new Material(unlitShader);
        }

        // 6. Setup Camera with Bird's Eye View & CameraController
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.transform.position = new Vector3(0, 18f, -10f);
            mainCam.transform.rotation = Quaternion.Euler(60f, 0, 0);
            mainCam.fieldOfView = 60f;

            CameraController camCtrl = mainCam.GetComponent<CameraController>();
            if (camCtrl == null) camCtrl = mainCam.gameObject.AddComponent<CameraController>();
            if (playerObj != null) camCtrl.target = playerObj.transform;

            if (mainCam.GetComponent<AudioListener>() == null)
            {
                mainCam.gameObject.AddComponent<AudioListener>();
            }
        }

        // 7. Setup Managers
        if (GameObject.Find("SoundEffectsManager") == null)
        {
            GameObject sfx = new GameObject("SoundEffectsManager");
            sfx.AddComponent<SoundEffects>();
        }

        if (GameObject.Find("GameManager") == null)
        {
            GameObject gm = new GameObject("GameManager");
            gm.AddComponent<GameManager>();
        }

        if (GameObject.Find("EnemySpawner") == null)
        {
            GameObject spawner = new GameObject("EnemySpawner");
            spawner.AddComponent<EnemySpawner>();
        }

        // Clean up redundant GameSetup GameObject if present
        GameObject legacySetup = GameObject.Find("GameSetup");
        if (legacySetup != null)
        {
            SafeDestroy(legacySetup);
        }

        // 8. Mark scene dirty and save
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        AssetDatabase.SaveAssets();

        Debug.Log("✅ [SceneSetupTool] Scene successfully updated and saved! All objects are now editable directly in the Scene Hierarchy.");
    }

    private static GameObject CreateWall(string name, Transform parent, Vector3 pos, Vector3 size, Material mat)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(parent);
        wall.transform.position = pos;
        wall.transform.localScale = size;
        wall.GetComponent<Renderer>().sharedMaterial = mat;
        return wall;
    }

    private static void SetupLighting()
    {
        Light dirLight = Object.FindAnyObjectByType<Light>();
        if (dirLight == null)
        {
            GameObject lightObj = new GameObject("Directional Light");
            dirLight = lightObj.AddComponent<Light>();
            dirLight.type = LightType.Directional;
        }

        dirLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        dirLight.intensity = 1.4f;
        dirLight.color = new Color(1f, 0.96f, 0.9f);
        dirLight.shadows = LightShadows.Soft;
    }

    private static Material GetOrCreateMaterial(string assetPath, Shader shader, Color color)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        if (mat == null)
        {
            mat = new Material(shader);
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(mat, assetPath);
        }
        return mat;
    }

    private static void SafeDestroy(GameObject obj)
    {
        if (obj != null)
        {
            Object.DestroyImmediate(obj);
        }
    }
}
