using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Hangs screenshots as wall posters behind gallery exhibits/podiums.
/// Menu: Verse Gallery > Place All Requested Wall Posters
/// Flag file: each line "podiumIndex|screenshotPath" (or BATCH then those lines)
/// </summary>
public static class PlaceWallPoster
{
    const string ScenePath = "Assets/Scenes/my scene.unity";
    const string FlagPath = "Assets/Editor/RUN_PLACE_POSTER.flag";
    const string MaterialsFolder = "Assets/Gallery/Materials/Posters";
    const string PosterChildName = "WallPoster";
    const string CubeName = "Cube";

    static readonly (int podium, string shot)[] DefaultJobs =
    {
        (1, "Assets/Gallery/Screenshots/Screenshot 2026-08-17 134834.png"),
        (2, "Assets/Gallery/Screenshots/Screenshot 2026-08-17 140115.png"),
        (3, "Assets/Gallery/Screenshots/Screenshot 2026-08-17 132923.png"),
        (4, "Assets/Gallery/Screenshots/Screenshot 2026-08-17 133531.png"),
        (5, "Assets/Gallery/Screenshots/Screenshot 2026-08-17 133957.png"),
        (6, "Assets/Gallery/Screenshots/Screenshot 2026-08-17 134324.png"),
        (7, "Assets/Gallery/Screenshots/Screenshot 2026-08-17 134736.png"),
        (8, "Assets/Gallery/Screenshots/Screenshot 2026-08-17 135322.png"),
        (9, "Assets/Gallery/Screenshots/Screenshot 2026-08-17 135608.png"),
        (10, "Assets/Gallery/Screenshots/Screenshot 2026-08-17 135653.png"),
    };

    [MenuItem("Verse Gallery/Place All Requested Wall Posters")]
    public static void PlaceMenu() => PlaceMany(DefaultJobs, showDialog: true);

    [InitializeOnLoadMethod]
    static void AutoRunFromFlag()
    {
        EditorApplication.delayCall += () =>
        {
            if (!System.IO.File.Exists(FlagPath)) return;

            var jobs = new List<(int, string)>();
            try
            {
                string[] lines = System.IO.File.ReadAllLines(FlagPath);
                System.IO.File.Delete(FlagPath);
                foreach (string raw in lines)
                {
                    string line = raw.Trim();
                    if (string.IsNullOrEmpty(line) || line.StartsWith("#") ||
                        line.Equals("BATCH", System.StringComparison.OrdinalIgnoreCase))
                        continue;

                    int sep = line.IndexOf('|');
                    if (sep > 0 &&
                        int.TryParse(line.Substring(0, sep).Trim(), out int podium) &&
                        !string.IsNullOrWhiteSpace(line.Substring(sep + 1)))
                    {
                        jobs.Add((podium, line.Substring(sep + 1).Trim()));
                    }
                }
            }
            catch { return; }

            if (jobs.Count == 0)
                PlaceMany(DefaultJobs, showDialog: true);
            else
                PlaceMany(jobs.ToArray(), showDialog: true);
        };
    }

    public static void PlaceBatch()
    {
        PlaceMany(DefaultJobs, showDialog: false);
        EditorApplication.Exit(0);
    }

    static void PlaceMany((int podium, string shot)[] jobs, bool showDialog)
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        int ok = 0;
        var missing = new List<string>();

        foreach (var (podium, shot) in jobs)
        {
            if (PlaceOne(podium, shot))
                ok++;
            else
                missing.Add($"Podium {podium}: {shot}");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log($"[PlaceWallPoster] Placed {ok}/{jobs.Length} posters.");

        if (showDialog)
        {
            string extra = missing.Count == 0
                ? ""
                : "\n\nSkipped:\n" + string.Join("\n", missing);
            EditorUtility.DisplayDialog(
                "Posters placed",
                $"Hung {ok} wall posters behind the podiums.{extra}",
                "OK");
        }
    }

    static bool PlaceOne(int podiumIndex, string screenshotPath)
    {
        var exhibit = FindExhibitOrPodium(podiumIndex);
        if (exhibit == null)
        {
            Debug.LogError($"[PlaceWallPoster] Missing Gallery_Exhibit_{podiumIndex:00}");
            return false;
        }

        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(screenshotPath);
        if (texture == null)
        {
            string[] guids = AssetDatabase.FindAssets(
                System.IO.Path.GetFileNameWithoutExtension(screenshotPath) + " t:Texture2D");
            if (guids != null && guids.Length > 0)
            {
                screenshotPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                texture = AssetDatabase.LoadAssetAtPath<Texture2D>(screenshotPath);
            }
        }

        if (texture == null)
        {
            Debug.LogError($"[PlaceWallPoster] Screenshot not found: {screenshotPath}");
            return false;
        }

        ConfigureTextureImport(screenshotPath);
        var mat = CreateOrUpdatePosterMaterial(podiumIndex, texture);

        Transform existing = exhibit.transform.Find(PosterChildName);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing.gameObject);

