using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Moves existing Gallery_Podium_* (books + triggers stay as children) onto the
/// north and south edges of the scene Cube — five evenly spaced per wall.
/// Menu: Verse Gallery > Arrange Podiums On Cube Walls
/// </summary>
public static class ArrangePodiumsOnWalls
{
    const string ScenePath = "Assets/Scenes/my scene.unity";
    const string FlagPath = "Assets/Editor/RUN_ARRANGE_PODIUMS.flag";
    const string PodiumsRootName = "Gallery_Podiums";
    const string ExhibitsRootName = "Gallery_Exhibits";
    const string FloorCubeName = "Cube";
    const int PodiumCount = 10;
    const int PodiumsPerWall = 5;
    const float EdgeInset = 1.35f;
    const float EndMargin = 2.25f;

    [MenuItem("Verse Gallery/Arrange Podiums On Cube Walls")]
    public static void ArrangeMenu() => Arrange(showDialog: true);

    [InitializeOnLoadMethod]
    static void AutoRunFromFlag()
    {
        EditorApplication.delayCall += () =>
        {
            if (!System.IO.File.Exists(FlagPath)) return;
            try { System.IO.File.Delete(FlagPath); } catch { return; }
            Arrange(showDialog: true);
        };
    }

    public static void ArrangeBatch()
    {
        Arrange(showDialog: false);
        EditorApplication.Exit(0);
    }

    static void Arrange(bool showDialog)
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var exhibitsRoot = GameObject.Find(ExhibitsRootName);
        var podiumsRoot = GameObject.Find(PodiumsRootName);
        var root = exhibitsRoot != null ? exhibitsRoot : podiumsRoot;
        if (root == null)
        {
            Debug.LogError("[ArrangePodiumsOnWalls] Gallery_Exhibits / Gallery_Podiums not found.");
            if (showDialog)
                EditorUtility.DisplayDialog("Missing exhibits", "Gallery_Exhibits or Gallery_Podiums was not found in my scene.", "OK");
            return;
        }

        var cube = GameObject.Find(FloorCubeName);
        if (cube == null)
        {
            Debug.LogError("[ArrangePodiumsOnWalls] Scene Cube not found.");
            if (showDialog)
                EditorUtility.DisplayDialog("Missing Cube", "Could not find the Cube floor object in my scene.", "OK");
            return;
        }

        Bounds cubeBounds = GetObjectBounds(cube);
        var placements = GetCubeNorthSouthPlacements(cubeBounds);

        Undo.SetCurrentGroupName("Arrange Podiums On Cube Walls");
        int undo = Undo.GetCurrentGroup();

        Undo.RecordObject(root.transform, "Reset exhibits root");
        root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        root.transform.localScale = Vector3.one;

        for (int i = 0; i < PodiumCount; i++)
        {
            // Prefer exhibit container so podium + book + trigger move together
            var unit = GameObject.Find($"Gallery_Exhibit_{i + 1:00}");
            if (unit == null)
                unit = GameObject.Find($"Gallery_Podium_{i + 1:00}");

            if (unit == null)
            {
                Debug.LogWarning($"[ArrangePodiumsOnWalls] Missing exhibit/podium {i + 1:00}");
                continue;
            }

            Undo.RecordObject(unit.transform, "Move exhibit onto Cube wall");
            var (pos, facing) = placements[i];
            unit.transform.position = pos;
            unit.transform.rotation = Quaternion.LookRotation(facing);
            EditorUtility.SetDirty(unit.transform);
        }

        Undo.CollapseUndoOperations(undo);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = root;

        Debug.Log($"[ArrangePodiumsOnWalls] 5 exhibits on Cube north edge, 5 on south. Cube bounds={cubeBounds}");

        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "Podiums on Cube walls",
                "5 exhibits evenly spaced on the Cube's north edge and 5 on the south edge.\n\n" +
                "Each Gallery_Exhibit includes its podium, book, and trigger — move the exhibit to move them all.",
                "OK");
        }
    }

    public static (Vector3 pos, Vector3 facing)[] GetCubeNorthSouthPlacements(Bounds cubeBounds)
    {
        float floorY = cubeBounds.max.y;
        float xStart = cubeBounds.min.x + EndMargin;
        float xEnd = cubeBounds.max.x - EndMargin;
        float zNorth = cubeBounds.max.z - EdgeInset;
        float zSouth = cubeBounds.min.z + EdgeInset;

        var results = new (Vector3 pos, Vector3 facing)[PodiumCount];
        int index = 0;

        for (int i = 0; i < PodiumsPerWall; i++)
        {
            float t = PodiumsPerWall == 1 ? 0.5f : i / (float)(PodiumsPerWall - 1);
            float x = Mathf.Lerp(xStart, xEnd, t);
            results[index++] = (new Vector3(x, floorY, zNorth), Vector3.back);
        }

        for (int i = 0; i < PodiumsPerWall; i++)
        {
            float t = PodiumsPerWall == 1 ? 0.5f : i / (float)(PodiumsPerWall - 1);
            float x = Mathf.Lerp(xStart, xEnd, t);
            results[index++] = (new Vector3(x, floorY, zSouth), Vector3.forward);
        }

        return results;
    }

    static Bounds GetObjectBounds(GameObject go)
    {
        var renderer = go.GetComponent<Renderer>();
        if (renderer != null)
            return renderer.bounds;

        var col = go.GetComponent<Collider>();
        if (col != null)
            return col.bounds;

        // Fallback from transform (unit cube mesh)
        Vector3 pos = go.transform.position;
        Vector3 scale = go.transform.lossyScale;
        return new Bounds(pos, scale);
    }
}
