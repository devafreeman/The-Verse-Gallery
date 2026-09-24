using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Each Gallery_Podium gets its own BookInfoTrigger + BookInfoCanvas
/// (title, description, screenshots, Visit Website).
/// Menu: Verse Gallery > Setup Podium Book Triggers And Canvas
/// </summary>
public static class SetupBookInteraction
{
    const string ScenePath = "Assets/Scenes/my scene.unity";
    const string FlagPath = "Assets/Editor/RUN_BOOK_UI.flag";
    const string TriggerChildName = "BookInfoTrigger";
    const string CanvasChildName = "BookInfoCanvas";
    const string BookUiRootName = "Gallery_BookUI";
    const int ScreenshotSlotCount = 4;

    struct BookData
    {
        public string title;
        public string author;
        public string description;
        public string url;
    }

    static readonly BookData[] Catalog =
    {
        new BookData {
            title = "Frankenstein",
            author = "Mary Shelley (1818)",
            description = "A landmark Gothic novel of creation, responsibility, and the loneliness of the outsider. Often called the first science-fiction novel.",
            url = "https://www.gutenberg.org/ebooks/84"
        },
        new BookData {
            title = "Pride and Prejudice",
            author = "Jane Austen (1813)",
            description = "A witty classic of manners, misunderstanding, and romance among the English gentry.",
            url = "https://www.gutenberg.org/ebooks/1342"
        },
        new BookData {
            title = "Moby-Dick",
            author = "Herman Melville (1851)",
            description = "An epic sea-hunt that becomes a meditation on obsession, nature, and fate aboard the Pequod.",
            url = "https://www.gutenberg.org/ebooks/2701"
        },
        new BookData {
            title = "Dracula",
            author = "Bram Stoker (1897)",
            description = "The definitive vampire novel — letters, journals, and dread as Count Dracula enters Victorian England.",
            url = "https://www.gutenberg.org/ebooks/345"
        },
        new BookData {
            title = "The Odyssey",
            author = "Homer (tr. Butler)",
            description = "Odysseus's long voyage home after Troy — hospitality, cunning, and the pull of homecoming.",
            url = "https://www.gutenberg.org/ebooks/1727"
        },
        new BookData {
            title = "Jane Eyre",
            author = "Charlotte Brontë (1847)",
            description = "A fierce coming-of-age story of independence, morality, and love at Thornfield Hall.",
            url = "https://www.gutenberg.org/ebooks/1260"
        },
        new BookData {
            title = "The Picture of Dorian Gray",
            author = "Oscar Wilde (1890)",
            description = "A Faustian tale of beauty, corruption, and a portrait that bears every sin.",
            url = "https://www.gutenberg.org/ebooks/174"
        },
        new BookData {
            title = "Alice's Adventures in Wonderland",
            author = "Lewis Carroll (1865)",
            description = "A dream-logic classic of riddles, nonsense, and curiosity down the rabbit-hole.",
            url = "https://www.gutenberg.org/ebooks/11"
        },
        new BookData {
            title = "Crime and Punishment",
            author = "Fyodor Dostoevsky",
            description = "A psychological study of guilt and redemption after a desperate crime in St. Petersburg.",
            url = "https://www.gutenberg.org/ebooks/2554"
        },
        new BookData {
            title = "The Strange Case of Dr Jekyll and Mr Hyde",
            author = "Robert Louis Stevenson (1886)",
            description = "A compact thriller of dual identity, repression, and the beast within.",
            url = "https://www.gutenberg.org/ebooks/43"
        },
    };

    [MenuItem("Verse Gallery/Setup Podium Book Triggers And Canvas")]
    public static void SetupMenu() => Setup(showDialog: true);

    [InitializeOnLoadMethod]
    static void AutoRunFromFlag()
    {
        EditorApplication.delayCall += TryRunFromFlag;
        EditorApplication.update += PollFlag;
    }

    static double _nextPoll;

    static void PollFlag()
    {
        if (EditorApplication.timeSinceStartup < _nextPoll) return;
        _nextPoll = EditorApplication.timeSinceStartup + 1.5;
        TryRunFromFlag();
    }

    static void TryRunFromFlag()
    {
        if (!System.IO.File.Exists(FlagPath)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        try { System.IO.File.Delete(FlagPath); } catch { return; }
        EditorApplication.update -= PollFlag;
        Setup(showDialog: true);
    }

    public static void SetupBatch()
    {
        Setup(showDialog: false);
        EditorApplication.Exit(0);
    }

    static void Setup(bool showDialog)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Gallery/Screenshots"))
            AssetDatabase.CreateFolder("Assets/Gallery", "Screenshots");

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (t.name == CanvasChildName && t.parent == null)
                Object.DestroyImmediate(t.gameObject);
        }

