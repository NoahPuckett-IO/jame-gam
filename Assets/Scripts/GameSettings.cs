using UnityEngine;
using UnityEngine.InputSystem;

public static class GameSettings
{
    const string BindingsKey = "bindings";
    static bool loaded;
    static float master = 1f, music = 0.8f, sfx = 1f, sensitivity = 1f;

    public static float Master { get { Load(); return master; } set { master = value; Save(); } }
    public static float Music { get { Load(); return music; } set { music = value; Save(); } }
    public static float Sfx { get { Load(); return sfx; } set { sfx = value; Save(); } }
    public static float Sensitivity { get { Load(); return sensitivity; } set { sensitivity = value; Save(); } }

    static void Load()
    {
        if (loaded) return;
        loaded = true;
        master = PlayerPrefs.GetFloat("vol_master", 1f);
        music = PlayerPrefs.GetFloat("vol_music", 0.8f);
        sfx = PlayerPrefs.GetFloat("vol_sfx", 1f);
        sensitivity = PlayerPrefs.GetFloat("mouse_sens", 1f);
        AudioListener.volume = master;
        var json = PlayerPrefs.GetString(BindingsKey, "");
        if (!string.IsNullOrEmpty(json) && InputSystem.actions != null) InputSystem.actions.LoadBindingOverridesFromJson(json);
    }

    static void Save()
    {
        AudioListener.volume = master;
        PlayerPrefs.SetFloat("vol_master", master);
        PlayerPrefs.SetFloat("vol_music", music);
        PlayerPrefs.SetFloat("vol_sfx", sfx);
        PlayerPrefs.SetFloat("mouse_sens", sensitivity);
        PlayerPrefs.Save();
    }

    public static void SaveBindings()
    {
        PlayerPrefs.SetString(BindingsKey, InputSystem.actions.SaveBindingOverridesAsJson());
        PlayerPrefs.Save();
    }

    public static void ResetBindings()
    {
        InputSystem.actions.RemoveAllBindingOverrides();
        PlayerPrefs.DeleteKey(BindingsKey);
        PlayerPrefs.Save();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        loaded = false;
        Load();
    }
}
