using UnityEngine;

[ExecuteAlways, RequireComponent(typeof(TextMesh))]
public class SignText : MonoBehaviour
{
    TextMesh text;
    Renderer rend;

    void OnEnable()
    {
        text = GetComponent<TextMesh>();
        rend = GetComponent<Renderer>();
        Font.textureRebuilt += OnRebuilt;
        Apply();
    }

    void OnDisable() => Font.textureRebuilt -= OnRebuilt;

    void OnRebuilt(Font f)
    {
        if (text != null && f == text.font) Apply();
    }

    void Apply()
    {
        if (text == null || text.font == null || rend == null || rend.sharedMaterial == null) return;
        rend.sharedMaterial.mainTexture = text.font.material.mainTexture;
    }
}
