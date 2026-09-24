using UnityEngine;

/// <summary>
/// Slowly rotates the active skybox while the game is playing.
/// Uses a runtime material instance so the asset on disk is not modified.
/// </summary>
public class SkyboxRotator : MonoBehaviour
{
    [Tooltip("Degrees per second the skybox spins")]
    public float degreesPerSecond = 2.5f;

    [Tooltip("If true, rotation continues while the game is paused (unscaled time)")]
    public bool useUnscaledTime;

    Material _runtimeSky;
    float _rotation;

    void Start()
    {
        var source = RenderSettings.skybox;
        if (source == null)
        {
            Debug.LogWarning("[SkyboxRotator] No skybox assigned in Lighting settings.");
            enabled = false;
            return;
        }

        // Instance so we don't permanently dirty Sky_Noon.mat
        _runtimeSky = new Material(source);
        _runtimeSky.name = source.name + " (Runtime)";
        RenderSettings.skybox = _runtimeSky;

        if (_runtimeSky.HasProperty("_Rotation"))
            _rotation = _runtimeSky.GetFloat("_Rotation");
    }

    void Update()
    {
        if (_runtimeSky == null || !_runtimeSky.HasProperty("_Rotation"))
            return;

        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        _rotation += degreesPerSecond * dt;
        if (_rotation > 360f || _rotation < -360f)
            _rotation %= 360f;

        _runtimeSky.SetFloat("_Rotation", _rotation);
    }

    void OnDestroy()
    {
        if (_runtimeSky != null)
            Destroy(_runtimeSky);
    }
}
