using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Full-screen black fade used by cutscenes and respawns.
public class ScreenFader : MonoBehaviour
{
    [SerializeField] Image image;
    public static ScreenFader Instance { get; private set; }

    void Awake()
    {
        Instance = this;
        if (image != null) image.raycastTarget = false;
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
