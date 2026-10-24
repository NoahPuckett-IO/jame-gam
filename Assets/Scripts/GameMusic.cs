using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class GameMusic : MonoBehaviour
{
    [SerializeField] float volume = 0.45f;
    [SerializeField] float fadeSeconds = 1.2f;

    public static bool KeepPlaying;

    AudioSource source;
    PlayerController player;
    IntroCutscene intro;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.loop = true;
        source.playOnAwake = false;
        source.volume = 0f;
    }

    void Start()
    {
        player = FindAnyObjectByType<PlayerController>();
        intro = FindAnyObjectByType<IntroCutscene>();
    }

    void Update()
    {
        bool playing = KeepPlaying || player != null && player.enabled && !player.InCutscene && !player.CameraLocked
                       && (intro == null || !intro.Running);
        float target = playing ? volume : 0f;
        source.volume = Mathf.MoveTowards(source.volume, target, volume / fadeSeconds * Time.unscaledDeltaTime);

        if (playing && !source.isPlaying)
        {
            if (source.time > 0f) source.UnPause();
            else source.Play();
        }
        else if (!playing && source.isPlaying && source.volume <= 0.001f)
            source.Pause();
    }
}
