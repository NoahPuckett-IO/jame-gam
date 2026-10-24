using UnityEditor;
using UnityEngine;

static class CerealArt
{
    const string Folder = "Assets/Materials/CerealArt";

    public enum Piece { Ring, Flake, Puff, Star, Loop }

    public struct Brand
    {
        public string name;
        public Color main, accent, deep;
        public Piece piece;
        public Color pieceColor;
    }

    public static Texture2D Front(int index, Brand b) => Get($"{Folder}/Front_{index}.png", () => PaintFront(b, index));
    public static Texture2D Side(int index, Brand b) => Get($"{Folder}/Side_{index}.png", () => PaintSide(b, index));
    public static Texture2D SideB(int index, Brand b) => Get($"{Folder}/SideB_{index}.png", () => PaintSideB(b, index));
    public static Texture2D Top(int index, Brand b) => Get($"{Folder}/Top_{index}.png", () => PaintTop(b));
    public static Texture2D Cardboard() => Get($"{Folder}/Cardboard.png", PaintCardboard);

    static Texture2D Get(string path, System.Func<Texture2D> paint)
    {
        var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null) return existing;
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Materials", "CerealArt");
        var tex = paint();
        System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);
        if (AssetImporter.GetAtPath(path) is TextureImporter ti)
        {
            ti.wrapMode = path.Contains("Cardboard") ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            ti.anisoLevel = 4;
            ti.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    class Canvas
    {
        public readonly int w, h;
        public readonly Color[] px;
        public Canvas(int w, int h) { this.w = w; this.h = h; px = new Color[w * h]; }

        public void Blend(int x, int y, Color c)
        {
            if (x < 0 || y < 0 || x >= w || y >= h) return;
            int i = y * w + x;
            px[i] = Color.Lerp(px[i], new Color(c.r, c.g, c.b, 1), c.a);
        }

        public void Ellipse(float cx, float cy, float rx, float ry, Color c, float rot = 0)
        {
            float cs = Mathf.Cos(rot), sn = Mathf.Sin(rot);
            int r = Mathf.CeilToInt(Mathf.Max(rx, ry)) + 2;
            for (int y = (int)cy - r; y <= cy + r; y++)
            for (int x = (int)cx - r; x <= cx + r; x++)
            {
                float dx = x - cx, dy = y - cy;
                float u = (dx * cs + dy * sn) / rx, v = (-dx * sn + dy * cs) / ry;
                float d = Mathf.Sqrt(u * u + v * v);
                float a = Mathf.Clamp01((1f - d) * Mathf.Min(rx, ry));
                if (a > 0) Blend(x, y, new Color(c.r, c.g, c.b, c.a * a));
            }
        }

        public void Rect(float x0, float y0, float x1, float y1, Color c, float radius = 0)
        {
            for (int y = (int)y0; y < y1; y++)
            for (int x = (int)x0; x < x1; x++)
            {
                if (radius > 0)
                {
                    float qx = Mathf.Max(x0 + radius - x, 0, x - (x1 - radius));
                    float qy = Mathf.Max(y0 + radius - y, 0, y - (y1 - radius));
                    float d = Mathf.Sqrt(qx * qx + qy * qy);
                    float a = Mathf.Clamp01(radius - d);
                    if (a <= 0) continue;
                    Blend(x, y, new Color(c.r, c.g, c.b, c.a * a));
                }
                else Blend(x, y, c);
            }
        }

        public void Star(float cx, float cy, float r, int points, Color c, float rot = 0)
        {
            int R = Mathf.CeilToInt(r) + 2;
            for (int y = (int)cy - R; y <= cy + R; y++)
            for (int x = (int)cx - R; x <= cx + R; x++)
            {
                float dx = x - cx, dy = y - cy;
                float ang = Mathf.Atan2(dy, dx) + rot;
                float spike = 0.6f + 0.4f * Mathf.Abs(Mathf.Cos(ang * points / 2f));
                float d = Mathf.Sqrt(dx * dx + dy * dy) / (r * spike);
                float a = Mathf.Clamp01((1f - d) * r * 0.5f);
                if (a > 0) Blend(x, y, new Color(c.r, c.g, c.b, c.a * a));
            }
        }

        public Texture2D ToTexture()
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, true);
            t.SetPixels(px);
            t.Apply();
            return t;
        }
    }

    static void Piece_(Canvas c, Piece p, float x, float y, float s, Color col, float rot)
    {
        var shade = col * 0.7f; shade.a = 1;
        switch (p)
        {
            case Piece.Ring:
            case Piece.Loop:
                c.Ellipse(x, y, s, s * 0.9f, shade, rot);
                c.Ellipse(x - s * 0.08f, y + s * 0.08f, s * 0.92f, s * 0.82f, col, rot);
                c.Ellipse(x, y, s * 0.38f, s * 0.34f, new Color(0, 0, 0, 0.0f), rot);
                c.Ellipse(x, y, s * 0.36f, s * 0.32f, shade * 0.8f, rot);
                break;
            case Piece.Flake:
                c.Ellipse(x, y, s * 1.2f, s * 0.75f, shade, rot);
                c.Ellipse(x - s * 0.1f, y + s * 0.08f, s * 1.05f, s * 0.62f, col, rot);
                c.Ellipse(x + s * 0.3f, y + s * 0.2f, s * 0.3f, s * 0.15f, Color.Lerp(col, Color.white, 0.4f), rot);
                break;
            case Piece.Puff:
                c.Ellipse(x, y, s, s, shade, rot);
                c.Ellipse(x - s * 0.15f, y + s * 0.15f, s * 0.82f, s * 0.82f, col, rot);
                c.Ellipse(x - s * 0.35f, y + s * 0.35f, s * 0.25f, s * 0.2f, Color.Lerp(col, Color.white, 0.5f), rot);
                break;
            case Piece.Star:
                c.Star(x, y, s * 1.15f, 5, shade, rot);
                c.Star(x - s * 0.08f, y + s * 0.08f, s, 5, col, rot);
                break;
        }
    }

    static Texture2D PaintFront(Brand b, int seed)
    {
        const int W = 512, H = 720;
        var c = new Canvas(W, H);
        var rng = new System.Random(seed * 97 + 3);
        float R(float a, float bb) => a + (float)rng.NextDouble() * (bb - a);

        Vector2 sun = new Vector2(W * 0.5f, H * 0.38f);
        for (int y = 0; y < H; y++)
        for (int x = 0; x < W; x++)
        {
            float t = y / (float)H;
            var bg = Color.Lerp(b.deep, b.main, Mathf.SmoothStep(0, 1, t * 1.2f));
            float ang = Mathf.Atan2(y - sun.y, x - sun.x);
            float ray = Mathf.Sin(ang * 18f) > 0.15f ? 1f : 0f;
            float dist = Vector2.Distance(new Vector2(x, y), sun) / W;
            bg = Color.Lerp(bg, Color.Lerp(b.main, Color.white, 0.35f), ray * Mathf.Clamp01(0.55f - dist * 0.5f));
            c.px[y * W + x] = bg;
        }

        c.Rect(36, H - 250, W - 36, H - 40, new Color(0, 0, 0, 0.25f), 46);
        c.Rect(28, H - 240, W - 44, H - 48, b.accent, 44);
        c.Rect(40, H - 228, W - 56, H - 60, Color.Lerp(b.accent, Color.white, 0.25f), 36);

        float bx = W * 0.5f, by = H * 0.3f;
        c.Ellipse(bx, by - 120, 200, 40, new Color(0, 0, 0, 0.35f));
        for (int y = 0; y < 150; y++)
        {
            float k = y / 150f;
            float half = Mathf.Lerp(120, 190, Mathf.Sqrt(k));
            c.Rect(bx - half, by - 110 + y, bx + half, by - 109 + y, Color.Lerp(new Color(0.15f, 0.35f, 0.75f), new Color(0.35f, 0.6f, 0.95f), k));
        }
        c.Ellipse(bx, by + 40, 190, 44, new Color(0.95f, 0.97f, 1f));
        for (int i = 0; i < 70; i++)
        {
            float a = R(0, Mathf.PI * 2), rr = Mathf.Sqrt(R(0, 1));
            float px = bx + Mathf.Cos(a) * 160 * rr, py = by + 40 + Mathf.Sin(a) * 34 * rr + R(0, 70) * (1 - rr);
            Piece_(c, b.piece, px, py, R(16, 24), Color.Lerp(b.pieceColor, Color.white, R(0, 0.15f)), R(0, 6.3f));
        }
        for (int i = 0; i < 9; i++)
            c.Ellipse(bx + R(-210, 210), by + R(70, 150), R(6, 14), R(8, 18), new Color(0.97f, 0.98f, 1f, 0.95f));
        for (int i = 0; i < 9; i++)
            Piece_(c, b.piece, R(40, W - 40), R(by + 100, H - 270), R(18, 30), b.pieceColor, R(0, 6.3f));

        c.Star(W - 90, H - 300, 70, 14, new Color(1f, 0.9f, 0.15f));
        c.Star(W - 90, H - 300, 58, 14, new Color(0.95f, 0.2f, 0.15f));
        c.Rect(0, 0, W, 54, new Color(0, 0, 0, 0.35f));
        return c.ToTexture();
    }

    static Texture2D PaintSideB(Brand b, int seed)
    {
        const int W = 256, H = 720;
        var c = new Canvas(W, H);
        for (int y = 0; y < H; y++)
        for (int x = 0; x < W; x++)
            c.px[y * W + x] = Color.Lerp(b.deep, b.main, y / (float)H);
        c.Rect(0, H - 150, W, H - 90, b.accent);
        c.Rect(0, H - 146, W, H - 94, Color.Lerp(b.accent, Color.white, 0.2f));
        c.Ellipse(W / 2f, H - 260, 90, 22, new Color(0.95f, 0.97f, 1f));
        for (int y = 0; y < 70; y++)
        {
            float half = Mathf.Lerp(55, 90, Mathf.Sqrt(y / 70f));
            c.Rect(W / 2f - half, H - 330 + y, W / 2f + half, H - 329 + y, new Color(0.2f, 0.4f, 0.8f));
        }
        var rng = new System.Random(seed + 7);
        for (int i = 0; i < 14; i++)
            Piece_(c, b.piece, W / 2f + (float)(rng.NextDouble() - 0.5) * 140, H - 262 + (float)(rng.NextDouble() - 0.5) * 18, 9, b.pieceColor, (float)rng.NextDouble() * 6);
        c.Rect(18, 140, W - 18, H - 370, new Color(1, 1, 1, 0.9f), 10);
        for (int i = 0; i < 14; i++)
        {
            float y = H - 400 - i * 13;
            float len = 60 + (float)rng.NextDouble() * (W - 110);
            c.Rect(30, y, 30 + len, y + 6, new Color(0.25f, 0.25f, 0.25f));
        }
        for (int i = 0; i < 3; i++)
        {
            float a = i * Mathf.PI * 2 / 3;
            c.Ellipse(W / 2f + Mathf.Cos(a) * 26, 80 + Mathf.Sin(a) * 26, 12, 12, new Color(1, 1, 1, 0.85f));
        }
        c.Ellipse(W / 2f, 80, 40, 40, new Color(1, 1, 1, 0.12f));
        return c.ToTexture();
    }

    static Texture2D PaintTop(Brand b)
    {
        const int W = 256, H = 128;
        var c = new Canvas(W, H);
        for (int i = 0; i < c.px.Length; i++) c.px[i] = b.main;
        c.Rect(0, H / 2f - 3, W, H / 2f + 3, b.deep);
        c.Rect(W / 2f - 22, H / 2f + 3, W / 2f + 22, H / 2f + 22, Color.Lerp(b.main, Color.white, 0.25f), 6);
        c.Rect(0, 0, W, 8, new Color(0, 0, 0, 0.2f));
        c.Rect(0, H - 8, W, H, new Color(0, 0, 0, 0.2f));
        return c.ToTexture();
    }

    static Texture2D PaintCardboard()
    {
        const int W = 256, H = 256;
        var c = new Canvas(W, H);
        var rng = new System.Random(99);
        for (int y = 0; y < H; y++)
        for (int x = 0; x < W; x++)
        {
            float flute = 0.5f + 0.5f * Mathf.Sin(x * Mathf.PI * 2 / 12f);
            float n = (float)rng.NextDouble();
            var col = new Color(0.62f, 0.47f, 0.3f) * (0.92f + flute * 0.06f + n * 0.04f);
            col.a = 1;
            c.px[y * W + x] = col;
        }
        for (int i = 0; i < 120; i++)
        {
            float x = rng.Next(W), y = rng.Next(H);
            c.Rect(x, y, x + rng.Next(3, 14), y + 1, new Color(0.45f, 0.33f, 0.2f, 0.5f));
        }
        var t = c.ToTexture();
        t.wrapMode = TextureWrapMode.Repeat;
        return t;
    }

    static Texture2D PaintSide(Brand b, int seed)
    {
        const int W = 256, H = 720;
        var c = new Canvas(W, H);
        for (int i = 0; i < c.px.Length; i++) c.px[i] = b.main;
        c.Rect(20, 240, W - 20, H - 60, Color.white, 8);
        c.Rect(30, H - 120, W - 30, H - 112, Color.black);
        for (int i = 0; i < 18; i++)
        {
            float y = H - 140 - i * 18;
            c.Rect(32, y, 32 + (i % 3 == 0 ? 150 : 110), y + 7, new Color(0.15f, 0.15f, 0.15f));
            c.Rect(W - 70, y, W - 34, y + 7, new Color(0.15f, 0.15f, 0.15f));
            c.Rect(30, y - 5, W - 30, y - 4, new Color(0.7f, 0.7f, 0.7f));
        }
        var rng = new System.Random(seed);
        c.Rect(40, 60, W - 40, 200, Color.white, 4);
        float x = 54;
        while (x < W - 56)
        {
            float bw = rng.Next(1, 5);
            c.Rect(x, 80, x + bw, 185, Color.black);
            x += bw + rng.Next(2, 5);
        }
        return c.ToTexture();
    }
}
