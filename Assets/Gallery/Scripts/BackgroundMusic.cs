using UnityEngine;

/// <summary>
/// Plays looping gallery background music for the whole play session.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class BackgroundMusic : MonoBehaviour
{
    [Header("Music")]
    public AudioClip musicClip;
    [Range(0f, 1f)]
    public float volume = 0.4f;

    static BackgroundMusic _instance;
    AudioSource _source;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        _source = GetComponent<AudioSource>();
        _source.playOnAwake = false;
        _source.loop = true;
        _source.spatialBlend = 0f;
        _source.priority = 64;
        _source.volume = volume;

        if (musicClip != null)
            _source.clip = musicClip;

        // Start music immediately when Play mode begins
        if (_source.clip != null)
            _source.Play();
    }

    void OnValidate()
    {
        var source = GetComponent<AudioSource>();
        if (source == null) return;
        source.loop = true;
        source.spatialBlend = 0f;
        source.volume = volume;
    }
}
