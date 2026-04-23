// Assets/Editor/IconCreatorWindow.cs
// A free icon creator for Unity. Drop a prefab in, orient it, save a transparent PNG.
// Menu: Tools > Icon Creator

using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class IconCreatorWindow : EditorWindow
{
    // ─── Model ─────────────────────────────────────────
    private GameObject sourcePrefab;
    private Vector3 modelRotation = new Vector3(-15f, 30f, 0f);
    private float zoom = 1f;

    // ─── Camera ────────────────────────────────────────
    private bool orthographic = true;
    private float fov = 30f;

    // ─── Background ────────────────────────────────────
    private bool transparent = true;
    private Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);

    // ─── Lighting ──────────────────────────────────────
    private Color keyLightColor = Color.white;
    private float keyLightIntensity = 1.1f;
    private Vector3 keyLightRotation = new Vector3(50f, -30f, 0f);
    private Color fillLightColor = new Color(0.75f, 0.85f, 1f);
    private float fillLightIntensity = 0.45f;
    private Color ambientColor = new Color(0.35f, 0.35f, 0.35f);

    // ─── Output ────────────────────────────────────────
    private int outputSize = 256;
    private string outputFolder = "Assets/Icons";
    private string outputName = "NewIcon";

    // ─── Internal ──────────────────────────────────────
    private Scene previewScene;
    private GameObject root;
    private GameObject pivot;
    private GameObject modelInstance;
    private Camera cam;
    private Light keyLight;
    private Light fillLight;
    private RenderTexture previewRT;
    private Bounds modelBounds;
    private Vector2 scrollPos;


    // ─── Particles ─────────────────────────────────────
    private float simulationTime = 0.5f;
    private float maxSimulationTime = 3f;


    [MenuItem("Tools/Icon Creator")]
    public static void Open()
    {
        var w = GetWindow<IconCreatorWindow>("Icon Creator");
        w.minSize = new Vector2(580, 640);
    }

    private void OnEnable() => SetupScene();
    private void OnDisable() => TeardownScene();

    // ───────────────────────────────────────────────────
    // Preview scene lifecycle
    // ───────────────────────────────────────────────────
    private void SetupScene()
    {
        previewScene = EditorSceneManager.NewPreviewScene();

        root = new GameObject("~IconCreatorRoot");
        SceneManager.MoveGameObjectToScene(root, previewScene);

        pivot = new GameObject("Pivot");
        pivot.transform.SetParent(root.transform, false);

        var camGO = new GameObject("IconCamera");
        camGO.transform.SetParent(root.transform, false);
        cam = camGO.AddComponent<Camera>();
        cam.scene = previewScene;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.enabled = false;          // we'll drive it manually
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = 500f;
        cam.cameraType = CameraType.Preview;

        var k = new GameObject("KeyLight");
        k.transform.SetParent(root.transform, false);
        keyLight = k.AddComponent<Light>();
        keyLight.type = LightType.Directional;

        var f = new GameObject("FillLight");
        f.transform.SetParent(root.transform, false);
        fillLight = f.AddComponent<Light>();
        fillLight.type = LightType.Directional;
    }

    private void TeardownScene()
    {
        if (previewRT != null)
        {
            previewRT.Release();
            DestroyImmediate(previewRT);
            previewRT = null;
        }
        if (previewScene.IsValid())
            EditorSceneManager.ClosePreviewScene(previewScene);
    }

    // ───────────────────────────────────────────────────
    // Model handling
    // ───────────────────────────────────────────────────
    private void InstantiateModel()
    {
        if (modelInstance != null) DestroyImmediate(modelInstance);
        if (sourcePrefab == null) return;

        modelInstance = Instantiate(sourcePrefab, pivot.transform);
        modelInstance.name = "Model";
        modelInstance.transform.localRotation = Quaternion.identity;
        modelInstance.transform.localPosition = Vector3.zero;
        SceneManager.MoveGameObjectToScene(modelInstance.transform.root.gameObject, previewScene);

        RecomputeBounds();
    }

    private void RecomputeBounds()
    {
        if (modelInstance == null) { modelBounds = new Bounds(); return; }

        var renderers = modelInstance.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            modelBounds = new Bounds(modelInstance.transform.position, Vector3.one);
            return;
        }

        // Make sure bounds are measured before any pivot rotation
        pivot.transform.rotation = Quaternion.identity;
        modelInstance.transform.localPosition = Vector3.zero;

        var b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        modelBounds = b;

        // Offset so the model's world-space center sits on the pivot origin
        modelInstance.transform.localPosition = -b.center;
    }

    // ───────────────────────────────────────────────────
    // GUI
    // ───────────────────────────────────────────────────
    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.BeginVertical(GUILayout.Width(290));
        DrawSettings();
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical();
        DrawPreview();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    private void DrawSettings()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        EditorGUILayout.LabelField("Model", EditorStyles.boldLabel);
        var newPrefab = (GameObject)EditorGUILayout.ObjectField("Prefab", sourcePrefab, typeof(GameObject), false);
        if (newPrefab != sourcePrefab)
        {
            sourcePrefab = newPrefab;
            InstantiateModel();
            outputName = sourcePrefab != null ? sourcePrefab.name + "_Icon" : "NewIcon";
        }
        if (GUILayout.Button("Reset View"))
        {
            modelRotation = new Vector3(-15f, 30f, 0f);
            zoom = 1f;
            RecomputeBounds();
        }
        // Show particle controls if the model has a particle system
        if (modelInstance != null && modelInstance.GetComponentInChildren<ParticleSystem>() != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Particle System", EditorStyles.boldLabel);
            maxSimulationTime = EditorGUILayout.Slider("Max Time", maxSimulationTime, 1f, 10f);
            simulationTime = EditorGUILayout.Slider("Freeze Frame", simulationTime, 0f, maxSimulationTime);
            EditorGUILayout.HelpBox("Scrub 'Freeze Frame' to find the best-looking moment.", MessageType.Info);
        }
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Transform", EditorStyles.boldLabel);
        modelRotation = EditorGUILayout.Vector3Field("Rotation", modelRotation);
        zoom = EditorGUILayout.Slider("Zoom", zoom, 0.1f, 4f);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Camera", EditorStyles.boldLabel);
        orthographic = EditorGUILayout.Toggle("Orthographic", orthographic);
        using (new EditorGUI.DisabledScope(orthographic))
            fov = EditorGUILayout.Slider("FOV", fov, 10f, 80f);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Background", EditorStyles.boldLabel);
        transparent = EditorGUILayout.Toggle("Transparent", transparent);
        using (new EditorGUI.DisabledScope(transparent))
            backgroundColor = EditorGUILayout.ColorField("Color", backgroundColor);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Lighting", EditorStyles.boldLabel);
        keyLightColor = EditorGUILayout.ColorField("Key Color", keyLightColor);
        keyLightIntensity = EditorGUILayout.Slider("Key Intensity", keyLightIntensity, 0f, 3f);
        keyLightRotation = EditorGUILayout.Vector3Field("Key Rotation", keyLightRotation);
        EditorGUILayout.Space(2);
        fillLightColor = EditorGUILayout.ColorField("Fill Color", fillLightColor);
        fillLightIntensity = EditorGUILayout.Slider("Fill Intensity", fillLightIntensity, 0f, 3f);
        EditorGUILayout.Space(2);
        ambientColor = EditorGUILayout.ColorField("Ambient", ambientColor);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
        outputSize = EditorGUILayout.IntPopup("Size", outputSize,
            new[] { "64", "128", "256", "512", "1024" },
            new[] { 64, 128, 256, 512, 1024 });
        outputFolder = EditorGUILayout.TextField("Folder", outputFolder);
        outputName = EditorGUILayout.TextField("Filename", outputName);

        EditorGUILayout.Space(10);
        using (new EditorGUI.DisabledScope(modelInstance == null))
        {
            if (GUILayout.Button("Save PNG", GUILayout.Height(34)))
                SaveIcon();
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "• Drag in the preview to rotate.\n" +
            "• Scroll wheel to zoom.\n" +
            "• Transparent bg writes alpha to the PNG.\n" +
            "• Output is auto-imported as a Sprite.",
            MessageType.None);

        EditorGUILayout.EndScrollView();
    }

    private void DrawPreview()
    {
        var rect = GUILayoutUtility.GetRect(
            100, 100,
            GUILayout.ExpandWidth(true),
            GUILayout.ExpandHeight(true));

        int size = Mathf.Min((int)rect.width, (int)rect.height);
        if (size < 32) return;

        if (previewRT == null || previewRT.width != size)
        {
            if (previewRT != null) { previewRT.Release(); DestroyImmediate(previewRT); }
            previewRT = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32);
            previewRT.Create();
        }

        RenderToRT(previewRT);

        var draw = new Rect(rect.x + (rect.width - size) * 0.5f, rect.y, size, size);
        EditorGUI.DrawTextureTransparent(draw, previewRT);

        var e = Event.current;
        if (draw.Contains(e.mousePosition))
        {
            if (e.type == EventType.MouseDrag && e.button == 0)
            {
                modelRotation.y -= e.delta.x * 0.5f;
                modelRotation.x += e.delta.y * 0.5f;
                e.Use();
                Repaint();
            }
            else if (e.type == EventType.ScrollWheel)
            {
                zoom *= (1f - e.delta.y * 0.05f);
                zoom = Mathf.Clamp(zoom, 0.1f, 4f);
                e.Use();
                Repaint();
            }
        }
    }

    // ───────────────────────────────────────────────────
    // Rendering
    // ───────────────────────────────────────────────────
    private void RenderToRT(RenderTexture rt)
    {
        // Always clear first so the background is correct even if model is null
        var prevActive = RenderTexture.active;
        RenderTexture.active = rt;
        GL.Clear(true, true, transparent ? new Color(0, 0, 0, 0) : backgroundColor);
        RenderTexture.active = prevActive;

        if (modelInstance == null) return;

        pivot.transform.rotation = Quaternion.Euler(modelRotation);

        // Frame camera based on bounds
        float maxExt = Mathf.Max(modelBounds.extents.x, modelBounds.extents.y, modelBounds.extents.z);
        if (maxExt < 0.001f) maxExt = 0.5f;

        cam.transform.position = new Vector3(0, 0, -maxExt * 6f);
        cam.transform.rotation = Quaternion.identity;

        cam.orthographic = orthographic;
        if (orthographic) cam.orthographicSize = maxExt * 1.25f / zoom;
        else cam.fieldOfView = fov / zoom;

        cam.backgroundColor = transparent ? new Color(0, 0, 0, 0) : backgroundColor;

        keyLight.color = keyLightColor;
        keyLight.intensity = keyLightIntensity;
        keyLight.transform.rotation = Quaternion.Euler(keyLightRotation);

        fillLight.color = fillLightColor;
        fillLight.intensity = fillLightIntensity;
        fillLight.transform.rotation = Quaternion.Euler(-keyLightRotation.x, keyLightRotation.y + 180f, 0f);

        // Swap ambient for the render, then restore
        var prevMode = RenderSettings.ambientMode;
        var prevAmb = RenderSettings.ambientLight;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = ambientColor;
        // Simulate particle systems to the chosen frame
        var particles = modelInstance.GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in particles)
        {
            ps.Simulate(simulationTime, false, true, false);
        }
        cam.targetTexture = rt;
        cam.Render();
        cam.targetTexture = null;

        RenderSettings.ambientMode = prevMode;
        RenderSettings.ambientLight = prevAmb;
    }

    // ───────────────────────────────────────────────────
    // Save
    // ───────────────────────────────────────────────────
    private void SaveIcon()
    {
        var rt = new RenderTexture(outputSize, outputSize, 24, RenderTextureFormat.ARGB32);
        rt.Create();
        RenderToRT(rt);

        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(outputSize, outputSize, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, outputSize, outputSize), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;

        var bytes = tex.EncodeToPNG();

        if (!Directory.Exists(outputFolder)) Directory.CreateDirectory(outputFolder);
        string safeName = string.IsNullOrWhiteSpace(outputName) ? "NewIcon" : outputName;
        string path = Path.Combine(outputFolder, safeName + ".png").Replace('\\', '/');
        File.WriteAllBytes(path, bytes);

        rt.Release();
        DestroyImmediate(rt);
        DestroyImmediate(tex);

        AssetDatabase.Refresh();

        // Auto-configure as a Sprite if inside the project
        if (path.StartsWith("Assets/"))
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Texture2D>(path));
        }

        Debug.Log($"[IconCreator] Saved: {path}");
    }
}