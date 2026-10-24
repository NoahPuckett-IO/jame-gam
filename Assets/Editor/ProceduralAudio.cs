using System.IO;
using UnityEditor;
using UnityEngine;

static class ProceduralAudio
{
    public static AudioClip NightAmbience()
    {
        const string path = "Assets/Audio/Intro/NightAmbience.wav";
        if (!File.Exists(path))
        {
            const int rate = 22050;
            const float seconds = 12f;
            int n = (int)(rate * seconds);
            var data = new float[n];
            var rng = new System.Random(7);
            float brown = 0f, lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float white = (float)rng.NextDouble() * 2f - 1f;
                brown = Mathf.Clamp(brown + white * 0.02f, -1f, 1f) * 0.998f;
                lp += (brown - lp) * 0.05f;
                float t = i / (float)rate;
                float gust = 0.55f + 0.45f * Mathf.Sin(t * Mathf.PI * 2f / seconds * 2f) * Mathf.Sin(t * 0.9f + 1.3f);
                data[i] = lp * 0.5f * gust;
            }
            void Cricket(float pitch, float every, float offset, float level)
            {
                for (float start = offset; start < seconds - 0.3f; start += every + (float)rng.NextDouble() * 0.25f)
                    for (int pulse = 0; pulse < 3; pulse++)
                    {
                        int s0 = (int)((start + pulse * 0.045f) * rate), len = (int)(0.028f * rate);
                        for (int k = 0; k < len && s0 + k < n; k++)
                        {
                            float env = Mathf.Sin(Mathf.PI * k / len);
                            data[s0 + k] += Mathf.Sin(2f * Mathf.PI * pitch * (s0 + k) / rate) * env * level;
                        }
                    }
            }
            Cricket(4300f, 0.72f, 0.1f, 0.12f);
            Cricket(3900f, 1.05f, 0.45f, 0.07f);
            Cricket(4700f, 1.6f, 0.8f, 0.05f);
            int fade = rate;
            for (int i = 0; i < fade; i++)
            {
                float k = i / (float)fade;
                data[i] = data[i] * k + data[n - fade + i] * (1f - k);
            }
            System.Array.Resize(ref data, n - fade);
            SoundSlicer.WriteWav(path, data, 1, rate);
            AssetDatabase.ImportAsset(path);
        }
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }
}
