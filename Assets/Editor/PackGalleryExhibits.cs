using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Wraps each Gallery_Podium (including its ClassicBook, BookInfoTrigger, and
/// BookInfoCanvas children) in a Gallery_Exhibit_## container so the whole
/// unit can be moved as one object.
/// Menu: Verse Gallery > Pack Exhibits (Podium + Book + Trigger)
/// </summary>
public static class PackGalleryExhibits
{
    const string ScenePath = "Assets/Scenes/my scene.unity";
    const string FlagPath = "Assets/Editor/RUN_PACK_EXHIBITS.flag";
    const string ExhibitsRootName = "Gallery_Exhibits";
    const string OldPodiumsRootName = "Gallery_Podiums";
    const int Count = 10;

    [MenuItem("Verse Gallery/Pack Exhibits (Podium + Book + Trigger)")]
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

        var exhibitsRoot = GameObject.Find(ExhibitsRootName);
        if (exhibitsRoot == null)
        {
            exhibitsRoot = new GameObject(ExhibitsRootName);
            Undo.RegisterCreatedObjectUndo(exhibitsRoot, ExhibitsRootName);
        }

        Undo.SetCurrentGroupName("Pack Gallery Exhibits");
        int undo = Undo.GetCurrentGroup();
        int packed = 0;

        for (int i = 1; i <= Count; i++)
        {
            string podiumName = $"Gallery_Podium_{i:00}";
            string exhibitName = $"Gallery_Exhibit_{i:00}";

            var podium = GameObject.Find(podiumName);
            if (podium == null)
            {
                Debug.LogWarning($"[PackGalleryExhibits] Missing {podiumName}");
                continue;
            }

            // Already packed?
            if (podium.transform.parent != null &&
                podium.transform.parent.name == exhibitName)
            {
                packed++;
                continue;
            }

            var exhibit = GameObject.Find(exhibitName);
            if (exhibit == null)
            {
                exhibit = new GameObject(exhibitName);
                Undo.RegisterCreatedObjectUndo(exhibit, exhibitName);
            }

            Vector3 worldPos = podium.transform.position;
            Quaternion worldRot = podium.transform.rotation;

            Undo.RecordObject(exhibit.transform, "Place exhibit");
            exhibit.transform.SetParent(exhibitsRoot.transform, false);
            exhibit.transform.position = worldPos;
            exhibit.transform.rotation = worldRot;
            exhibit.transform.localScale = Vector3.one;

            // Podium keeps book + trigger + canvas as children; parenting podium
            // under the exhibit makes the whole set one movable unit.
            Undo.SetTransformParent(podium.transform, exhibit.transform, "Parent podium into exhibit");
            podium.transform.localPosition = Vector3.zero;
            podium.transform.localRotation = Quaternion.identity;
            podium.transform.localScale = Vector3.one;

            packed++;
            EditorUtility.SetDirty(exhibit);
            EditorUtility.SetDirty(podium);
        }

        var oldRoot = GameObject.Find(OldPodiumsRootName);
        if (oldRoot != null && oldRoot.transform.childCount == 0)
            Undo.DestroyObjectImmediate(oldRoot);

        Undo.CollapseUndoOperations(undo);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = exhibitsRoot;

        Debug.Log($"[PackGalleryExhibits] Packed {packed} exhibits. Move Gallery_Exhibit_## to move podium + book + trigger together.");

        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "Exhibits packed",
                "Each podium, book, and trigger is now inside one Gallery_Exhibit_## object.\n\n" +
                "In the Hierarchy, select Gallery_Exhibit_01 (etc.) and move that — the podium, book, and trigger all move together.",
                "OK");
        }
    }
}
