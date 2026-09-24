using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Adds Starter Assets first-person player controls to my scene.
/// Menu: Verse Gallery > Add Starter Player To My Scene
/// </summary>
public static class PlaceStarterPlayer
{
    const string ScenePath = "Assets/Scenes/my scene.unity";
    const string FlagPath = "Assets/Editor/RUN_PLACE_PLAYER.flag";

    // Center of the gallery Cube floor
    static readonly Vector3 SpawnPosition = new Vector3(378.94f, -153.555f, -34.43f);

    [MenuItem("Verse Gallery/Add Starter Player To My Scene")]
    public static void AddPlayerMenu()
    {
        AddPlayer(showDialog: true);
    }

    [InitializeOnLoadMethod]
    static void AutoRunFromFlag()
    {
        EditorApplication.delayCall += () =>
        {
            if (!System.IO.File.Exists(FlagPath)) return;
            try { System.IO.File.Delete(FlagPath); } catch { return; }
            AddPlayer(showDialog: true);
        };
    }

    public static void AddPlayerBatch()
    {
        AddPlayer(showDialog: false);
        EditorApplication.Exit(0);
    }

    static void AddPlayer(bool showDialog)
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        DestroyIfExists("PlayerCapsule");
        DestroyIfExists("PlayerFollowCamera");
        DestroyIfExists("MainCamera");
        DestroyIfExists("UI_EventSystem");
        DestroyIfExists("NestedParent_Unpack");

        var oldCam = GameObject.Find("Main Camera");
        if (oldCam != null)
            Undo.DestroyObjectImmediate(oldCam);

        var playerPrefab = Load("Assets/StarterAssets/FirstPersonController/Prefabs/PlayerCapsule.prefab");
        var mainCamPrefab = Load("Assets/StarterAssets/FirstPersonController/Prefabs/MainCamera.prefab");
        var followPrefab = Load("Assets/StarterAssets/FirstPersonController/Prefabs/PlayerFollowCamera.prefab");
        var eventPrefab = Load("Assets/StarterAssets/Mobile/Prefabs/EventSystem/UI_EventSystem.prefab");

        if (playerPrefab == null || mainCamPrefab == null || followPrefab == null)
        {
            Debug.LogError("[PlaceStarterPlayer] Missing Starter Assets prefabs.");
            if (showDialog)
                EditorUtility.DisplayDialog("Missing prefabs", "Could not find Starter Assets First Person prefabs.", "OK");
            return;
        }

        var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        Undo.RegisterCreatedObjectUndo(player, "PlayerCapsule");
        player.name = "PlayerCapsule";
        player.transform.position = SpawnPosition;
        player.transform.rotation = Quaternion.identity;

        var mainCam = (GameObject)PrefabUtility.InstantiatePrefab(mainCamPrefab);
        Undo.RegisterCreatedObjectUndo(mainCam, "MainCamera");
        mainCam.name = "MainCamera";
        mainCam.transform.position = SpawnPosition + new Vector3(0f, 1.375f, 0f);

        var follow = (GameObject)PrefabUtility.InstantiatePrefab(followPrefab);
        Undo.RegisterCreatedObjectUndo(follow, "PlayerFollowCamera");
        follow.name = "PlayerFollowCamera";
        follow.transform.position = SpawnPosition + new Vector3(0f, 1.375f, 0f);

        if (eventPrefab != null)
        {
            var es = (GameObject)PrefabUtility.InstantiatePrefab(eventPrefab);
            Undo.RegisterCreatedObjectUndo(es, "UI_EventSystem");
            es.name = "UI_EventSystem";
        }

        var cameraRoot = FindChildNamed(player.transform, "PlayerCameraRoot");
        if (cameraRoot != null)
            WireFollowTarget(follow, cameraRoot);
        else
            Debug.LogWarning("[PlaceStarterPlayer] PlayerCameraRoot not found on PlayerCapsule.");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = player;

        Debug.Log("[PlaceStarterPlayer] PlayerCapsule added to my scene at " + SpawnPosition);

        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "Starter player ready",
                "PlayerCapsule is in my scene.\n\nPress Play, then use WASD to walk and mouse to look.\n\nSpawn is at the center of your gallery room.",
                "OK");
        }
    }

    static void WireFollowTarget(GameObject followCam, Transform target)
    {
        foreach (var comp in followCam.GetComponents<Component>())
        {
            if (comp == null) continue;
            var so = new SerializedObject(comp);

            // CM2 / deprecated VirtualCamera
            var followProp = so.FindProperty("m_Follow");
            if (followProp != null && followProp.propertyType == SerializedPropertyType.ObjectReference)
            {
                followProp.objectReferenceValue = target;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(comp);
                Debug.Log("[PlaceStarterPlayer] Wired Follow via " + comp.GetType().Name);
                return;
            }

            // CM3 Target.TrackingTarget nested property
            var targetProp = so.FindProperty("Target");
            if (targetProp != null)
            {
                var tracking = targetProp.FindPropertyRelative("TrackingTarget");
                if (tracking != null)
                {
                    tracking.objectReferenceValue = target;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(comp);
                    Debug.Log("[PlaceStarterPlayer] Wired CM3 TrackingTarget via " + comp.GetType().Name);
                    return;
                }
            }
        }

        Debug.LogWarning("[PlaceStarterPlayer] Could not wire PlayerFollowCamera Follow target.");
    }

    static Transform FindChildNamed(Transform root, string name)
    {
        if (root.name == name) return root;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == name) return t;
        }
        return null;
    }

    static GameObject Load(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(path);

    static void DestroyIfExists(string name)
    {
        var go = GameObject.Find(name);
        if (go != null)
            Undo.DestroyObjectImmediate(go);
    }
}
