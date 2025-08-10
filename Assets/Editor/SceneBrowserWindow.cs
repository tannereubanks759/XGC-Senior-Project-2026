using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;

public class SceneBrowserWindow : EditorWindow
{
    private Vector2 scrollPos;
    private List<string> scenePaths = new List<string>();
    private Dictionary<string, Texture2D> sceneThumbnails = new Dictionary<string, Texture2D>();
    private string thumbnailFolder = "Assets/SceneThumbnails";

    [MenuItem("Window/Scene Browser")]
    public static void ShowWindow()
    {
        var window = GetWindow<SceneBrowserWindow>("Scene Browser");
        window.RefreshSceneList();
        window.Show();
    }

    private void OnEnable()
    {
        EditorSceneManager.sceneSaved += OnSceneSaved;

        if (!Directory.Exists(thumbnailFolder))
        {
            Directory.CreateDirectory(thumbnailFolder);
            AssetDatabase.Refresh();
        }
    }

    private void OnDisable()
    {
        EditorSceneManager.sceneSaved -= OnSceneSaved;
    }

    private void OnSceneSaved(Scene scene)
    {
        string path = scene.path;
        CaptureThumbnailFromScene(path, true);
        RefreshSceneList();
    }

    private void RefreshSceneList()
    {
        scenePaths.Clear();
        sceneThumbnails.Clear();

        string[] guids = AssetDatabase.FindAssets("t:Scene");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            scenePaths.Add(path);

            // Try Unity's preview
            Texture2D preview = AssetPreview.GetAssetPreview(AssetDatabase.LoadAssetAtPath<Object>(path));

            // If no preview, try cached
            if (preview == null)
                preview = LoadCachedThumbnail(path);

            sceneThumbnails[path] = preview;
        }

        scenePaths.Sort();
    }

    private Texture2D LoadCachedThumbnail(string scenePath)
    {
        string fileName = Path.GetFileNameWithoutExtension(scenePath) + ".png";
        string fullPath = Path.Combine(thumbnailFolder, fileName);

        if (File.Exists(fullPath))
        {
            byte[] data = File.ReadAllBytes(fullPath);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(data);
            return tex;
        }

        return null;
    }

    private void CaptureThumbnailFromScene(string scenePath, bool forceRefresh = false)
    {
        // Save current scene path
        string currentScenePath = SceneManager.GetActiveScene().path;

        // Open target scene without prompt
        var openedScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // Find camera
        Camera cam = null;
        GameObject thumbCamObj = GameObject.Find("ThumbnailCamera");
        if (thumbCamObj != null)
            cam = thumbCamObj.GetComponent<Camera>();

        if (cam == null)
        {
            Camera[] cams = GameObject.FindObjectsOfType<Camera>();
            if (cams.Length > 0) cam = cams[0];
        }

        if (cam != null)
        {
            RenderTexture rt = new RenderTexture(256, 144, 24);
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(256, 144, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 256, 144), 0, 0);
            tex.Apply();

            cam.targetTexture = null;
            RenderTexture.active = null;
            DestroyImmediate(rt);

            string fileName = Path.GetFileNameWithoutExtension(scenePath) + ".png";
            string savePath = Path.Combine(thumbnailFolder, fileName);

            // Always overwrite if forceRefresh or file doesn't exist
            if (forceRefresh || !File.Exists(savePath))
            {
                File.WriteAllBytes(savePath, tex.EncodeToPNG());
                AssetDatabase.Refresh();
            }
        }

        // Reopen original scene if valid
        if (!string.IsNullOrEmpty(currentScenePath) && File.Exists(currentScenePath))
        {
            EditorSceneManager.OpenScene(currentScenePath, OpenSceneMode.Single);
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Scenes in Project", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh List", GUILayout.Height(20)))
        {
            RefreshSceneList();
        }
        if (GUILayout.Button("Regenerate All Thumbnails", GUILayout.Height(20)))
        {
            foreach (var path in scenePaths)
            {
                CaptureThumbnailFromScene(path, true);
                sceneThumbnails[path] = LoadCachedThumbnail(path);
            }
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(5);

        int columns = Mathf.Max(1, Mathf.FloorToInt(position.width / 150f));
        int count = 0;

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        EditorGUILayout.BeginHorizontal();

        foreach (string scenePath in scenePaths)
        {
            DrawSceneItem(scenePath);
            count++;

            if (count % columns == 0)
            {
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
            }
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndScrollView();
    }

    private void DrawSceneItem(string scenePath)
    {
        string sceneName = Path.GetFileNameWithoutExtension(scenePath);
        sceneThumbnails.TryGetValue(scenePath, out Texture2D preview);

        GUILayout.BeginVertical("box", GUILayout.Width(140));

        if (preview != null)
            GUILayout.Label(preview, GUILayout.Width(128), GUILayout.Height(72));
        else
            GUILayout.Label("No Preview", EditorStyles.centeredGreyMiniLabel, GUILayout.Height(72));

        GUILayout.Label(sceneName, EditorStyles.boldLabel, GUILayout.Width(128), GUILayout.Height(18));

        if (GUILayout.Button("Open", GUILayout.Width(128)))
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(scenePath);
            }
        }

        if (GUILayout.Button("Ping", GUILayout.Width(128)))
        {
            Object sceneObj = AssetDatabase.LoadAssetAtPath<Object>(scenePath);
            EditorGUIUtility.PingObject(sceneObj);
        }

        if (GUILayout.Button("Regenerate", GUILayout.Width(128)))
        {
            CaptureThumbnailFromScene(scenePath, true);
            sceneThumbnails[scenePath] = LoadCachedThumbnail(scenePath);
        }

        GUILayout.EndVertical();
    }
}
