using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

static class SoundSlicer
{
    public static AudioClip[] Slice(string sourcePath, string outFolder, string prefix, float threshold, float minGap,
        float minLength, int maxCount)
    {
        if (!(AssetImporter.GetAtPath(sourcePath) is AudioImporter importer)) return new AudioClip[0];
        var s = importer.defaultSampleSettings;
        if (s.loadType != AudioClipLoadType.DecompressOnLoad)
        {
            s.loadType = AudioClipLoadType.DecompressOnLoad;
            importer.defaultSampleSettings = s;
            importer.SaveAndReimport();
        }
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(sourcePath);
        if (clip == null || !clip.LoadAudioData()) return new AudioClip[0];

        int ch = clip.channels, rate = clip.frequency, frames = clip.samples;
        var data = new float[frames * ch];
        clip.GetData(data, 0);

        int win = Mathf.Max(1, rate / 100);
        int windows = frames / win;
        var env = new float[windows];
        float peak = 0f;
        for (int w = 0; w < windows; w++)
        {
            double sum = 0;
            for (int i = w * win; i < (w + 1) * win; i++)
                for (int c = 0; c < ch; c++) { float v = data[i * ch + c]; sum += v * v; }
            env[w] = Mathf.Sqrt((float)(sum / (win * ch)));
            peak = Mathf.Max(peak, env[w]);
        }

        var segments = new List<Vector2Int>();
        float cut = peak * threshold;
        int gapWindows = Mathf.RoundToInt(minGap * 100f);
        int start = -1, lastLoud = -1;
        for (int w = 0; w < windows; w++)
        {
            if (env[w] >= cut)
            {
                if (start < 0) start = w;
                lastLoud = w;
            }
            else if (start >= 0 && w - lastLoud > gapWindows)
            {
                segments.Add(new Vector2Int(start, lastLoud));
                start = -1;
            }
        }
        if (start >= 0) segments.Add(new Vector2Int(start, lastLoud));
        segments.RemoveAll(seg => (seg.y - seg.x + 1) / 100f < minLength);

        Directory.CreateDirectory(outFolder);
        for (int k = Mathf.Min(segments.Count, maxCount); k < 64; k++)
            AssetDatabase.DeleteAsset($"{outFolder}/{prefix}_{k}.wav");

        var paths = new List<string>();
        for (int k = 0; k < segments.Count && k < maxCount; k++)
        {
            int from = Mathf.Max(0, segments[k].x * win - rate / 100);
            int to = Mathf.Min(frames, (segments[k].y + 1) * win + rate / 12);
            int len = to - from;
            var samples = new float[len * ch];
            float segPeak = 0.001f;
            for (int i = 0; i < len * ch; i++) segPeak = Mathf.Max(segPeak, Mathf.Abs(data[from * ch + i]));
            float gain = 0.95f / segPeak;
            int fadeIn = Mathf.Min(len / 4, rate / 400), fadeOut = Mathf.Min(len / 3, rate / 20);
            for (int i = 0; i < len; i++)
            {
                float g = 1f;
                if (i < fadeIn) g = i / (float)fadeIn;
                if (i > len - fadeOut) g = Mathf.Min(g, (len - i) / (float)fadeOut);
                for (int c = 0; c < ch; c++) samples[i * ch + c] = data[(from + i) * ch + c] * g * gain;
            }
            string path = $"{outFolder}/{prefix}_{k}.wav";
            WriteWav(path, samples, ch, rate);
            paths.Add(path);
        }
        AssetDatabase.Refresh();

        var clips = new List<AudioClip>();
        foreach (var p in paths)
        {
            if (AssetImporter.GetAtPath(p) is AudioImporter ai)
            {
                var ss = ai.defaultSampleSettings;
                ss.loadType = AudioClipLoadType.DecompressOnLoad;
                ss.compressionFormat = AudioCompressionFormat.ADPCM;
                ai.defaultSampleSettings = ss;
                ai.SaveAndReimport();
            }
            var c = AssetDatabase.LoadAssetAtPath<AudioClip>(p);
            if (c != null) clips.Add(c);
        }
        Debug.Log($"SoundSlicer: {sourcePath} → {clips.Count} sounds");
        return clips.ToArray();
    }

    public static void WriteWav(string path, float[] samples, int channels, int rate)
    {
        using var w = new BinaryWriter(File.Create(path));
        int bytes = samples.Length * 2;
        w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        w.Write(36 + bytes);
        w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
        w.Write(16);
        w.Write((short)1);
        w.Write((short)channels);
        w.Write(rate);
        w.Write(rate * channels * 2);
        w.Write((short)(channels * 2));
        w.Write((short)16);
        w.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        w.Write(bytes);
        foreach (var f in samples) w.Write((short)Mathf.Clamp(Mathf.RoundToInt(f * 32767f), -32768, 32767));
    }
}
