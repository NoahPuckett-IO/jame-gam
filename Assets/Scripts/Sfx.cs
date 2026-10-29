using System.Collections.Generic;
using UnityEngine;

// Small one-shot and looping sound effects baked by SfxBaker into Resources/Sfx. Sounds play from a few pooled
// sources so nothing has to carry its own AudioSource, and they follow the sound effects volume.
public static class Sfx
{
    static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    static AudioSource[] pool;
    static int next;

    public static AudioClip Clip(string name)
    {
        if (!clips.TryGetValue(name, out var clip)) clips[name] = clip = Resources.Load<AudioClip>("Sfx/" + name);
        return clip;
    }

    // at == null plays flat, as for the player's own sounds; otherwise it's placed in the world
    public static void Play(string name, float volume = 1f, float pitch = 1f, Vector3? at = null)
    {
        var clip = Clip(name);
        if (clip == null) return;
        var s = Next();
        s.transform.position = at ?? Vector3.zero;
        s.spatialBlend = at.HasValue ? 0.85f : 0f;
        s.pitch = pitch * Random.Range(0.95f, 1.05f);
        s.clip = clip;
        s.volume = volume * GameSettings.Sfx;
        s.Play();
    }

    static AudioSource Next()
    {
        if (pool == null || pool[0] == null)
        {
            var go = new GameObject("Sfx");
            Object.DontDestroyOnLoad(go);
            pool = new AudioSource[10];
            for (int i = 0; i < pool.Length; i++)
            {
                var child = new GameObject("Voice " + i);
                child.transform.SetParent(go.transform);
                var s = pool[i] = child.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.rolloffMode = AudioRolloffMode.Linear;
                s.minDistance = 8f;
                s.maxDistance = 70f;
                s.dopplerLevel = 0f;
            }
        }
        // take a free voice if there is one, otherwise steal the oldest
        for (int i = 0; i < pool.Length; i++)
        {
            var s = pool[(next + i) % pool.Length];
            if (!s.isPlaying) { next = (next + i + 1) % pool.Length; return s; }
        }
        var oldest = pool[next];
        next = (next + 1) % pool.Length;
        return oldest;
    }

    // a looping source owned by whatever needs it (a zipline, the Roomba's motor), faded by the caller
    public static AudioSource Loop(GameObject owner, string name, float spatial)
    {
        var s = owner.AddComponent<AudioSource>();
        s.clip = Clip(name);
        s.loop = true;
        s.playOnAwake = false;
        s.volume = 0f;
        s.spatialBlend = spatial;
        s.rolloffMode = AudioRolloffMode.Linear;
        s.minDistance = 8f;
        s.maxDistance = 80f;
        s.dopplerLevel = 0f;
        return s;
    }

    // eases a loop toward a volume, starting and stopping it as needed
    public static void Fade(AudioSource s, float volume, float rate = 4f)
    {
        if (s == null || s.clip == null) return;
        float target = volume * GameSettings.Sfx;
        s.volume = Mathf.MoveTowards(s.volume, target, rate * Time.deltaTime);
        if (s.volume > 0.001f && !s.isPlaying) s.Play();
        else if (s.volume <= 0.001f && s.isPlaying) s.Stop();
    }
}
