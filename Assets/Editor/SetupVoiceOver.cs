using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Places Gallery_VoiceOver with a 2s delayed intro clip over background music.
/// Menu: Verse Gallery > Setup Intro Voice Over
/// </summary>
public static class SetupVoiceOver
{
    const string ScenePath = "Assets/Scenes/my scene.unity";
    const string FlagPath = "Assets/Editor/RUN_SETUP_VOICEOVER.flag";
    const string ObjectName = "Gallery_VoiceOver";
    const string ClipPath = "Assets/Gallery/Audio/GalleryIntro_VoiceOver.mp3";

    [MenuItem("Verse Gallery/Setup Intro Voice Over")]
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
            string[] guids = AssetDatabase.FindAssets("GalleryIntro_VoiceOver t:AudioClip");
            if (guids != null && guids.Length > 0)
                clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        if (clip == null)
        {
            Debug.LogError($"[SetupVoiceOver] Could not find {ClipPath}");
            if (showDialog)
                EditorUtility.DisplayDialog("Missing voice over", $"Could not find:\n{ClipPath}", "OK");
            return;
        }

        var go = new GameObject(ObjectName);
        Undo.RegisterCreatedObjectUndo(go, ObjectName);

        var source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.volume = 1f;
        source.priority = 32;

        var vo = go.AddComponent<VoiceOverIntro>();
        vo.voiceClip = clip;
        vo.volume = 1f;
        vo.delaySeconds = 2f;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = go;

        Debug.Log("[SetupVoiceOver] Gallery_VoiceOver ready — plays 2s after Play starts.");

        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "Voice over ready",
                "Gallery_VoiceOver will play your ElevenLabs clip 2 seconds after Play starts, over the background music.\n\nAdjust Volume / Delay Seconds on Gallery_VoiceOver if needed.",
                "OK");
        }
    }
}
