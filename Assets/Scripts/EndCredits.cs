using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(BoxCollider))]
public class EndCredits : MonoBehaviour
{
    [SerializeField] Font titleFont, bodyFont;
    [SerializeField] Vector3 walkOut = Vector3.left;

    static readonly (string role, string name)[] Lines =
    {
        ("DEVELOPMENT & DESIGN", "Ezra Kerney"),
        ("LEVEL DESIGN & STORYBOARDING", "Noah Puckett"),
        ("MUSIC", "Leif Andresen"),
    };

    bool done;
    Image white, backdrop;
    Text title;
    RectTransform roll;

    void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponent<PlayerController>();
        if (done || player == null || player.InCutscene) return;
        done = true;
        StartCoroutine(Run(player));
    }

    IEnumerator Run(PlayerController player)
    {
        BuildUi();
        player.BeginCutscene();
        GameMusic.KeepPlaying = true;

        for (float t = 0; t < 2.2f; t += Time.deltaTime)
        {
            if (t < 1.1f) player.ScriptedMove(walkOut.normalized * 9f);
            else player.ScriptedMove(Vector3.zero);
            white.color = new Color(1, 1, 1, Mathf.SmoothStep(0, 1, t / 2.2f));
            yield return null;
        }
        player.ScriptedMove(Vector3.zero);
        player.CameraLocked = true;

        for (float t = 0; t < 1.5f; t += Time.deltaTime)
        {
            title.color = new Color(0.04f, 0.07f, 0.08f, t / 1.5f);
            yield return null;
        }
        yield return new WaitForSeconds(2.5f);

        for (float t = 0; t < 1.5f; t += Time.deltaTime)
        {
            float k = t / 1.5f;
            backdrop.color = new Color(0.035f, 0.075f, 0.085f, k);
            title.color = Color.Lerp(new Color(0.04f, 0.07f, 0.08f, 1f), new Color(1, 1, 1, 0f), k);
            yield return null;
        }

        roll.gameObject.SetActive(true);
        float height = ((RectTransform)roll.parent).rect.height;
        float travel = roll.rect.height + height * 0.5f;
        var start = Vector2.zero;
        for (float t = 0; t < 18f; t += Time.deltaTime)
        {
            roll.anchoredPosition = start + Vector2.up * travel * (t / 18f);
            yield return null;
        }
        yield return new WaitForSeconds(3f);

        var black = MakeImage("Black", Color.clear);
        for (float t = 0; t < 1.5f; t += Time.deltaTime)
        {
            black.color = new Color(0, 0, 0, t / 1.5f);
            yield return null;
        }
        GameMusic.KeepPlaying = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void BuildUi()
    {
        var canvas = new GameObject("End Credits", typeof(Canvas), typeof(CanvasScaler)).GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasRoot = canvas.transform;

        white = MakeImage("White", new Color(1, 1, 1, 0));
        backdrop = MakeImage("Backdrop", new Color(0.035f, 0.075f, 0.085f, 0));
        title = MakeText(canvasRoot, "TINY HENRY", titleFont, 260, new Color(0, 0, 0, 0));
        Fill(title.rectTransform);

        roll = new GameObject("Roll", typeof(RectTransform)).GetComponent<RectTransform>();
        roll.SetParent(canvasRoot, false);
        roll.anchorMin = roll.anchorMax = new Vector2(0.5f, 0f);
        roll.pivot = new Vector2(0.5f, 1f);
        roll.sizeDelta = new Vector2(1400, 0);
        float y = 0f;
        void Add(string text, Font f, int size, Color c, float gap)
        {
            var t = MakeText(roll, text, f, size, c);
            var rt = t.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.sizeDelta = new Vector2(0, size * 1.3f);
            rt.anchoredPosition = new Vector2(0, -y);
            y += size * 1.3f + gap;
        }
        var accent = new Color(0.2f, 0.85f, 0.75f);
        Add("TINY HENRY", titleFont, 170, Color.white, 120);
        foreach (var (role, who) in Lines)
        {
            Add(role, titleFont, 44, accent, 6);
            Add(who, bodyFont, 64, Color.white, 110);
        }
        Add("THANKS FOR PLAYING", titleFont, 70, Color.white, 40);
        Add("THE END", titleFont, 120, accent, 0);
        roll.sizeDelta = new Vector2(1400, y);
        roll.gameObject.SetActive(false);
    }

    Transform canvasRoot;

    Image MakeImage(string name, Color c)
    {
        var img = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        img.transform.SetParent(canvasRoot, false);
        Fill(img.rectTransform);
        img.color = c;
        img.raycastTarget = false;
        return img;
    }

    static Text MakeText(Transform parent, string text, Font f, int size, Color c)
    {
        var t = new GameObject(text, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
        t.transform.SetParent(parent, false);
        t.font = f;
        t.fontSize = size;
        t.color = c;
        t.text = text;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    static void Fill(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    public void Setup(Font titleF, Font bodyF, Vector3 outward)
    {
        titleFont = titleF;
        bodyFont = bodyF;
        walkOut = outward;
    }
}