        EnsureEventSystem();
        int count = WirePodiums();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log($"[SetupBookInteraction] {count} podiums updated with title, description, URL, and screenshot slots.");
        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "Book panels updated",
                $"Each of {count} podiums now shows title, description, screenshots, and Visit Website.\n\n" +
                "To add pictures:\n" +
                "To add pictures:\n" +
                "1. Put PNG/JPG files in Assets/Gallery/Screenshots\n" +
                "2. Select BookInfoTrigger (exit Play Mode first)\n" +
                "3. Set Screenshots Size to 4\n" +
                "4. Drag images from Project into Element 0–3\n" +
                "   Or click the circle next to Element and pick the image\n\n" +
                "Your existing titles / URLs are kept.",
                "OK");
        }
    }

    static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        Undo.RegisterCreatedObjectUndo(es, "EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    static int WirePodiums()
    {
        int count = 0;
        for (int i = 1; i <= 10; i++)
        {
            var podium = FindByName($"Gallery_Podium_{i:00}");
            if (podium == null) continue;

            var data = Catalog[(i - 1) % Catalog.Length];
            var trigger = EnsureTrigger(podium.transform, data);
            var ui = EnsurePodiumCanvas(podium.transform, trigger, i);

            trigger.bookCanvas = ui;
            if (trigger.screenshots == null || trigger.screenshots.Length == 0)
                trigger.screenshots = new Texture2D[ScreenshotSlotCount];

            ui.ApplyBookContent(trigger);
            EditorUtility.SetDirty(trigger);
            EditorUtility.SetDirty(ui);
            count++;
        }
        return count;
    }

    static GalleryBook EnsureTrigger(Transform podium, BookData data)
    {
        Transform existing = podium.Find(TriggerChildName);
        GameObject triggerGo;
        bool brandNew = false;
        if (existing != null)
            triggerGo = existing.gameObject;
        else
        {
            triggerGo = new GameObject(TriggerChildName);
            Undo.RegisterCreatedObjectUndo(triggerGo, TriggerChildName);
            triggerGo.transform.SetParent(podium, false);
            brandNew = true;
        }

        triggerGo.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        triggerGo.transform.localRotation = Quaternion.identity;
        triggerGo.transform.localScale = Vector3.one;

        var book = triggerGo.GetComponent<GalleryBook>();
        if (book == null)
        {
            book = Undo.AddComponent<GalleryBook>(triggerGo);
            brandNew = true;
        }

        // Only fill sample catalog text when this trigger is brand new
        if (brandNew || string.IsNullOrWhiteSpace(book.bookTitle) || book.bookTitle == "Untitled Volume")
        {
            book.bookTitle = data.title;
            book.bookAuthor = data.author;
            book.bookDescription = data.description;
            book.websiteUrl = data.url;
        }

        book.interactRadius = 2.6f;
        if (book.screenshots == null || book.screenshots.Length == 0)
            book.screenshots = new Texture2D[ScreenshotSlotCount];

        return book;
    }

    static BookInteractionUI EnsurePodiumCanvas(Transform podium, GalleryBook book, int index)
    {
        // Screen-space canvases must NOT live under exhibits — their bounds pull
        // the move gizmo into the sky when Unity tool handle mode is Center.
        var uiRoot = EnsureBookUiRoot();

        Transform oldOnPodium = podium.Find(CanvasChildName);
        if (oldOnPodium != null)
            Object.DestroyImmediate(oldOnPodium.gameObject);

        Transform oldUnderUi = uiRoot.Find($"{CanvasChildName}_{index:00}");
        if (oldUnderUi != null)
            Object.DestroyImmediate(oldUnderUi.gameObject);

        var canvasGo = new GameObject($"{CanvasChildName}_{index:00}");
        Undo.RegisterCreatedObjectUndo(canvasGo, CanvasChildName);
        canvasGo.transform.SetParent(uiRoot, false);
        canvasGo.transform.localPosition = Vector3.zero;
        canvasGo.transform.localRotation = Quaternion.identity;
        canvasGo.transform.localScale = Vector3.one;

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100 + index;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        var root = CreateUiObject("BookPanelRoot", canvasGo.transform);

        var backdrop = CreateUiObject("Backdrop", root.transform);
        var backdropImg = backdrop.AddComponent<Image>();
        backdropImg.color = new Color(0f, 0f, 0f, 0.45f);
        StretchFull(backdrop.GetComponent<RectTransform>());

        var panel = CreateUiObject("Panel", root.transform);
        var panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.12f, 0.1f, 0.08f, 0.94f);
        var panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(980, 640);
        panelRt.anchoredPosition = Vector2.zero;

        string titleStr = book != null ? book.bookTitle : "";
        string authorStr = book != null ? book.bookAuthor : "";
        string descStr = book != null ? book.bookDescription : "";
        string urlStr = book != null ? book.websiteUrl : "";

        var title = CreateText("Title", panel.transform, 34, FontStyle.Bold, TextAnchor.UpperLeft);
        title.text = titleStr;
        SetRect(title, 24, -20, -24, 48);

        var author = CreateText("Author", panel.transform, 20, FontStyle.Italic, TextAnchor.UpperLeft);
        author.text = authorStr;
        SetRect(author, 24, -68, -24, 32);
        author.color = new Color(0.85f, 0.75f, 0.55f);

        var description = CreateText("Description", panel.transform, 18, FontStyle.Normal, TextAnchor.UpperLeft);
        description.text = descStr;
        SetRect(description, 24, -108, -24, 110);
        description.horizontalOverflow = HorizontalWrapMode.Wrap;
        description.verticalOverflow = VerticalWrapMode.Truncate;

        // Screenshots section
        var shotsSection = CreateUiObject("ScreenshotsSection", panel.transform);
        var shotsRt = shotsSection.GetComponent<RectTransform>();
        shotsRt.anchorMin = new Vector2(0f, 1f);
        shotsRt.anchorMax = new Vector2(1f, 1f);
        shotsRt.pivot = new Vector2(0.5f, 1f);
        shotsRt.offsetMin = new Vector2(24, -360);
        shotsRt.offsetMax = new Vector2(-24, -230);

        var shotsLabel = CreateText("ScreenshotsLabel", shotsSection.transform, 16, FontStyle.Normal, TextAnchor.UpperLeft);
        shotsLabel.text = "Screenshots";
        SetRect(shotsLabel, 0, 0, 0, 24);
        shotsLabel.color = new Color(0.7f, 0.7f, 0.7f);

        var slots = new RawImage[ScreenshotSlotCount];
        float slotW = 210f;
        float gap = 14f;
        float startX = 0f;
        for (int s = 0; s < ScreenshotSlotCount; s++)
        {
            var slotGo = CreateUiObject($"Screenshot_{s + 1}", shotsSection.transform);
            var img = slotGo.AddComponent<RawImage>();
            img.color = new Color(0.2f, 0.18f, 0.15f, 0.85f);
            img.raycastTarget = false;
            var rt = slotGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(startX + s * (slotW + gap), 0f);
            rt.sizeDelta = new Vector2(slotW, 118f);
            slots[s] = img;
        }

        var urlLabel = CreateText("UrlLabel", panel.transform, 16, FontStyle.Normal, TextAnchor.UpperLeft);
        urlLabel.text = "Website";
        SetRect(urlLabel, 24, -380, -24, 24);
        urlLabel.color = new Color(0.7f, 0.7f, 0.7f);

        var urlPreview = CreateText("UrlPreview", panel.transform, 15, FontStyle.Normal, TextAnchor.UpperLeft);
        urlPreview.text = urlStr;
        SetRect(urlPreview, 24, -404, -24, 36);
        urlPreview.color = new Color(0.55f, 0.75f, 1f);
        urlPreview.horizontalOverflow = HorizontalWrapMode.Wrap;

        var prompt = CreateText("Prompt", panel.transform, 15, FontStyle.Normal, TextAnchor.LowerLeft);
        prompt.text = "Click Visit Website to open this page. Press Q or Esc to close.";
        SetRect(prompt, 24, 70, -24, 36);
        prompt.color = new Color(0.75f, 0.75f, 0.75f);

        var visitBtn = CreateButton("VisitWebsiteButton", panel.transform, "Visit Website",
            new Vector2(24, 24), new Vector2(260, 52), new Color(0.45f, 0.28f, 0.14f, 1f));
        var closeBtn = CreateButton("CloseButton", panel.transform, "Close",
            new Vector2(300, 24), new Vector2(140, 52), new Color(0.25f, 0.25f, 0.25f, 1f));

        var ui = canvasGo.AddComponent<BookInteractionUI>();
        ui.panelRoot = root;
        ui.titleText = title;
        ui.authorText = author;
        ui.descriptionText = description;
        ui.promptText = prompt;
        ui.urlPreviewText = urlPreview;
        ui.visitWebsiteButton = visitBtn.GetComponent<Button>();
        ui.closeButton = closeBtn.GetComponent<Button>();
        ui.screenshotsSection = shotsSection;
        ui.screenshotSlots = slots;
        root.SetActive(false);

        return ui;
    }

    static Transform EnsureBookUiRoot()
    {
        var existing = GameObject.Find(BookUiRootName);
        if (existing != null)
            return existing.transform;

        var go = new GameObject(BookUiRootName);
        Undo.RegisterCreatedObjectUndo(go, BookUiRootName);
        go.transform.position = Vector3.zero;
        return go.transform;
    }

    static GameObject FindByName(string name)
    {
        var go = GameObject.Find(name);
        if (go != null) return go;
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (t.name == name) return t.gameObject;
        }
        return null;
    }

    static GameObject CreateUiObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static Text CreateText(string name, Transform parent, int size, FontStyle style, TextAnchor anchor)
    {
        var go = CreateUiObject(name, parent);
        var text = go.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (text.font == null)
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = anchor;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    static GameObject CreateButton(string name, Transform parent, string label, Vector2 anchoredPos, Vector2 size, Color color)
    {
        var go = CreateUiObject(name, parent);
        var img = go.AddComponent<Image>();
        img.color = color;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        var textGo = CreateUiObject("Label", go.transform);
        var text = textGo.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (text.font == null)
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.text = label;
        text.fontSize = 22;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;
        StretchFull(textGo.GetComponent<RectTransform>());
        return go;
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void SetRect(Text text, float left, float top, float right, float height)
    {
        var rt = text.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(left, -Mathf.Abs(top) - height);
        rt.offsetMax = new Vector2(right, -Mathf.Abs(top));
    }
}
