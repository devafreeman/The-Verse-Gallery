using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Easy screenshot assignment with a variable number of shots per book.
/// </summary>
[CustomEditor(typeof(GalleryBook))]
public class GalleryBookEditor : Editor
{
    const string ScreenshotsFolder = "Assets/Gallery/Screenshots";
    const int MaxShots = 12;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("bookTitle"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("bookAuthor"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("bookDescription"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("websiteUrl"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("bookCanvas"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("interactRadius"));

        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("Screenshots", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Exit Play Mode first.\n" +
            "Change How Many Screenshots for this book, then Choose or drop images.\n" +
            "Each book can have a different number (0–12).",
            MessageType.Info);

        var book = (GalleryBook)target;
        if (book.screenshots == null)
            book.screenshots = new Texture2D[0];

        EditorGUI.BeginChangeCheck();
        int count = EditorGUILayout.IntSlider("How Many Screenshots", book.screenshots.Length, 0, MaxShots);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(book, "Resize Screenshots");
            Resize(book, count);
            EditorUtility.SetDirty(book);
        }

        Rect drop = GUILayoutUtility.GetRect(0, 72, GUILayout.ExpandWidth(true));
        GUI.Box(drop, "DROP SCREENSHOTS HERE\n(adds into empty slots, or grows the list)");
        HandleDrop(drop, book);

        EditorGUILayout.Space(6);

        for (int i = 0; i < book.screenshots.Length; i++)
        {
            EditorGUILayout.BeginHorizontal();
            book.screenshots[i] = (Texture2D)EditorGUILayout.ObjectField(
                $"Shot {i + 1}",
                book.screenshots[i],
                typeof(Texture2D),
                false);

            if (GUILayout.Button("Choose...", GUILayout.Width(80)))
            {
                string path = EditorUtility.OpenFilePanel(
                    $"Choose screenshot {i + 1}",
                    AbsoluteScreenshotsPath(),
                    "png,jpg,jpeg");
                if (!string.IsNullOrEmpty(path))
                {
                    var tex = ImportOrLoadTexture(path);
                    if (tex != null)
                    {
                        Undo.RecordObject(book, "Assign Screenshot");
                        book.screenshots[i] = tex;
                        EditorUtility.SetDirty(book);
                    }
                }
            }

            if (GUILayout.Button("Clear", GUILayout.Width(50)))
            {
                Undo.RecordObject(book, "Clear Screenshot");
                book.screenshots[i] = null;
                EditorUtility.SetDirty(book);
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Screenshot Slot") && book.screenshots.Length < MaxShots)
        {
            Undo.RecordObject(book, "Add Screenshot Slot");
            Resize(book, book.screenshots.Length + 1);
            EditorUtility.SetDirty(book);
        }
        if (GUILayout.Button("Remove Last Slot") && book.screenshots.Length > 0)
        {
            Undo.RecordObject(book, "Remove Screenshot Slot");
            Resize(book, book.screenshots.Length - 1);
            EditorUtility.SetDirty(book);
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Ping Screenshots Folder"))
        {
            EnsureScreenshotsFolder();
            var folder = AssetDatabase.LoadAssetAtPath<Object>(ScreenshotsFolder);
            if (folder != null)
                EditorGUIUtility.PingObject(folder);
        }

        serializedObject.ApplyModifiedProperties();
        if (GUI.changed)
            EditorUtility.SetDirty(book);
    }

    static void Resize(GalleryBook book, int newCount)
    {
        newCount = Mathf.Clamp(newCount, 0, MaxShots);
        var old = book.screenshots ?? new Texture2D[0];
        var next = new Texture2D[newCount];
        for (int i = 0; i < Mathf.Min(old.Length, newCount); i++)
            next[i] = old[i];
        book.screenshots = next;
    }

    void HandleDrop(Rect drop, GalleryBook book)
    {
        Event e = Event.current;
        if (!drop.Contains(e.mousePosition)) return;

        if (e.type == EventType.DragUpdated || e.type == EventType.DragPerform)
        {
            bool has = DragAndDrop.objectReferences.Length > 0 || DragAndDrop.paths.Length > 0;
            if (!has) return;

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (e.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                Undo.RecordObject(book, "Drop Screenshots");

                var textures = new List<Texture2D>();
                foreach (var obj in DragAndDrop.objectReferences)
                {
                    if (obj is Texture2D t) textures.Add(t);
                }

                foreach (var path in DragAndDrop.paths)
                {
                    if (string.IsNullOrEmpty(path)) continue;
                    if (path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase) ||
                        path.EndsWith(".jpg", System.StringComparison.OrdinalIgnoreCase) ||
                        path.EndsWith(".jpeg", System.StringComparison.OrdinalIgnoreCase))
                    {
                        var tex = ImportOrLoadTexture(path);
                        if (tex != null && !textures.Contains(tex))
                            textures.Add(tex);
                    }
                }

                var list = new List<Texture2D>(book.screenshots ?? new Texture2D[0]);
                foreach (var tex in textures)
                {
                    int empty = list.FindIndex(x => x == null);
                    if (empty >= 0)
                        list[empty] = tex;
                    else if (list.Count < MaxShots)
                        list.Add(tex);
                }
                book.screenshots = list.ToArray();
                EditorUtility.SetDirty(book);
                if (book.bookCanvas != null)
                    book.bookCanvas.ApplyBookContent(book);
            }
            e.Use();
        }
    }

    static string AbsoluteScreenshotsPath()
    {
        EnsureScreenshotsFolder();
        return Path.GetFullPath(ScreenshotsFolder);
    }

    static void EnsureScreenshotsFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Gallery"))
            AssetDatabase.CreateFolder("Assets", "Gallery");
        if (!AssetDatabase.IsValidFolder(ScreenshotsFolder))
            AssetDatabase.CreateFolder("Assets/Gallery", "Screenshots");
    }

    static Texture2D ImportOrLoadTexture(string absoluteOrAssetPath)
    {
        EnsureScreenshotsFolder();

        string assetPath;
        if (absoluteOrAssetPath.Replace('\\', '/').StartsWith("Assets/"))
        {
            assetPath = absoluteOrAssetPath.Replace('\\', '/');
        }
        else
        {
            string fileName = Path.GetFileName(absoluteOrAssetPath);
            assetPath = $"{ScreenshotsFolder}/{fileName}";
            string dest = Path.GetFullPath(assetPath);
            if (!File.Exists(dest))
            {
                File.Copy(absoluteOrAssetPath, dest, true);
                AssetDatabase.Refresh();
            }
        }

        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (tex == null)
        {
            AssetDatabase.ImportAsset(assetPath);
            tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }
        return tex;
    }
}
