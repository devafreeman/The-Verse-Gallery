using System.Collections;
using UnityEngine;

/// <summary>
/// Plays a voice-over clip once after a short delay when the game starts,
/// on top of existing background music (separate AudioSource).
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class VoiceOverIntro : MonoBehaviour
{
    [Header("Voice Over")]
    public AudioClip voiceClip;
    [Range(0f, 1f)]
    public float volume = 1f;
    [Tooltip("Seconds to wait after Play starts before the voice-over begins")]
    public float delaySeconds = 2f;

    AudioSource _source;

    void Awake()
    {
        _source = GetComponent<AudioSource>();
        _source.playOnAwake = false;
        _source.loop = false;
        _source.spatialBlend = 0f;
        _source.volume = volume;
    }

    void Start()
    {
        if (voiceClip == null)
        {
            Debug.LogWarning("[VoiceOverIntro] No voice clip assigned.");
            return;
        }

        StartCoroutine(PlayAfterDelay());
    }

    IEnumerator PlayAfterDelay()
    {
        yield return new WaitForSeconds(delaySeconds);
        _source.clip = voiceClip;
        _source.volume = volume;
        _source.Play();
    }
}
