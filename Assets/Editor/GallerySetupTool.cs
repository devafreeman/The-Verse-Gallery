using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Places 10 gallery podiums around existing scene structure,
/// adds glass windows for sky views, and sets a blue cloudy sky.
/// Menu: Verse Gallery > Place Podiums And Windows
/// </summary>
public static class GallerySetupTool
{
    const string PodiumsRootName = "Gallery_Podiums";
    const string WindowsRootName = "Gallery_Windows";
    const int PodiumCount = 10;
    const int PodiumsPerWall = 5;

    static readonly string[] SkipNames =
    {
        "Main Camera", "Directional Light", "Global Volume",
        PodiumsRootName, WindowsRootName, "Gallery_SkySetup",
        "Light_01", "Fan_01"
    };

    [MenuItem("Verse Gallery/Place Podiums And Windows")]
    public static void PlacePodiumsAndWindows()
    {
        PlaceInActiveScene(showDialogs: true);
    }

    /// <summary>
    /// Batch / CLI entry: opens my scene, places podiums + windows, saves, exits.
    /// Unity.exe -batchmode -projectPath "..." -executeMethod GallerySetupTool.PlaceAndSaveBatch
    /// </summary>
    public static void PlaceAndSaveBatch()
    {
        string scenePath = "Assets/Scenes/my scene.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        Debug.Log($"[GallerySetup] Opened {scenePath}");

        PlaceInActiveScene(showDialogs: false);

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[GallerySetup] Saved scene with podiums and windows.");
        EditorApplication.Exit(0);
    }

    /// <summary>
    /// Drop Assets/Editor/RUN_GALLERY_SETUP.flag while Unity is open to auto-place once.
    /// </summary>
    [InitializeOnLoadMethod]
    static void AutoRunFromFlag()
    {
        EditorApplication.delayCall += () =>
        {
            const string flag = "Assets/Editor/RUN_GALLERY_SETUP.flag";
            if (!System.IO.File.Exists(flag)) return;

            try { System.IO.File.Delete(flag); }
            catch { return; }

            string scenePath = "Assets/Scenes/my scene.unity";
            var active = EditorSceneManager.GetActiveScene();
            if (active.path != scenePath)
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            PlaceInActiveScene(showDialogs: false);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("[GallerySetup] Auto-placed 10 podiums + windows and saved the scene.");
            EditorUtility.DisplayDialog(
                "Gallery setup done",
                "Placed 10 podiums around your modular room and added glass windows with a blue cloudy sky.\n\nScene saved.",
                "OK");
        };
    }

    static void PlaceInActiveScene(bool showDialogs)
    {
        var bounds = GetStructureBounds(out int structureCount);
        if (structureCount == 0)
        {
            if (showDialogs)
            {
                bool proceed = EditorUtility.DisplayDialog(
                    "No structure found",
                    "The open scene only has camera/light (or your build isn't saved yet).\n\n" +
                    "Click Save Scene (Ctrl+S) if you built something, then run this again.\n\n" +
                    "Or click Create to place a starter gallery floor, 10 podiums, and windows at the origin.",
                    "Create starter gallery",
                    "Cancel");
                if (!proceed) return;
            }
            else
            {
                Debug.LogWarning("[GallerySetup] No structure renderers found; creating starter floor.");
            }
            bounds = CreateStarterFloor();
        }
        else
        {
            Debug.Log($"[GallerySetup] Structure pieces found: {structureCount}. Bounds center={bounds.center} size={bounds.size}");
        }

        Undo.SetCurrentGroupName("Place Gallery Podiums And Windows");
        int undo = Undo.GetCurrentGroup();

        ClearOld(PodiumsRootName);
        ClearOld(WindowsRootName);

        var cube = GameObject.Find("Cube");
        Bounds podiumBounds;
        bool useCube = cube != null;
        if (useCube)
        {
            var rend = cube.GetComponent<Renderer>();
            podiumBounds = rend != null ? rend.bounds : new Bounds(cube.transform.position, cube.transform.lossyScale);
            Debug.Log($"[GallerySetup] Using Cube bounds {podiumBounds} for wall podiums.");
        }
        else
        {
            podiumBounds = bounds;
            float insetX = Mathf.Clamp(bounds.size.x * 0.08f, 1.2f, 3.5f);
            float insetZ = Mathf.Clamp(bounds.size.z * 0.08f, 1.2f, 3.5f);
            podiumBounds.Expand(new Vector3(-insetX * 2f, 0f, -insetZ * 2f));
            if (podiumBounds.size.x < 4f || podiumBounds.size.z < 4f)
                podiumBounds = bounds;
        }

        PlacePodiums(podiumBounds, useCube);
        PlaceWindows(bounds);
        SetupSkyAndSun();

        // Move camera near the room so the gallery is visible on open
        var cam = Object.FindFirstObjectByType<Camera>();
        if (cam != null)
        {
            Undo.RecordObject(cam.transform, "Frame Gallery");
            Vector3 camPos = bounds.center + new Vector3(0f, Mathf.Max(2f, bounds.extents.y * 0.4f), -Mathf.Max(8f, bounds.extents.z * 0.55f));
            cam.transform.position = camPos;
            cam.transform.LookAt(bounds.center + Vector3.up * 1.2f);
        }

        Undo.CollapseUndoOperations(undo);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        var podiums = GameObject.Find(PodiumsRootName);
        if (podiums != null)
            Selection.activeGameObject = podiums;

        if (showDialogs)
        {
            EditorUtility.DisplayDialog(
                "Gallery setup done",
                $"Placed {PodiumCount} podiums (5 on each of the two longest walls) and added glass windows.\n\n" +
                "Skybox set to blue sky with soft clouds. Save the scene (Ctrl+S).",
                "OK");
        }
    }

