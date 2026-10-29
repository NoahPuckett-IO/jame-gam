using System.IO;
using UnityEditor;
using UnityEngine;

// Synthesises the game's small sound effects into Assets/Resources/Sfx, where Sfx.Play loads them by name.
// Everything is made from sines, noise and envelopes, so there's nothing to license and they stay tiny.
static class SfxBaker
{
    const string Folder = "Assets/Resources/Sfx";
    const int Rate = 22050;

    [MenuItem("JameGam/Bake Sound Effects")]
    static void Bake()
    {
        Directory.CreateDirectory(Folder);
        var rng = new System.Random(11);
        float Noise() => (float)rng.NextDouble() * 2f - 1f;

        // pick up: a quick rising bloop
        Write("pickup", 0.16f, (t, k) => Sine(Sweep(t, 0.16f, 320f, 980f)) * Env(k, 0.04f, 3f) * 0.55f);

        // put down: a soft cardboard thud
        Write("drop", 0.22f, Thud(rng, 95f, 0.6f));

        // a heavy landing after a long fall
        Write("land", 0.26f, Thud(rng, 70f, 0.85f));

        // bounce pads: the first half of Ezra's boing recording, faded out at the cut
        TrimFirstHalf("Assets/Audio/Sfx/BoingSource.mp3", "boing");

        // dash: a swept breath of air
        float lp = 0f, bp = 0f;
        Write("whoosh", 0.34f, (t, k) =>
        {
            float cut = Mathf.Lerp(0.04f, 0.3f, Mathf.Sin(k * Mathf.PI));
            lp += (Noise() - lp) * cut;
            bp += (lp - bp) * 0.3f;
            return (lp - bp) * Mathf.Sin(k * Mathf.PI) * 1.4f;
        });


        // back at a checkpoint: a soft upward shimmer
        Write("respawn", 0.45f, (t, k) =>
        {
            float s = 0f;
            for (int i = 0; i < 4; i++) s += Sine(Sweep(t, 0.45f, 420f * (1 + i * 0.5f), 900f * (1 + i * 0.5f))) / (i + 1);
            return s * Mathf.Sin(k * Mathf.PI) * 0.22f;
        });

        // a hand or foot on a ladder rung
        float tap = 0f;
        Write("rung", 0.07f, (t, k) =>
        {
            tap += (Noise() - tap) * 0.5f;
            return (tap * 0.6f + Sine(t * 900f) * 0.5f) * Env(k, 0f, 9f) * 0.5f;
        });

        // the trolley wheel humming along a zipline wire, looped
        float hum = 0f;
        WriteLoop("zip", 1f, (t, k) =>
        {
            hum += (Noise() - hum) * 0.35f;
            float trem = 0.75f + 0.25f * Mathf.Sin(t * 2f * Mathf.PI * 14f);
            return (hum * 0.35f + Sine(t * 220f) * 0.12f + Sine(t * 440f) * 0.06f) * trem;
        });

        // lying on a tray, sliding: low rumbly scrape, looped
        float rum = 0f;
        WriteLoop("scrape", 1f, (t, k) =>
        {
            rum += (Noise() - rum) * 0.08f;
            return rum * (0.8f + 0.2f * Mathf.Sin(t * 2f * Mathf.PI * 9f)) * 1.2f;
        });

        // Roomba: electric zap
        Write("zap", 0.45f, (t, k) =>
        {
            float buzz = Mathf.Sign(Mathf.Sin(t * 2f * Mathf.PI * Mathf.Lerp(140f, 60f, k)));
            return (buzz * 0.4f + Noise() * 0.45f) * Env(k, 0.005f, 3.5f) * (0.6f + 0.4f * Mathf.Sin(t * 2f * Mathf.PI * 33f)) * 0.5f;
        });

        // Roomba: alarm two-tone as it wakes or switches mode
        Write("alarm", 0.6f, (t, k) => Square(t, t % 0.3f < 0.15f ? 880f : 660f) * 0.18f * Env(k, 0.01f, 0.6f));

        // Roomba: hitting a shelf at full speed
        Write("slam", 0.6f, (t, k) =>
            (Sine(Sweep(t, 0.6f, 90f, 40f)) * 0.8f + Noise() * Env(k, 0f, 12f) * 0.6f + Bell(t, 310f) * 0.25f) * Env(k, 0.002f, 4f) * 0.7f);

        // Roomba: powering down after the stomp
        Write("powerdown", 1.4f, (t, k) =>
        {
            float f = Mathf.Lerp(520f, 35f, Mathf.Pow(k, 0.6f));
            float sputter = k > 0.5f && Mathf.Repeat(t * 12f, 1f) < 0.3f ? 0.3f : 1f;
            return (Saw(t, f) * 0.35f + Sine(t * f * 0.5f) * 0.3f) * sputter * (1f - k) * 0.6f;
        });

        // Roomba: the motor and brushes, looped and pitched up with speed at runtime
        WriteLoop("motor", 1f, (t, k) =>
            (Saw(t, 55f) * 0.25f + Saw(t, 110.5f) * 0.12f + Sine(t * 330f) * 0.04f) * (0.85f + 0.15f * Mathf.Sin(t * 2f * Mathf.PI * 8f)));

        // stomp on the Roomba's button
        Write("stomp", 0.3f, (t, k) => (Sine(Sweep(t, 0.3f, 260f, 70f)) + Square(t, 140f) * 0.3f * Env(k, 0f, 18f)) * Env(k, 0.002f, 5f) * 0.6f);

        // a fanfare when the boss is beaten
        Write("victory", 1.3f, (t, k) =>
        {
            float[] notes = { 523.3f, 659.3f, 784f, 1046.5f };
            float s = 0f;
            for (int i = 0; i < notes.Length; i++)
            {
                float at = i * 0.12f;
                if (t < at) continue;
                float u = t - at, len = i == notes.Length - 1 ? 1.3f - at : 0.3f;
                if (u > len) continue;
                s += (Square(u, notes[i]) * 0.3f + Sine(u * notes[i]) * 0.5f) * Env(u / len, 0.01f, i == notes.Length - 1 ? 2f : 4f);
            }
            return s * 0.3f;
        });

        AssetDatabase.Refresh();

        // the soft particle the landing dust and sparkles use, a copy of the level three snow
        const string dustPath = "Assets/Resources/Fx/Dust.mat";
        var snow = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Snow.mat");
        if (snow != null && AssetDatabase.LoadAssetAtPath<Material>(dustPath) == null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(dustPath));
            var dust = new Material(snow);
            dust.SetColor("_BaseColor", Color.white);
            AssetDatabase.CreateAsset(dust, dustPath);
        }
        Debug.Log("[SfxBaker] baked sound effects into " + Folder);
    }

    // phase (in cycles) of a sine swept linearly from f0 to f1 over len seconds, at time t
    static float Sweep(float t, float len, float f0, float f1) => f0 * t + (f1 - f0) * t * t / (2f * len);
    static float Sine(float cycles) => Mathf.Sin(cycles * 2f * Mathf.PI);
    static float Square(float t, float f) => Mathf.Sign(Mathf.Sin(t * f * 2f * Mathf.PI));
    static float Saw(float t, float f) => Mathf.Repeat(t * f, 1f) * 2f - 1f;
    static float Bell(float t, float f) => Sine(t * f) + 0.4f * Sine(t * f * 2.76f) * Mathf.Exp(-t * 6f) + 0.2f * Sine(t * f * 5.4f) * Mathf.Exp(-t * 10f);

    // a quick attack, then an exponential tail; k runs 0..1 across the sound
    static float Env(float k, float attack, float decay) => (attack > 0f && k < attack ? k / attack : 1f) * Mathf.Exp(-k * decay);

    static System.Func<float, float, float> Thud(System.Random rng, float pitch, float level)
    {
        float lp = 0f;
        return (t, k) =>
        {
            lp += ((float)rng.NextDouble() * 2f - 1f - lp) * 0.12f;
            return (Sine(Sweep(t, 0.25f, pitch * 1.6f, pitch * 0.6f)) * 0.8f + lp * 1.2f) * Env(k, 0.003f, 7f) * level;
        };
    }

    static void Write(string name, float seconds, System.Func<float, float, float> f)
    {
        int n = Mathf.RoundToInt(seconds * Rate);
        var data = new float[n];
        for (int i = 0; i < n; i++) data[i] = f(i / (float)Rate, i / (float)n);
        // a few ms fade at the end so nothing clicks
        int edge = Mathf.Min(n / 4, 40);
        for (int i = 0; i < edge; i++) data[n - 1 - i] *= i / (float)edge;
        Save(name, data);
    }

    // loops are rendered a little long and crossfaded into themselves so they repeat seamlessly
    static void WriteLoop(string name, float seconds, System.Func<float, float, float> f)
    {
        int n = Mathf.RoundToInt(seconds * Rate), fade = Rate / 5;
        var data = new float[n + fade];
        for (int i = 0; i < data.Length; i++) data[i] = f(i / (float)Rate, i / (float)data.Length);
        for (int i = 0; i < fade; i++)
        {
            float k = i / (float)fade;
            data[i] = data[i] * k + data[n + i] * (1f - k);
        }
        System.Array.Resize(ref data, n);
        Save(name, data);
    }

    static void TrimFirstHalf(string source, string name)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(source);
        if (clip == null) return;
        if (AssetImporter.GetAtPath(source) is AudioImporter ai && ai.defaultSampleSettings.loadType != AudioClipLoadType.DecompressOnLoad)
        {
            var s = ai.defaultSampleSettings;
            s.loadType = AudioClipLoadType.DecompressOnLoad;
            ai.defaultSampleSettings = s;
            ai.SaveAndReimport();
            clip = AssetDatabase.LoadAssetAtPath<AudioClip>(source);
        }
        clip.LoadAudioData();
        int ch = clip.channels, frames = clip.samples / 2;
        var all = new float[clip.samples * ch];
        clip.GetData(all, 0);
        // down to mono, with a short fade so the cut doesn't click
        var data = new float[frames];
        int fade = Mathf.Min(frames / 4, clip.frequency / 20);
        for (int i = 0; i < frames; i++)
        {
            float v = 0f;
            for (int c = 0; c < ch; c++) v += all[i * ch + c];
            data[i] = v / ch * Mathf.Clamp01((frames - i) / (float)fade);
        }
        SoundSlicer.WriteWav($"{Folder}/{name}.wav", data, 1, clip.frequency);
    }

    static void Save(string name, float[] data)
    {
        float peak = 0f;
        foreach (var v in data) peak = Mathf.Max(peak, Mathf.Abs(v));
        if (peak > 0.95f) for (int i = 0; i < data.Length; i++) data[i] *= 0.95f / peak;
        SoundSlicer.WriteWav($"{Folder}/{name}.wav", data, 1, Rate);
    }
}
