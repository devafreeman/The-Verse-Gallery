using UnityEngine;
using UnityEngine.UI;
using StarterAssets;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// One canvas per podium. Shows that book's info, screenshots, and Visit Website button.
/// </summary>
public class BookInteractionUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject panelRoot;
    public Text titleText;
    public Text authorText;
    public Text descriptionText;
    public Text promptText;
    public Text urlPreviewText;
    public Button visitWebsiteButton;
    public Button closeButton;

    [Header("Screenshots")]
    public GameObject screenshotsSection;
    public RawImage[] screenshotSlots;

    [Header("Cursor")]
    public bool unlockCursorWhileOpen = true;

    GalleryBook _current;
    StarterAssetsInputs _inputs;
    bool _wired;

    void Awake()
    {
        EnsureScreenshotSlots();
        WireButtons();
        if (panelRoot != null)
            panelRoot.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    void OnEnable() => WireButtons();

    void EnsureScreenshotSlots()
    {
        try
        {
            Transform section = screenshotsSection != null
                ? screenshotsSection.transform
                : transform.Find("BookPanelRoot/Panel/ScreenshotsSection");

            if (section == null)
            {
                foreach (var t in GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == "ScreenshotsSection")
                    {
                        section = t;
                        screenshotsSection = t.gameObject;
                        break;
                    }
                }
            }

            if (section == null) return;

            var list = new System.Collections.Generic.List<RawImage>();
            for (int i = 0; i < section.childCount; i++)
            {
                var child = section.GetChild(i);
                if (child == null || !child.name.StartsWith("Screenshot_")) continue;

                // UI Image and RawImage cannot share one GameObject — remove Image first.
                var legacy = child.GetComponent<Image>();
                var raw = child.GetComponent<RawImage>();
                if (raw == null)
                {
                    Color c = new Color(0.2f, 0.18f, 0.15f, 0.85f);
                    if (legacy != null)
                    {
                        c = legacy.color;
                        DestroyImmediate(legacy);
                    }

                    raw = child.gameObject.AddComponent<RawImage>();
                    if (raw == null) continue;
                    raw.color = c;
                    raw.raycastTarget = false;
                }
                else if (legacy != null)
                {
                    DestroyImmediate(legacy);
                }

                list.Add(raw);
            }

            if (list.Count > 0)
                screenshotSlots = list.ToArray();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[BookInteractionUI] Screenshot slot setup skipped: {ex.Message}");
        }
    }

    void WireButtons()
    {
        if (_wired) return;
        _wired = true;

        if (visitWebsiteButton != null)
        {
            visitWebsiteButton.onClick.RemoveListener(OnVisitWebsiteClicked);
            visitWebsiteButton.onClick.AddListener(OnVisitWebsiteClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Hide);
            closeButton.onClick.AddListener(Hide);
        }
    }

    void Update()
    {
        if (_current == null) return;

        bool closePressed = false;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null &&
            (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.qKey.wasPressedThisFrame))
            closePressed = true;
#else
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Q))
            closePressed = true;
