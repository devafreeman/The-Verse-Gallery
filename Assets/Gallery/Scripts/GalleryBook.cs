using UnityEngine;

/// <summary>
/// Invisible trigger at a podium. Shows that podium's own book canvas when entered.
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class GalleryBook : MonoBehaviour
{
    [Header("Book Info")]
    public string bookTitle = "Untitled Volume";
    [TextArea(2, 4)]
    public string bookAuthor = "Unknown Author";
    [TextArea(3, 8)]
    public string bookDescription = "An aged leather-bound volume from the gallery collection.";

    [Header("Website")]
    [Tooltip("Full URL opened when Visit Website is clicked on this podium's canvas")]
    public string websiteUrl = "https://www.gutenberg.org/";

    [Header("Screenshots")]
    [Tooltip("Any number of screenshots for this book (set size per book in the Inspector).")]
    public Texture2D[] screenshots = new Texture2D[4];

    [Header("This Podium's Canvas")]
    public BookInteractionUI bookCanvas;

    [Header("Trigger")]
    public float interactRadius = 2.6f;

    SphereCollider _trigger;

    void Reset() => EnsureTrigger();
    void OnValidate() => EnsureTrigger();
    void Awake() => EnsureTrigger();

    void EnsureTrigger()
    {
        _trigger = GetComponent<SphereCollider>();
        if (_trigger == null)
            _trigger = gameObject.AddComponent<SphereCollider>();

        _trigger.isTrigger = true;
        _trigger.radius = Mathf.Max(0.5f, interactRadius);
        _trigger.center = Vector3.zero;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other)) return;
        if (bookCanvas == null)
        {
            Debug.LogWarning($"[GalleryBook] No canvas assigned on '{name}' ({bookTitle}).");
            return;
        }
        bookCanvas.Show(this);
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other)) return;
        bookCanvas?.HideIfShowing(this);
    }

    static bool IsPlayer(Collider other)
    {
        return other.CompareTag("Player")
            || other.GetComponentInParent<StarterAssets.FirstPersonController>() != null;
    }

    public void OpenWebsite()
    {
        if (string.IsNullOrWhiteSpace(websiteUrl))
        {
            Debug.LogWarning($"[GalleryBook] No URL set on '{bookTitle}'.");
            return;
        }

        string url = websiteUrl.Trim();
        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            url = "https://" + url;

        Application.OpenURL(url);
    }
}
