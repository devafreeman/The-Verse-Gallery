using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Parents every object in my scene under one Verse_Gallery_World container
/// so the whole gallery can be moved while the skybox stays fixed.
/// Menu: Verse Gallery > Pack Scene Into Movable Container
/// </summary>
public static class PackSceneIntoContainer
{
    const string ScenePath = "Assets/Scenes/my scene.unity";
    const string FlagPath = "Assets/Editor/RUN_PACK_WORLD.flag";
    const string ContainerName = "Verse_Gallery_World";

    [MenuItem("Verse Gallery/Pack Scene Into Movable Container")]
    public static void PackMenu() => Pack(showDialog: true);

    [InitializeOnLoadMethod]
    static void AutoRunFromFlag()
    {
        EditorApplication.delayCall += () =>
        {
            if (!System.IO.File.Exists(FlagPath)) return;
            try { System.IO.File.Delete(FlagPath); } catch { return; }
            Pack(showDialog: true);
        };
    }

    public static void PackBatch()
    {
        Pack(showDialog: false);
        EditorApplication.Exit(0);
    }

    static void Pack(bool showDialog)
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var container = GameObject.Find(ContainerName);
        if (container == null)
        {
            container = new GameObject(ContainerName);
            Undo.RegisterCreatedObjectUndo(container, ContainerName);
        }

        // Keep container at world origin so parenting preserves current layout
        Undo.RecordObject(container.transform, "Reset world container");
        container.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        container.transform.localScale = Vector3.one;

        Undo.SetCurrentGroupName("Pack Scene Into Movable Container");
        int undo = Undo.GetCurrentGroup();

        int moved = 0;
        var roots = scene.GetRootGameObjects();
        foreach (var go in roots)
        {
            if (go == null || go == container) continue;

            Undo.SetTransformParent(go.transform, container.transform, "Parent under world container");
            moved++;
        }

        Undo.CollapseUndoOperations(undo);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = container;
        EditorGUIUtility.PingObject(container);

        Debug.Log($"[PackSceneIntoContainer] Moved {moved} root objects under '{ContainerName}'.");

        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "Scene packed",
                $"Everything in the scene is now inside '{ContainerName}'.\n\n" +
                "Select that object in the Hierarchy and move it — the whole gallery moves together.\n" +
                "The skybox stays fixed in the background.",
                "OK");
        }
    }
}
