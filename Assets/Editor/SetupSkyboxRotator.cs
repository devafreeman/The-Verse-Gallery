using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Adds a SkyboxRotator so the sky moves during Play mode.
/// Menu: Verse Gallery > Setup Moving Skybox
/// </summary>
public static class SetupSkyboxRotator
{
    const string ScenePath = "Assets/Scenes/my scene.unity";
    const string FlagPath = "Assets/Editor/RUN_SETUP_SKYBOX_ROTATOR.flag";
    const string ObjectName = "Gallery_SkyboxRotator";

    [MenuItem("Verse Gallery/Setup Moving Skybox")]
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

        var go = new GameObject(ObjectName);
        Undo.RegisterCreatedObjectUndo(go, ObjectName);

        var world = GameObject.Find("Verse_Gallery_World");
        if (world != null)
            go.transform.SetParent(world.transform, true);

        var rotator = go.AddComponent<SkyboxRotator>();
        rotator.degreesPerSecond = 2.5f;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = go;

        Debug.Log("[SetupSkyboxRotator] Skybox will rotate during Play mode.");

        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "Moving skybox ready",
                "Sky_Noon will slowly rotate while you are in Play mode.\n\n" +
                "Select Gallery_SkyboxRotator to change Degrees Per Second (default 2.5).",
                "OK");
        }
    }
}