        float aspect = texture.height > 0 ? (float)texture.width / texture.height : 1.6f;
        float height = 1.35f;
        float width = height * aspect;

        var poster = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Undo.RegisterCreatedObjectUndo(poster, "Wall Poster");
        poster.name = PosterChildName;
        Object.DestroyImmediate(poster.GetComponent<MeshCollider>());
        Undo.SetTransformParent(poster.transform, exhibit.transform, "Parent poster");

        poster.transform.localPosition = ComputeBehindWallLocalPosition(exhibit.transform);
        poster.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        poster.transform.localScale = new Vector3(width, height, 1f);
        poster.GetComponent<MeshRenderer>().sharedMaterial = mat;

        var frame = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Undo.RegisterCreatedObjectUndo(frame, "Poster Frame");
        frame.name = "Frame";
        Object.DestroyImmediate(frame.GetComponent<MeshCollider>());
        frame.transform.SetParent(poster.transform, false);
        frame.transform.localPosition = new Vector3(0f, 0f, 0.025f);
        frame.transform.localRotation = Quaternion.identity;
        frame.transform.localScale = new Vector3(1.1f, 1.1f, 1f);
        frame.GetComponent<MeshRenderer>().sharedMaterial = LoadOrCreateFrameMaterial();

        Debug.Log($"[PlaceWallPoster] Podium {podiumIndex:00} <- {screenshotPath}");
        return true;
    }

    static GameObject FindExhibitOrPodium(int index)
    {
        var exhibit = GameObject.Find($"Gallery_Exhibit_{index:00}");
        if (exhibit != null) return exhibit;
        return GameObject.Find($"Gallery_Podium_{index:00}");
    }

    static Vector3 ComputeBehindWallLocalPosition(Transform exhibit)
    {
        float eyeY = 1.55f;
        float fallbackDepth = 2.2f;

        var cube = GameObject.Find(CubeName);
        if (cube == null)
            return new Vector3(0f, eyeY, -fallbackDepth);

        Bounds b = cube.GetComponent<Renderer>() != null
            ? cube.GetComponent<Renderer>().bounds
            : new Bounds(cube.transform.position, cube.transform.lossyScale);

        Vector3 back = -exhibit.forward;
        back.y = 0f;
        if (back.sqrMagnitude < 0.001f) back = Vector3.back;
        back.Normalize();

        Vector3 origin = exhibit.position + Vector3.up * eyeY;
        float dist = fallbackDepth;

        if (Mathf.Abs(back.z) >= Mathf.Abs(back.x))
        {
            float wallZ = back.z > 0f ? b.max.z : b.min.z;
            dist = Mathf.Abs(wallZ - origin.z) - 0.06f;
        }
        else
        {
            float wallX = back.x > 0f ? b.max.x : b.min.x;
            dist = Mathf.Abs(wallX - origin.x) - 0.06f;
        }

        dist = Mathf.Clamp(dist, 0.8f, 12f);
        Vector3 local = exhibit.InverseTransformPoint(origin + back * dist);
        local.x = 0f;
        local.y = eyeY;
        return local;
    }

    static void ConfigureTextureImport(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;

        bool dirty = false;
        if (importer.textureType != TextureImporterType.Default)
        {
            importer.textureType = TextureImporterType.Default;
            dirty = true;
        }
        if (importer.mipmapEnabled)
        {
            importer.mipmapEnabled = false;
            dirty = true;
        }
        if (!importer.sRGBTexture)
        {
            importer.sRGBTexture = true;
            dirty = true;
        }
        if (dirty)
            importer.SaveAndReimport();
    }

    static Material CreateOrUpdatePosterMaterial(int podiumIndex, Texture2D texture)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Gallery/Materials"))
            AssetDatabase.CreateFolder("Assets/Gallery", "Materials");
        if (!AssetDatabase.IsValidFolder(MaterialsFolder))
            AssetDatabase.CreateFolder("Assets/Gallery/Materials", "Posters");

        string matPath = $"{MaterialsFolder}/Poster_Podium_{podiumIndex:00}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Texture");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");

        if (mat == null)
        {
            mat = new Material(shader);
            mat.name = $"Poster_Podium_{podiumIndex:00}";
            AssetDatabase.CreateAsset(mat, matPath);
        }
        else
        {
            mat.shader = shader;
        }

        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", texture);
        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", texture);

        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
        return mat;
    }

    static Material LoadOrCreateFrameMaterial()
    {
        string path = $"{MaterialsFolder}/PosterFrame.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;

        if (!AssetDatabase.IsValidFolder(MaterialsFolder))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Gallery/Materials"))
                AssetDatabase.CreateFolder("Assets/Gallery", "Materials");
            AssetDatabase.CreateFolder("Assets/Gallery/Materials", "Posters");
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        mat = new Material(shader);
        mat.name = "PosterFrame";
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", new Color(0.08f, 0.07f, 0.06f, 1f));
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", new Color(0.08f, 0.07f, 0.06f, 1f));
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }
}
