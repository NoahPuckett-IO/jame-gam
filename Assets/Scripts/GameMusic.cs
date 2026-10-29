using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class GameMusic : MonoBehaviour
{
    [SerializeField] float volume = 0.45f;
    [SerializeField] float fadeSeconds = 1.2f;

    public static bool KeepPlaying;
    // below 1 while a short cutscene wants the music turned down rather than off
    public static float Duck = 1f;

    AudioSource source;
    PlayerController player;
    IntroCutscene intro;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.loop = true;
        source.playOnAwake = false;
        source.volume = 0f;
        Duck = 1f;
    }

    void Start()
    {
        player = FindAnyObjectByType<PlayerController>();
        intro = FindAnyObjectByType<IntroCutscene>();
    }

    void Update()
    {
        bool ducked = Duck < 1f;
        bool playing = KeepPlaying || ducked || player != null && player.enabled && !player.InCutscene && !player.CameraLocked
                       && (intro == null || !intro.Running);
        // the credits roll with it a bit louder; a short camera move only dips it, quickly
        float level = KeepPlaying ? Mathf.Min(1f, volume * 1.6f) : volume * Duck;
        float target = playing ? level * GameSettings.Music : 0f;
        float rate = ducked ? volume / 0.35f : volume / fadeSeconds;
        source.volume = Mathf.MoveTowards(source.volume, target, rate * Time.unscaledDeltaTime);

        if (playing && !source.isPlaying)
        {
            if (source.time > 0f) source.UnPause();
            else source.Play();
        }
        else if (!playing && source.isPlaying && source.volume <= 0.001f)
            source.Pause();
    }
}
