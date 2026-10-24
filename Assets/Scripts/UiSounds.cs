using UnityEngine;

public class UiSounds : MonoBehaviour
{
    [SerializeField] AudioClip hover, click, confirm, back, open;

    static UiSounds instance;
    AudioSource source;

    void Awake()
    {
        instance = this;
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.ignoreListenerPause = true;
        source.spatialBlend = 0f;
    }

    static void Play(AudioClip clip, float volume)
    {
        if (instance == null || clip == null) return;
        instance.source.PlayOneShot(clip, volume * GameSettings.Sfx);
    }

    public static void Hover() => Play(instance != null ? instance.hover : null, 0.5f);
    public static void Click() => Play(instance != null ? instance.click : null, 0.8f);
    public static void Confirm() => Play(instance != null ? instance.confirm : null, 0.8f);
    public static void Back() => Play(instance != null ? instance.back : null, 0.8f);
    public static void Open() => Play(instance != null ? instance.open : null, 0.8f);

    public void Setup(AudioClip hoverClip, AudioClip clickClip, AudioClip confirmClip, AudioClip backClip, AudioClip openClip)
    {
        hover = hoverClip;
        click = clickClip;
        confirm = confirmClip;
        back = backClip;
        open = openClip;
    }
}