    static Bounds GetStructureBounds(out int count)
    {
        count = 0;
        var renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        Bounds? combined = null;

        foreach (var r in renderers)
        {
            if (r == null || !r.enabled) continue;
            if (ShouldSkip(r.gameObject)) continue;
            if (combined == null) combined = r.bounds;
            else
            {
                var b = combined.Value;
                b.Encapsulate(r.bounds);
                combined = b;
            }
            count++;
        }

        // Terrains
        foreach (var t in Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None))
        {
            if (t == null || ShouldSkip(t.gameObject)) continue;
            var size = t.terrainData != null ? t.terrainData.size : new Vector3(50, 0, 50);
            var tb = new Bounds(t.transform.position + size * 0.5f, size);
            if (combined == null) combined = tb;
            else
            {
                var b = combined.Value;
                b.Encapsulate(tb);
                combined = b;
            }
            count++;
        }

        return combined ?? new Bounds(Vector3.zero, new Vector3(12, 3, 12));
    }

    static bool ShouldSkip(GameObject go)
    {
        Transform t = go.transform;
        while (t != null)
        {
            foreach (var name in SkipNames)
            {
                if (t.name == name || t.name.StartsWith("Gallery_Podium_"))
                    return true;
            }
            if (t.GetComponent<Camera>() || t.GetComponent<Light>())
                return true;
            t = t.parent;
        }
        return false;
    }

    static Bounds CreateStarterFloor()
    {
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(floor, "Starter Floor");
        floor.name = "Gallery_Floor";
        floor.transform.position = new Vector3(0f, -0.05f, 0f);
        floor.transform.localScale = new Vector3(20f, 0.1f, 20f);

        var mat = LoadOrCreatePodiumMaterial();
        floor.GetComponent<Renderer>().sharedMaterial = mat;

        // Simple perimeter walls with gaps for windows
        CreateWall("Wall_North", new Vector3(0, 1.5f, 10f), new Vector3(20f, 3f, 0.2f));
        CreateWall("Wall_South", new Vector3(0, 1.5f, -10f), new Vector3(20f, 3f, 0.2f));
        CreateWall("Wall_East", new Vector3(10f, 1.5f, 0f), new Vector3(0.2f, 3f, 20f));
        CreateWall("Wall_West", new Vector3(-10f, 1.5f, 0f), new Vector3(0.2f, 3f, 20f));

        return new Bounds(Vector3.zero, new Vector3(20f, 3f, 20f));
    }

    static void CreateWall(string name, Vector3 pos, Vector3 scale)
    {
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(wall, name);
        wall.name = name;
        wall.transform.position = pos;
        wall.transform.localScale = scale;
        wall.GetComponent<Renderer>().sharedMaterial = LoadOrCreatePodiumMaterial();
    }

    static void ClearOld(string rootName)
    {
        var existing = GameObject.Find(rootName);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing);
    }

    static void PlacePodiums(Bounds bounds, bool useCubeEdges = false)
    {
        var root = new GameObject(PodiumsRootName);
        Undo.RegisterCreatedObjectUndo(root, "Podiums Root");

        Material podiumMat = LoadOrCreatePodiumMaterial();
        var placements = useCubeEdges
            ? ArrangePodiumsOnWalls.GetCubeNorthSouthPlacements(bounds)
            : GetLongWallPodiumPlacements(bounds);

        for (int i = 0; i < placements.Length; i++)
        {
            var (pos, facing) = placements[i];
            var podium = BuildPodium($"Gallery_Podium_{i + 1:00}", pos, podiumMat);
            podium.transform.SetParent(root.transform, true);
            podium.transform.rotation = Quaternion.LookRotation(facing);
        }
    }

    /// <summary>
    /// Five podiums on each of the two longest walls, evenly spaced, facing inward.
    /// Books / triggers parented to a podium move with it.
    /// </summary>
    public static (Vector3 pos, Vector3 facing)[] GetLongWallPodiumPlacements(Bounds bounds)
    {
        float floorY = bounds.min.y;
        bool xIsLonger = bounds.size.x >= bounds.size.z;

        var results = new (Vector3 pos, Vector3 facing)[PodiumCount];
        int index = 0;

        if (xIsLonger)
        {
            // Long walls are North (+Z) and South (-Z)
            float xStart = bounds.min.x;
            float xEnd = bounds.max.x;
            float zNorth = bounds.max.z;
            float zSouth = bounds.min.z;

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
        }
        else
        {
            // Long walls are East (+X) and West (-X)
            float zStart = bounds.min.z;
            float zEnd = bounds.max.z;
            float xEast = bounds.max.x;
            float xWest = bounds.min.x;

            for (int i = 0; i < PodiumsPerWall; i++)
            {
                float t = PodiumsPerWall == 1 ? 0.5f : i / (float)(PodiumsPerWall - 1);
                float z = Mathf.Lerp(zStart, zEnd, t);
                results[index++] = (new Vector3(xEast, floorY, z), Vector3.left);
            }

            for (int i = 0; i < PodiumsPerWall; i++)
            {
                float t = PodiumsPerWall == 1 ? 0.5f : i / (float)(PodiumsPerWall - 1);
                float z = Mathf.Lerp(zStart, zEnd, t);
                results[index++] = (new Vector3(xWest, floorY, z), Vector3.right);
            }
        }

        return results;
    }

    static GameObject BuildPodium(string name, Vector3 position, Material mat)
    {
        var root = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(root, name);
        root.transform.position = position;

        // Base plinth
        var baseBlock = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseBlock.name = "Base";
        baseBlock.transform.SetParent(root.transform, false);
        baseBlock.transform.localPosition = new Vector3(0f, 0.15f, 0f);
        baseBlock.transform.localScale = new Vector3(1.1f, 0.3f, 1.1f);
        baseBlock.GetComponent<Renderer>().sharedMaterial = mat;

        // Column / stem
        var stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stem.name = "Stem";
        stem.transform.SetParent(root.transform, false);
        stem.transform.localPosition = new Vector3(0f, 0.55f, 0f);
        stem.transform.localScale = new Vector3(0.45f, 0.35f, 0.45f);
        stem.GetComponent<Renderer>().sharedMaterial = mat;

        // Top display plate
        var top = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        top.name = "Top";
        top.transform.SetParent(root.transform, false);
        top.transform.localPosition = new Vector3(0f, 0.95f, 0f);
        top.transform.localScale = new Vector3(0.95f, 0.08f, 0.95f);
        top.GetComponent<Renderer>().sharedMaterial = mat;

        return root;
    }

    static void PlaceWindows(Bounds bounds)
    {
        var root = new GameObject(WindowsRootName);
        Undo.RegisterCreatedObjectUndo(root, "Windows Root");

        Material glass = LoadGlassMaterial();
        Material frameMat = LoadOrCreatePodiumMaterial();

        Vector3 c = bounds.center;
        float y = Mathf.Lerp(bounds.min.y, bounds.max.y, 0.55f);
        float height = Mathf.Clamp(bounds.size.y * 0.45f, 1.6f, 3.5f);
        float inset = 0.15f;

        // Four walls — two wide windows each (8 total)
        var specs = new List<(Vector3 pos, Vector3 scale, Vector3 euler)>
        {
            // North
            (new Vector3(c.x - bounds.extents.x * 0.35f, y, bounds.max.z + inset),
                new Vector3(bounds.size.x * 0.28f, height, 0.08f), Vector3.zero),
            (new Vector3(c.x + bounds.extents.x * 0.35f, y, bounds.max.z + inset),
                new Vector3(bounds.size.x * 0.28f, height, 0.08f), Vector3.zero),
            // South
            (new Vector3(c.x - bounds.extents.x * 0.35f, y, bounds.min.z - inset),
                new Vector3(bounds.size.x * 0.28f, height, 0.08f), Vector3.zero),
            (new Vector3(c.x + bounds.extents.x * 0.35f, y, bounds.min.z - inset),
                new Vector3(bounds.size.x * 0.28f, height, 0.08f), Vector3.zero),
            // East
            (new Vector3(bounds.max.x + inset, y, c.z - bounds.extents.z * 0.35f),
                new Vector3(0.08f, height, bounds.size.z * 0.28f), Vector3.zero),
            (new Vector3(bounds.max.x + inset, y, c.z + bounds.extents.z * 0.35f),
                new Vector3(0.08f, height, bounds.size.z * 0.28f), Vector3.zero),
            // West
            (new Vector3(bounds.min.x - inset, y, c.z - bounds.extents.z * 0.35f),
                new Vector3(0.08f, height, bounds.size.z * 0.28f), Vector3.zero),
            (new Vector3(bounds.min.x - inset, y, c.z + bounds.extents.z * 0.35f),
                new Vector3(0.08f, height, bounds.size.z * 0.28f), Vector3.zero),
        };

        for (int i = 0; i < specs.Count; i++)
        {
            var (pos, scale, euler) = specs[i];
            CreateWindowPane(root.transform, $"Window_{i + 1:00}", pos, scale, euler, glass, frameMat);
        }

        TryPlaceArchOpenings(root.transform, bounds);
        OpenWallCutsForWindows(bounds, root.transform, glass);
    }

    static void TryPlaceArchOpenings(Transform parent, Bounds bounds)
    {
        var archPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Barking_Dog/3D Free Modular Kit/Prefabs/Door_Arch_01.prefab");
        if (archPrefab == null) return;

        Vector3 c = bounds.center;
        float y = bounds.min.y;
        float pad = 0.05f;

        var placements = new (Vector3 pos, float yRot)[]
        {
            (new Vector3(c.x, y, bounds.max.z + pad), 0f),
            (new Vector3(c.x, y, bounds.min.z - pad), 180f),
            (new Vector3(bounds.max.x + pad, y, c.z), 90f),
            (new Vector3(bounds.min.x - pad, y, c.z), -90f),
        };

        for (int i = 0; i < placements.Length; i++)
        {
            var (pos, yRot) = placements[i];
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(archPrefab);
            Undo.RegisterCreatedObjectUndo(instance, "Window Arch");
            instance.name = $"WindowArch_{i + 1:00}";
            instance.transform.SetParent(parent, true);
            instance.transform.position = pos;
            instance.transform.rotation = Quaternion.Euler(0f, yRot, 0f);
        }
    }

    static void CreateWindowPane(Transform parent, string name, Vector3 pos, Vector3 scale, Vector3 euler,
        Material glass, Material frameMat)
    {
        var window = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(window, name);
        window.transform.SetParent(parent, true);
        window.transform.position = pos;
        window.transform.eulerAngles = euler;

        // Glass
        var glassGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        glassGo.name = "Glass";
        glassGo.transform.SetParent(window.transform, false);
        glassGo.transform.localPosition = Vector3.zero;
        glassGo.transform.localScale = scale;
        Object.DestroyImmediate(glassGo.GetComponent<Collider>());
        glassGo.GetComponent<Renderer>().sharedMaterial = glass;

        // Thin frame border (slightly larger, darker)
        var frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
        frame.name = "Frame";
        frame.transform.SetParent(window.transform, false);
        frame.transform.localPosition = Vector3.zero;
        Vector3 frameScale = scale;
        if (scale.x >= scale.z)
            frameScale = new Vector3(scale.x + 0.12f, scale.y + 0.12f, Mathf.Max(0.04f, scale.z * 0.5f));
        else
            frameScale = new Vector3(Mathf.Max(0.04f, scale.x * 0.5f), scale.y + 0.12f, scale.z + 0.12f);
        frame.transform.localScale = frameScale;
        // Put frame behind glass slightly for a border look via second rim cubes
        Object.DestroyImmediate(frame);
        AddFrameRim(window.transform, scale, frameMat);
    }

    static void AddFrameRim(Transform parent, Vector3 glassScale, Material frameMat)
    {
        bool northSouth = glassScale.x >= glassScale.z;
        float thickness = 0.06f;
        float depth = 0.1f;

        void Rim(string n, Vector3 localPos, Vector3 localScale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = n;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            go.GetComponent<Renderer>().sharedMaterial = frameMat;
        }

        if (northSouth)
        {
            float w = glassScale.x;
            float h = glassScale.y;
            Rim("Frame_Top", new Vector3(0, h * 0.5f + thickness * 0.5f, 0), new Vector3(w + thickness * 2f, thickness, depth));
            Rim("Frame_Bottom", new Vector3(0, -h * 0.5f - thickness * 0.5f, 0), new Vector3(w + thickness * 2f, thickness, depth));
            Rim("Frame_Left", new Vector3(-w * 0.5f - thickness * 0.5f, 0, 0), new Vector3(thickness, h, depth));
            Rim("Frame_Right", new Vector3(w * 0.5f + thickness * 0.5f, 0, 0), new Vector3(thickness, h, depth));
        }
        else
        {
            float d = glassScale.z;
            float h = glassScale.y;
            Rim("Frame_Top", new Vector3(0, h * 0.5f + thickness * 0.5f, 0), new Vector3(depth, thickness, d + thickness * 2f));
            Rim("Frame_Bottom", new Vector3(0, -h * 0.5f - thickness * 0.5f, 0), new Vector3(depth, thickness, d + thickness * 2f));
            Rim("Frame_Left", new Vector3(0, 0, -d * 0.5f - thickness * 0.5f), new Vector3(depth, h, thickness));
            Rim("Frame_Right", new Vector3(0, 0, d * 0.5f + thickness * 0.5f), new Vector3(depth, h, thickness));
        }
    }

    static void OpenWallCutsForWindows(Bounds bounds, Transform windowsRoot, Material glass)
    {
        // For solid cube walls named Wall_*, carve visibility by shrinking
        // overlapping wall mass into open window slots (disable mesh in cut region via hole cubes removed).
        // Practical approach: if Wall_* exist, punch holes by replacing mid sections with empty space.
        string[] wallNames = { "Wall_North", "Wall_South", "Wall_East", "Wall_West" };
        foreach (var wallName in wallNames)
        {
            var wall = GameObject.Find(wallName);
            if (wall == null) continue;

            // Split wall into side pillars leaving center open for sky
            var t = wall.transform;
            Vector3 pos = t.position;
            Vector3 scale = t.localScale;
            Material mat = wall.GetComponent<Renderer>()?.sharedMaterial;

            Undo.DestroyObjectImmediate(wall);

            bool northSouth = wallName.Contains("North") || wallName.Contains("South");
            if (northSouth)
            {
                float fullW = scale.x;
                float pillarW = fullW * 0.18f;
                float z = pos.z;
                CreateWallPiece($"{wallName}_L", new Vector3(pos.x - fullW * 0.35f, pos.y, z),
                    new Vector3(pillarW, scale.y, scale.z), mat);
                CreateWallPiece($"{wallName}_R", new Vector3(pos.x + fullW * 0.35f, pos.y, z),
                    new Vector3(pillarW, scale.y, scale.z), mat);
                // Low sill under windows
                CreateWallPiece($"{wallName}_Sill", new Vector3(pos.x, bounds.min.y + 0.4f, z),
                    new Vector3(fullW * 0.55f, 0.8f, scale.z), mat);
            }
            else
            {
                float fullD = scale.z;
                float pillarD = fullD * 0.18f;
                float x = pos.x;
                CreateWallPiece($"{wallName}_L", new Vector3(x, pos.y, pos.z - fullD * 0.35f),
                    new Vector3(scale.x, scale.y, pillarD), mat);
                CreateWallPiece($"{wallName}_R", new Vector3(x, pos.y, pos.z + fullD * 0.35f),
                    new Vector3(scale.x, scale.y, pillarD), mat);
                CreateWallPiece($"{wallName}_Sill", new Vector3(x, bounds.min.y + 0.4f, pos.z),
                    new Vector3(scale.x, 0.8f, fullD * 0.55f), mat);
            }
        }
    }

    static void CreateWallPiece(string name, Vector3 pos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(go, name);
        go.name = name;
        go.transform.position = pos;
        go.transform.localScale = scale;
        if (mat != null)
            go.GetComponent<Renderer>().sharedMaterial = mat;
    }

    static void SetupSkyAndSun()
    {
        // Prefer SC Skyboxes Noon; fall back to gallery / default skies
        var sky = AssetDatabase.LoadAssetAtPath<Material>(
            "Packages/xyz.staggartcreations.skyboxes/Skyboxes/Sky_Noon.mat");
        if (sky == null)
            sky = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Gallery/Materials/GallerySky_BlueClouds.mat");
        if (sky == null)
            sky = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");
        if (sky == null)
            sky = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/StarterAssets/Environment/Art/Skybox/SkyboxLite.mat");

        RenderSettings.skybox = sky;
        // Lock ambient so swapping skyboxes doesn't restyle gallery shadows/fill
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.212f, 0.227f, 0.259f);
        RenderSettings.ambientEquatorColor = new Color(0.114f, 0.125f, 0.133f);
        RenderSettings.ambientGroundColor = new Color(0.047f, 0.043f, 0.035f);
        RenderSettings.ambientIntensity = 1f;
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
        RenderSettings.reflectionIntensity = 1.2f;

        var sun = Object.FindFirstObjectByType<Light>();
        if (sun != null && sun.type == LightType.Directional)
        {
            Undo.RecordObject(sun, "Tune Sun");
            sun.color = new Color(1f, 0.96f, 0.88f);
            sun.intensity = 1.35f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(42f, -35f, 0f);
            RenderSettings.sun = sun;
        }

        // Ensure cameras clear to skybox
        foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            Undo.RecordObject(cam, "Camera Skybox");
            cam.clearFlags = CameraClearFlags.Skybox;
        }

        DynamicGI.UpdateEnvironment();
    }

    static Material LoadGlassMaterial()
    {
        var glass = AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/Barking_Dog/3D Free Modular Kit/Meshes/Materials/Glass.mat");
        if (glass != null) return glass;

        string path = "Assets/Gallery/Materials/WindowGlass.mat";
        glass = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (glass != null) return glass;

        glass = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        glass.name = "WindowGlass";
        glass.SetFloat("_Surface", 1f); // Transparent
        glass.SetFloat("_Blend", 0f);
        glass.SetColor("_BaseColor", new Color(0.75f, 0.9f, 1f, 0.25f));
        glass.SetFloat("_Smoothness", 0.9f);
        glass.SetFloat("_Metallic", 0f);
        glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        glass.renderQueue = (int)RenderQueue.Transparent;
        AssetDatabase.CreateAsset(glass, path);
        return glass;
    }

    static Material LoadOrCreatePodiumMaterial()
    {
        var granite = AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/Gallery/Materials/WhiteGranite.mat");
        if (granite != null) return granite;

        string path = "Assets/Gallery/Materials/Podium.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;

        mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.name = "Podium";
        mat.SetColor("_BaseColor", new Color(0.96f, 0.95f, 0.93f));
        mat.SetFloat("_Smoothness", 0.72f);
        mat.SetFloat("_Metallic", 0.05f);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }
}