#endif
        if (closePressed)
            Hide();
    }

    public void Show(GalleryBook book)
    {
        if (book == null) return;
        try
        {
            EnsureScreenshotSlots();
        }
        catch { /* never block the book panel */ }

        WireButtons();
        _current = book;

        ApplyBookContent(book);

        gameObject.SetActive(true);
        if (panelRoot != null)
            panelRoot.SetActive(true);

        SetCursorForUi(true);
    }

    public void HideIfShowing(GalleryBook book)
    {
        if (_current == book)
            Hide();
    }

    public void Hide()
    {
        _current = null;
        if (panelRoot != null)
            panelRoot.SetActive(false);
        SetCursorForUi(false);
    }

    public void ApplyBookTexts(GalleryBook book) => ApplyBookContent(book);

    public void ApplyBookContent(GalleryBook book)
    {
        if (book == null) return;
        EnsureScreenshotSlots();

        if (titleText != null) titleText.text = book.bookTitle;
        if (authorText != null) authorText.text = book.bookAuthor;
        if (descriptionText != null) descriptionText.text = book.bookDescription;
        if (urlPreviewText != null)
            urlPreviewText.text = string.IsNullOrWhiteSpace(book.websiteUrl) ? "(no link set)" : book.websiteUrl;
        if (promptText != null)
            promptText.text = "Click Visit Website to open this page. Press Q or Esc to close.";
        if (visitWebsiteButton != null)
            visitWebsiteButton.interactable = !string.IsNullOrWhiteSpace(book.websiteUrl);

        ApplyScreenshots(book.screenshots);
    }

    void ApplyScreenshots(Texture2D[] shots)
    {
        EnsureScreenshotSlots();

        int needed = 0;
        if (shots != null)
        {
            for (int i = 0; i < shots.Length; i++)
            {
                if (shots[i] != null) needed++;
            }
        }

        // Grow UI slots if this book has more screenshots than the panel was built with
        if (screenshotsSection != null && needed > 0)
        {
            var existing = new System.Collections.Generic.List<RawImage>();
            if (screenshotSlots != null)
                existing.AddRange(screenshotSlots);

            while (existing.Count < needed)
            {
                int index = existing.Count;
                var go = new GameObject($"Screenshot_{index + 1}", typeof(RectTransform));
                go.transform.SetParent(screenshotsSection.transform, false);
                var raw = go.AddComponent<RawImage>();
                raw.raycastTarget = false;
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0f, 0f);
                rt.pivot = new Vector2(0f, 0f);
                float slotW = 160f;
                float gap = 10f;
                rt.anchoredPosition = new Vector2(index * (slotW + gap), 0f);
                rt.sizeDelta = new Vector2(slotW, 100f);
                existing.Add(raw);
            }

            screenshotSlots = existing.ToArray();
        }

        if (screenshotSlots == null || screenshotSlots.Length == 0)
        {
            if (screenshotsSection != null)
                screenshotsSection.SetActive(needed > 0);
            return;
        }

        int shotIndex = 0;
        for (int i = 0; i < screenshotSlots.Length; i++)
        {
            var slot = screenshotSlots[i];
            if (slot == null) continue;

            Texture2D tex = null;
            if (shots != null)
            {
                while (shotIndex < shots.Length && shots[shotIndex] == null)
                    shotIndex++;
                if (shotIndex < shots.Length)
                {
                    tex = shots[shotIndex];
                    shotIndex++;
                }
            }

            if (tex != null)
            {
                slot.texture = tex;
                slot.color = Color.white;
                slot.enabled = true;
                slot.gameObject.SetActive(true);
            }
            else
            {
                slot.texture = null;
                slot.gameObject.SetActive(false);
            }
        }

        if (screenshotsSection != null)
            screenshotsSection.SetActive(needed > 0);
    }

    void OnVisitWebsiteClicked()
    {
        _current?.OpenWebsite();
    }

    void SetCursorForUi(bool uiOpen)
    {
        if (!unlockCursorWhileOpen) return;

        if (_inputs == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                _inputs = player.GetComponent<StarterAssetsInputs>();
            if (_inputs == null)
                _inputs = Object.FindFirstObjectByType<StarterAssetsInputs>();
        }

        if (uiOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (_inputs != null)
            {
                _inputs.cursorLocked = false;
                _inputs.cursorInputForLook = false;
                _inputs.look = Vector2.zero;
            }
        }
        else
        {
            foreach (var ui in Object.FindObjectsByType<BookInteractionUI>(FindObjectsSortMode.None))
            {
                if (ui != this && ui._current != null)
                    return;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (_inputs != null)
            {
                _inputs.cursorLocked = true;
                _inputs.cursorInputForLook = true;
            }
        }
    }
}
