using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Places looping background music (Dreams of Highland Pass) in my scene.
/// Menu: Verse Gallery > Setup Background Music
/// </summary>
public static class SetupBackgroundMusic
{
    const string ScenePath = "Assets/Scenes/my scene.unity";
    const string FlagPath = "Assets/Editor/RUN_BACKGROUND_MUSIC.flag";
    const string ObjectName = "Gallery_BackgroundMusic";
    const string ClipPath =
        "Assets/Timeless European Strings – Romantic Cello, Folk Violin & Cinematic Acoustic Music Pack - Free/Dreams of Highland Pass.wav";

    [MenuItem("Verse Gallery/Setup Background Music")]
    public static void SetupMenu() => Setup(showDialog: true);

    [InitializeOnLoadMethod]
    static void AutoRunFromFlag()
    {
        EditorApplication.delayCall += () =>
        {
            if (!System.IO.File.Exists(FlagPath)) return;
            try { System.IO.File.Delete(FlagPath); } catch { return; }
            Setup(showDialog: true);
        };
    }

    public static void SetupBatch()
    {
        Setup(showDialog: false);
        EditorApplication.Exit(0);
    }

    static void Setup(bool showDialog)
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var existing = GameObject.Find(ObjectName);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing);

        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ClipPath);
        if (clip == null)
        {
            // Fallback: search by file name if the en-dash path differs on disk.
            string[] guids = AssetDatabase.FindAssets("Dreams of Highland Pass t:AudioClip");
            if (guids != null && guids.Length > 0)
                clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        if (clip == null)
        {
            Debug.LogError("[SetupBackgroundMusic] Could not find Dreams of Highland Pass.wav");
            if (showDialog)
                EditorUtility.DisplayDialog("Missing audio", "Could not find Dreams of Highland Pass in the Timeless European Strings pack.", "OK");
            return;
        }

        ConfigureClipImport(clip);

        var go = new GameObject(ObjectName);
        Undo.RegisterCreatedObjectUndo(go, "Gallery Background Music");

        var source = go.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = true;
        source.spatialBlend = 0f;
        source.volume = 0.45f;
        source.priority = 64;

        var music = go.AddComponent<BackgroundMusic>();
        music.musicClip = clip;
        music.volume = 0.45f;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = go;

        Debug.Log("[SetupBackgroundMusic] Background music set to Dreams of Highland Pass.");

        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "Background music ready",
                "Dreams of Highland Pass will loop as gallery background music while you play.\n\nVolume is 45% — adjust on Gallery_BackgroundMusic if needed.",
                "OK");
        }
    }

    static void ConfigureClipImport(AudioClip clip)
    {
        string path = AssetDatabase.GetAssetPath(clip);
        var importer = AssetImporter.GetAtPath(path) as AudioImporter;
        if (importer == null) return;

        var settings = importer.defaultSampleSettings;
        bool dirty = false;

        if (settings.loadType != AudioClipLoadType.Streaming)
        {
            settings.loadType = AudioClipLoadType.Streaming;
            dirty = true;
        }

        if (importer.forceToMono)
        {
            importer.forceToMono = false;
            dirty = true;
        }

        // Prefer 2D music playback defaults.
        var so = new SerializedObject(importer);
        var loadInBackground = so.FindProperty("m_LoadInBackground");
        if (loadInBackground != null && !loadInBackground.boolValue)
        {
            loadInBackground.boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            dirty = true;
        }

        if (dirty)
        {
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
        }
    }
}
