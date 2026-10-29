using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFader : MonoBehaviour
{
    [SerializeField] Image image;
    public static ScreenFader Instance { get; private set; }

    void Awake()
    {
        Instance = this;
        if (image != null) image.raycastTarget = false;
        // fades cover every menu and HUD (the credits, at 100, still draw over them)
        var canvas = GetComponent<Canvas>();
        if (canvas != null) canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, 90);
    }

    public void Set(float alpha)
    {
        if (image == null) return;
        var c = image.color;
        c.a = alpha;
        image.color = c;
        image.enabled = alpha > 0.001f;
    }

    public IEnumerator Fade(float to, float seconds)
    {
        if (image == null) yield break;
        float from = image.color.a;
        image.enabled = true;
        for (float t = 0; t < seconds; t += Time.unscaledDeltaTime)
        {
            Set(Mathf.Lerp(from, to, Mathf.SmoothStep(0, 1, t / seconds)));
            yield return null;
        }
        Set(to);
    }

    public void Setup(Image img) => image = img;
}
